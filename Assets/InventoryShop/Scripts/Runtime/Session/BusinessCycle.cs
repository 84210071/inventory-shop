using System;
using System.IO;
using System.Linq;

namespace InventoryShop
{
    public sealed partial class ShopEconomy
    {
        public const int ChallengeDays = 5;
        public const int ProfitTarget = 1000;

        public int Day { get; private set; } = 1;
        public BusinessPhase Phase { get; private set; }
        public ChallengeResult Result { get; private set; }
        private long openingPurchases, openingSales;
        public long DailyPurchases => TotalPurchaseSpend - openingPurchases;
        public long DailySales => TotalSalesRevenue - openingSales;
        public long NetProfit => TotalSalesRevenue - TotalPurchaseSpend;

        public bool Settle()
        {
            if (transacting || Phase != BusinessPhase.Trading || CustomerDay == null || !CustomerDay.EndTrading()) return false;
            if (Day == ChallengeDays)
                Result = NetProfit >= ProfitTarget ? ChallengeResult.Won : ChallengeResult.Lost;
            Phase = BusinessPhase.Settlement;
            return true;
        }

        public bool ShowResult()
        {
            if (Phase != BusinessPhase.Settlement || Day != ChallengeDays) return false;
            Phase = BusinessPhase.Result;
            return true;
        }

        public bool Advance(CustomerRoster roster, WholesaleCatalog catalog)
        {
            if (transacting || Day == int.MaxValue ||
                (Day == ChallengeDays ? Phase != BusinessPhase.Result : Phase != BusinessPhase.Settlement)) return false;
            DailyOrders next = roster.Create(catalog, Day + 1);
            Day++;
            CustomerDay = next;
            openingPurchases = TotalPurchaseSpend;
            openingSales = TotalSalesRevenue;
            Phase = BusinessPhase.Trading;
            return true;
        }

        public ShopSave Capture() => new ShopSave
        {
            version = 1, day = Day, gold = Gold, purchases = TotalPurchaseSpend, sales = TotalSalesRevenue,
            openingPurchases = openingPurchases, openingSales = openingSales, phase = Phase, result = Result,
            backpack = CaptureSlots(StockArea.Backpack, InventoryStock.BackpackCapacity),
            shelf = CaptureSlots(StockArea.Shelf, InventoryStock.ShelfCapacity),
            orders = CustomerDay.Orders.Select(order => new ShopSave.Order
            {
                customer = order.Customer.Name, id = order.Item.Id, quantity = order.Quantity, total = order.Total, state = order.State
            }).ToArray()
        };

        private ShopSave.Stack[] CaptureSlots(StockArea area, int capacity) => Enumerable.Range(0, capacity)
            .Select(i => Stock.Get(new SlotAddress(area, i)))
            .Select(stack => new ShopSave.Stack { id = stack.Item?.Id, quantity = stack.Quantity }).ToArray();

        public static ShopEconomy Restore(ShopSave save, WholesaleCatalog catalog, CustomerRoster roster)
        {
            if (save == null || save.version != 1 || save.day < 1 || save.gold < 0 || save.purchases < 0 || save.sales < 0 ||
                save.openingPurchases < 0 || save.openingPurchases > save.purchases ||
                save.openingSales < 0 || save.openingSales > save.sales ||
                save.sales - save.purchases != save.gold - 500L ||
                !Enum.IsDefined(typeof(BusinessPhase), save.phase) || !Enum.IsDefined(typeof(ChallengeResult), save.result) ||
                (save.phase == BusinessPhase.Result && save.day != ChallengeDays) ||
                ((save.day < ChallengeDays || (save.day == ChallengeDays && save.phase == BusinessPhase.Trading))
                    != (save.result == ChallengeResult.Pending)))
                throw new InvalidDataException("Invalid business state.");

            var items = catalog.Items.ToDictionary(item => item.Id);
            ItemStack[] Slots(ShopSave.Stack[] slots, int capacity)
            {
                if (slots == null || slots.Length != capacity) throw new InvalidDataException("Invalid inventory size.");
                return slots.Select(slot =>
                {
                    if (string.IsNullOrEmpty(slot.id) && slot.quantity == 0) return default;
                    if (string.IsNullOrEmpty(slot.id) || !items.TryGetValue(slot.id, out var item))
                        throw new InvalidDataException("Unknown inventory item.");
                    return new ItemStack(item, slot.quantity);
                }).ToArray();
            }
            var economy = new ShopEconomy(new InventoryStock(Slots(save.backpack, InventoryStock.BackpackCapacity),
                Slots(save.shelf, InventoryStock.ShelfCapacity)))
            {
                Day = save.day, Gold = save.gold, TotalPurchaseSpend = save.purchases, TotalSalesRevenue = save.sales,
                openingPurchases = save.openingPurchases, openingSales = save.openingSales,
                Phase = save.phase, Result = save.result
            };
            if (save.orders == null || save.orders.Length != DailyOrders.DailyLimit ||
                save.orders.Select(order => order.customer).Distinct().Count() != DailyOrders.DailyLimit)
                throw new InvalidDataException("Invalid customer list.");
            var orders = save.orders.Select(entry =>
            {
                if (!items.TryGetValue(entry.id, out var item) || entry.quantity < 1 ||
                    entry.quantity > Math.Min(3, item.StackLimit) || entry.total != (long)item.SalePrice * entry.quantity ||
                    !Enum.IsDefined(typeof(OrderState), entry.state))
                    throw new InvalidDataException("Invalid order.");
                var order = new CustomerOrder(roster.Find(entry.customer), item, entry.quantity);
                order.Resolve(entry.state);
                return order;
            }).ToArray();
            economy.CustomerDay = DailyOrders.Restore(orders, save.phase != BusinessPhase.Trading);
            return economy;
        }
    }
}
