#if INVENTORYSHOP_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class ReleaseSmoke : MonoBehaviour
    {
        private int checks;
        private string Mode => Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("-inventory-"));
        private string SaveFile => Path.Combine(Application.persistentDataPath, "inventory-shop-smoke.json");
        private string ExpectedFile => Path.Combine(Application.persistentDataPath, "inventory-shop-smoke-expected.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run()
        {
            string mode = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("-inventory-"));
            if (mode == null) return;
            var node = new GameObject("Release smoke test", typeof(ReleaseSmoke));
            DontDestroyOnLoad(node);
            var smoke = node.GetComponent<ReleaseSmoke>();
            if (mode == "-inventory-win" || mode == "-inventory-fail")
            {
                if (File.Exists(smoke.SaveFile)) File.Delete(smoke.SaveFile);
            }
            else if (mode == "-inventory-corrupt")
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(smoke.SaveFile, "{ broken");
            }
        }

        private static void Seed(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Shop") return;
            var roster = FindObjectOfType<CustomerOrdersView>().Roster;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(CustomerRoster).GetField("useFixedSeed", flags).SetValue(roster, true);
            typeof(CustomerRoster).GetField("seed", flags).SetValue(roster, 117);
        }

        private void Check(bool condition, string name)
        {
            if (!condition)
            {
                Debug.LogError("FAIL " + name);
                Application.Quit(2);
                throw new InvalidOperationException(name);
            }
            checks++;
            Debug.Log("PASS " + name);
        }

        private PointerEventData At(Component component)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = component.transform.position, button = PointerEventData.InputButton.Left, pointerId = -1
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count > 0) pointer.pointerCurrentRaycast = hits[0];
            return pointer;
        }

        private void Click(Button button)
        {
            var e = At(button);
            Check(button.IsInteractable() && e.pointerCurrentRaycast.gameObject == button.gameObject, "binding " + button.name);
            ExecuteEvents.Execute(button.gameObject, e, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, e, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, e, ExecuteEvents.pointerClickHandler);
        }

        private static IEnumerator Wait(float seconds = 0.55f) => new WaitForSecondsRealtime(seconds);

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", name + ".png"), image.EncodeToPNG());
            Destroy(image);
        }

        private static string Snapshot(ShopEconomy economy) => JsonUtility.ToJson(economy.Capture());

        private IEnumerator Start()
        {
            yield return Wait();
            var menu = FindObjectOfType<MenuFlow>();
            Check(SceneManager.GetActiveScene().name == "MainMenu" && menu != null, "main menu loaded");
            if (Mode == "-inventory-corrupt")
            {
                Check(!menu.ContinueButton.interactable && menu.SaveStatus.text.Contains("无法读取"), "corrupt save is handled");
                Complete();
                yield break;
            }
            if (Mode == "-inventory-restore")
            {
                Click(menu.ContinueButton);
                yield return Wait(1.2f);
                var restored = FindObjectOfType<BusinessView>().Customers.Inventory.Economy;
                Check(Snapshot(restored) == File.ReadAllText(ExpectedFile), "separate process restores complete saved state");
                Check(restored.Day == 6 && restored.Result == ChallengeResult.Won, "free play survives restart");
                Complete();
                yield break;
            }
            Check(!menu.ContinueButton.interactable, "continue disabled without save");
            bool win = Mode == "-inventory-win";
            if (win) yield return Capture("01-MainMenu");
            SceneManager.sceneLoaded += Seed;
            Click(menu.NewButton);
            yield return Wait(1.25f);

            var view = FindObjectOfType<BusinessView>();
            var customers = view.Customers;
            var inventory = customers.Inventory;
            var economy = inventory.Economy;
            var catalog = customers.Catalog;
            var roster = customers.Roster;
            Check(economy.Gold == 500 && economy.Day == 1 && economy.NetProfit == 0 &&
                Empty(economy.Stock, StockArea.Backpack, 20) && Empty(economy.Stock, StockArea.Shelf, 6), "new game starts empty with 500");
            Check(!economy.Settle() && !economy.Advance(roster, catalog), "cannot settle or advance early");
            Check(new[] { "Click","Open","Close","Drag","Place","Buy","Sale","Error","Settlement","Music" }
                .All(name => Resources.Load<AudioClip>("ShopAudio/" + name) != null), "all ten audio clips included");
            TestInventory(roster, catalog);
            TestBalance(roster, catalog);
            SceneManager.sceneLoaded -= Seed;

            if (win)
            {
                foreach (int width in new[] { 1280, 1920, 1600 })
                {
                    Screen.SetResolution(width, width * 9 / 16, FullScreenMode.Windowed);
                    yield return Wait();
                    Check(Screen.width == width &&
                        At(customers.Card.Sell).pointerCurrentRaycast.gameObject == customers.Card.Sell.gameObject &&
                        At(inventory.ShelfSlots[5]).pointerCurrentRaycast.gameObject == inventory.ShelfSlots[5].gameObject,
                        "shop layout " + width);
                }
                yield return Capture("02-Shop");
                var wholesale = FindObjectOfType<WholesalePanel>();
                var first = economy.CustomerDay.Current;
                var failed = Snapshot(economy);
                Click(customers.Card.Sell);
                Check(Snapshot(economy) == failed && customers.Feedback.text == "货架库存不足", "UI sale shortage preserves state");
                Click(wholesale.OpenButton);
                yield return Wait();
                yield return Capture("04-Wholesale");
                int index = Array.IndexOf(catalog.Items.ToArray(), first.Item);
                var scroll = wholesale.GetComponentInChildren<ScrollRect>();
                scroll.verticalNormalizedPosition = index < 5 ? 1f : 0f;
                yield return Wait(0.18f);
                Click(wholesale.Cards[index].Button);
                for (int i = 1; i < first.Quantity; i++) Click(wholesale.PlusButton);
                Click(wholesale.BuyButton);
                Check(economy.Gold == 500 - first.Item.PurchasePrice * first.Quantity, "UI purchase debits exact amount");
                Click(wholesale.CloseButton);
                yield return Wait(0.65f);
                Click(inventory.BackpackButton);
                yield return Wait();
                yield return Capture("03-Backpack");
                var bag = inventory.BackpackSlots[0];
                var slot = inventory.ShelfSlots[0];
                var source = At(bag);
                var target = At(slot);
                Check(source.pointerCurrentRaycast.gameObject == bag.gameObject &&
                    target.pointerCurrentRaycast.gameObject == slot.gameObject, "drag raycasts in actual UI");
                ExecuteEvents.Execute(bag.gameObject, source, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(bag.gameObject, target, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(bag.gameObject, target, ExecuteEvents.endDragHandler);
                Check(economy.Stock.Get(slot.Address).Quantity == first.Quantity &&
                    economy.Stock.Get(bag.Address).IsEmpty, "UI drag moves exact stock");
                Click(inventory.CloseButton);
                yield return Wait();
                yield return Capture("05-Customer");
                Click(customers.Card.Sell);
                customers.Card.Sell.onClick.Invoke();
                Check(first.State == OrderState.Sold && economy.TotalSalesRevenue == first.Total &&
                    economy.Stock.Get(slot.Address).IsEmpty && economy.CustomerDay.Processed == 1, "UI sale credits once and advances order");
                yield return Wait(0.75f);
                Click(view.StatsButton);
                yield return Wait();
                Check(view.Values.text.Split('\n')[0] == economy.Gold.ToString(), "statistics use live gold");
                yield return Capture("08-Statistics");
                Click(view.Close);
                yield return Wait();
                Click(inventory.Screen.transform.Find("Screen/Settings").GetComponent<Button>());
                yield return Wait();
                var controls = inventory.Screen.Settings.GetComponent<VolumeControls>();
                float originalVolume = GameAudio.Volume;
                Click(originalVolume >= 0.1f ? controls.Lower : controls.Raise);
                Check(!Mathf.Approximately(originalVolume, GameAudio.Volume), "volume controls change master gain");
                GameAudio.Volume = originalVolume;
                Click(view.SaveButton);
                Check(GameSession.TryRead(catalog, roster, out var manual) && Snapshot(manual) == Snapshot(economy),
                    "manual save preserves current order");
                Click(inventory.Screen.SettingsClose);
                yield return Wait();
            }

            var reserve = new SlotAddress(StockArea.Shelf, 5);
            for (int day = 1; day <= 5; day++)
            {
                Check(economy.Day == day && economy.CustomerDay.Orders.Count == 5 && economy.CustomerDay.Processed == (day == 1 && win ? 1 : 0),
                    "five fresh orders day " + day);
                while (economy.CustomerDay.Current != null)
                {
                    var order = economy.CustomerDay.Current;
                    if (win) SupplyAndSell(economy, order);
                    else Check(economy.Reject(order), "reject order");
                }
                if (win && day == 1)
                {
                    var wood = catalog.Items.First(item => item.Id == "timber");
                    Check(economy.Purchase(wood, 1) == PurchaseResult.Success &&
                        economy.Stock.Move(new SlotAddress(StockArea.Backpack, 0), reserve) == StockMove.Moved,
                        "retained stock prepared for next day");
                }
                customers.RefreshDay();
                Check(economy.CustomerDay.Processed == 5 && economy.CustomerDay.Current == null &&
                    !economy.Reject(economy.CustomerDay.Orders[4]), "no sixth order or repeat");
                Click(customers.EndButton);
                Check(economy.Phase == BusinessPhase.Settlement && !economy.Settle() &&
                    GameSession.TryRead(catalog, roster, out var saved) && Snapshot(saved) == Snapshot(economy), "settlement saves exactly once");
                yield return Wait();
                if (win && day == 1) { yield return Wait(1.1f); yield return Capture("06-Settlement"); }
                Click(view.Primary);
                yield return Wait(0.65f);
                if (win) Check(economy.Stock.Get(reserve).Quantity == 1, "unsold stock survives settlement and advance");
            }

            Check(economy.Day == 5 && economy.Phase == BusinessPhase.Result &&
                economy.Result == (win ? ChallengeResult.Won : ChallengeResult.Lost) &&
                view.Heading.text == (win ? "挑战胜利" : "挑战失败"), "fifth day challenge result");
            Debug.Log("FINAL PROFIT " + economy.NetProfit);
            yield return Capture(win ? "07-Victory" : "09-Defeat");
            if (win)
            {
                Click(view.Primary);
                yield return Wait(0.68f);
                Check(economy.Day == 6 && economy.Phase == BusinessPhase.Trading && economy.Result == ChallengeResult.Won &&
                    economy.CustomerDay.Processed == 0, "free play starts day six");
                var item = catalog.Items.First(entry => entry.Id == "timber");
                Check(economy.Purchase(item, 4) == PurchaseResult.Success &&
                    economy.Stock.Move(new SlotAddress(StockArea.Backpack, 0), new SlotAddress(StockArea.Shelf, 4)) == StockMove.Moved,
                    "save contains real stock on a specific shelf slot");
                Check(economy.Reject(economy.CustomerDay.Current) && GameSession.Save(), "save partially processed free day");
                File.WriteAllText(ExpectedFile, Snapshot(economy));
            }
            else
            {
                Click(view.Restart);
                yield return Wait(1.15f);
                var reset = FindObjectOfType<BusinessView>().Customers.Inventory.Economy;
                Check(!ReferenceEquals(reset, economy) && reset.Day == 1 && reset.Gold == 500 &&
                    reset.NetProfit == 0 && reset.Result == ChallengeResult.Pending && reset.CustomerDay.Processed == 0,
                    "restart resets session");
            }
            Complete();
        }

        private static bool Empty(InventoryStock stock, StockArea area, int count) =>
            Enumerable.Range(0, count).All(i => stock.Get(new SlotAddress(area, i)).IsEmpty);

        private void TestInventory(CustomerRoster roster, WholesaleCatalog catalog)
        {
            var wood = catalog.Items.First(item => item.Id == "timber");
            var gem = catalog.Items.First(item => item.Id == "frost-crystal");
            var dagger = catalog.Items.First(item => item.Id == "adventurer-dagger");
            var economy = new ShopEconomy(new InventoryStock(Array.Empty<ItemStack>(), Array.Empty<ItemStack>()));
            economy.InitializeCustomers(roster, catalog);
            var stock = economy.Stock;
            var bag0 = new SlotAddress(StockArea.Backpack, 0);
            var bag1 = new SlotAddress(StockArea.Backpack, 1);
            var shelf0 = new SlotAddress(StockArea.Shelf, 0);
            Check(economy.Purchase(wood, 19) == PurchaseResult.Success && economy.Purchase(wood, 3) == PurchaseResult.Success &&
                stock.Get(bag0).Quantity == 20 && stock.Get(bag1).Quantity == 2 && economy.Gold == 324, "purchase fills and splits stacks");
            var snapshot = Snapshot(economy);
            Check(economy.Purchase(gem, 3) == PurchaseResult.InsufficientGold && economy.Purchase(wood, 0) == PurchaseResult.Invalid &&
                Snapshot(economy) == snapshot, "insufficient gold and illegal purchase leave snapshot intact");
            Check(stock.Move(bag0, shelf0) == StockMove.Moved && stock.Move(bag1, shelf0) == StockMove.Rejected &&
                stock.Move(shelf0, bag0) == StockMove.Moved && stock.Get(bag0).Quantity + stock.Get(bag1).Quantity == 22,
                "backpack shelf transfers conserve quantities");
            var full = new ShopEconomy(new InventoryStock(Enumerable.Repeat(new ItemStack(dagger, 1), 20).ToArray(), Array.Empty<ItemStack>()));
            Check(full.Purchase(wood, 1) == PurchaseResult.BackpackFull && full.Gold == 500 && full.TotalPurchaseSpend == 0,
                "full backpack does not debit");
            var packedShelf = new InventoryStock(new[] { new ItemStack(wood, 1) },
                Enumerable.Repeat(new ItemStack(dagger, 1), 6).ToArray());
            Check(packedShelf.Move(bag0, shelf0) == StockMove.Rejected && packedShelf.Get(bag0).Quantity == 1 &&
                packedShelf.Get(shelf0).Item == dagger, "full shelf does not lose stock");
            var splitShelf = new ShopEconomy(new InventoryStock(Array.Empty<ItemStack>(),
                new[] { new ItemStack(gem, 1), new ItemStack(gem, 1) }));
            splitShelf.InitializeCustomers(roster, catalog);
            while (splitShelf.CustomerDay.Current.Item != gem)
                splitShelf.Reject(splitShelf.CustomerDay.Current);
            Check(splitShelf.Sell(splitShelf.CustomerDay.Current) == SaleResult.Success &&
                splitShelf.Gold == 980 && Empty(splitShelf.Stock, StockArea.Shelf, 6), "one sale consumes two shelf slots");
            var order = economy.CustomerDay.Current;
            snapshot = Snapshot(economy);
            Check(economy.Sell(order) == SaleResult.OutOfStock && Snapshot(economy) == snapshot, "stock shortage leaves snapshot intact");
            int gold = economy.Gold;
            Check(economy.Reject(order) && economy.Gold == gold && stock.Get(bag0).Quantity == 20 &&
                !economy.Reject(order), "reject does not change inventory or gold");
        }

        private void TestBalance(CustomerRoster roster, WholesaleCatalog catalog)
        {
            var record = roster.Find("赤羽艾琳");
            var rosterEntries = new[] {record, roster.Find("雾袍奥伦"), roster.Find("商人萨娜"),
                roster.Find("骑士罗恩"), roster.Find("旅者米拉"), roster.Find("学者薇拉")};
            long minimum = long.MaxValue;
            for (int seed = 0; seed < 100; seed++)
            {
                long profit = 0;
                long gold = 500;
                for (int day = 0; day < 5; day++)
                {
                    var orders = new DailyOrders(rosterEntries, catalog.Items, seed + day);
                    foreach (var order in orders.Orders)
                    {
                        long cost = order.Item.PurchasePrice * order.Quantity;
                        if (cost > gold) throw new InvalidOperationException("Unfundable catalog order.");
                        gold += order.Total - cost;
                        profit += order.Total - cost;
                    }
                }
                minimum = Math.Min(minimum, profit);
            }
            Check(minimum >= ShopEconomy.ProfitTarget, "100 deterministic challenge seeds fund and meet target; min " + minimum);
        }

        private void SupplyAndSell(ShopEconomy economy, CustomerOrder order)
        {
            int gold = economy.Gold;
            int cost = order.Item.PurchasePrice * order.Quantity;
            Check(economy.Purchase(order.Item, order.Quantity) == PurchaseResult.Success &&
                economy.Stock.Move(new SlotAddress(StockArea.Backpack, 0), new SlotAddress(StockArea.Shelf, 0)) == StockMove.Moved,
                "buy and stock customer request");
            Check(economy.Sell(order) == SaleResult.Success && economy.Gold == gold - cost + order.Total &&
                economy.Sell(order) == SaleResult.Invalid &&
                economy.Stock.Get(new SlotAddress(StockArea.Shelf, 0)).IsEmpty, "sale credits exact amount once");
        }

        private void Complete()
        {
            Debug.Log("INVENTORY V1 COMPLETE " + Mode + " " + checks);
            Application.Quit();
        }
    }
}
#endif
