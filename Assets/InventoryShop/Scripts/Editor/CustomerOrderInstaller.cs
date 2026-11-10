using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static InventoryShop.Editor.InventoryInstaller;

namespace InventoryShop.Editor
{
    public static class CustomerOrderInstaller
    {
        private const string Root = "Assets/InventoryShop/";
        private const string ShopPath = "Assets/Scenes/Shop.unity";
        private static readonly Color Ivory = new Color32(255, 230, 174, 255);
        private static readonly Color Ink = new Color32(64, 37, 24, 255);
        private static readonly string[] Portraits = { "Adventurer", "Wizard", "Merchant", "Knight", "Traveler", "Scholar" };

        [MenuItem("Tools/InventoryShop/Install Customer Orders")]
        public static void Install()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ShopPath);
            bool alreadyOpen = scene.isLoaded;
            if (!alreadyOpen)
            {
                if (Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                    previous = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                scene = EditorSceneManager.OpenScene(ShopPath, OpenSceneMode.Additive);
            }
            try
            {
                SceneManager.SetActiveScene(scene);
                var inventory = scene.GetRootGameObjects().Select(root => root.GetComponent<ShopInventory>())
                    .FirstOrDefault(component => component != null);
                if (inventory == null) throw new InvalidOperationException("Install the backpack first.");
                if (inventory.Screen.Customers != null)
                {
                    Debug.Log("InventoryShop customer orders are already installed.");
                    return;
                }
                var catalog = AssetDatabase.LoadAssetAtPath<WholesaleCatalog>(Root + "Data/WholesaleCatalog.asset");
                if (catalog == null) throw new InvalidOperationException("Install wholesale before orders.");

                var roster = CreateRoster();
                var font = LoadFont("CustomerChinese", "CustomerGlyphs");
                var prefab = CreateCard(font);
                AddToScene(inventory, roster, catalog, prefab, font);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Shop scene.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static CustomerRoster CreateRoster()
        {
            const string path = Root + "Data/CustomerRoster.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CustomerRoster>(path);
            if (existing != null) return existing;

            string[] names = { "赤羽艾琳", "雾袍奥伦", "商人萨娜", "骑士罗恩", "旅者米拉", "学者薇拉" };
            string[] greetings =
            {
                "离开城门前，我想再补些行囊。",
                "我的下一场试验正缺些材料。",
                "路过贵店，想挑些货带回集市。",
                "请帮我备好这份巡路补给。",
                "山路还很长，我得把行囊备齐。",
                "能否替我找一份清单上的货物？"
            };
            var roster = ScriptableObject.CreateInstance<CustomerRoster>();
            var data = new SerializedObject(roster);
            var entries = data.FindProperty("customers");
            entries.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                string imagePath = Root + "Art/Portraits/" + Portraits[i] + ".png";
                var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
                if (importer == null) throw new FileNotFoundException(imagePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Name").stringValue = names[i];
                entry.FindPropertyRelative("Greeting").stringValue = greetings[i];
                entry.FindPropertyRelative("Portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(roster, path);
            return roster;
        }

        private static GameObject CreateCard(TMP_FontAsset font)
        {
            var parchment = Art("Panel");
            var card = Picture("CustomerOrderCard", null, parchment, 0, 0, 555, 704);
            try
            {
                card.preserveAspect = false;
                card.raycastTarget = true;
                var view = card.gameObject.AddComponent<CustomerOrderCard>();
                view.Surface = HiddenGroup(card.gameObject);
                Label("Heading", card.transform, font, "来客订单", Ink, 39, 0, 273, 345, 61);
                view.Position = Label("Position", card.transform, font, "", Ink, 21, 0, 223, 310, 36);

                var frame = Picture("Portrait Frame", card.transform, Art("Slot"), -126, 119, 171, 174);
                frame.preserveAspect = false;
                view.Portrait = Picture("Portrait", frame.transform, null, 0, 1, 146, 150);
                view.PortraitFade = HiddenGroup(view.Portrait.gameObject);
                view.Name = Label("Customer Name", card.transform, font, "", Ink, 30, 82, 159, 256, 49);
                view.Greeting = Label("Greeting", card.transform, font, "", Ink, 22, 82, 89, 257, 82);

                Label("Request Label", card.transform, font, "顾客所求", Ink, 23, 0, 7, 325, 38);
                view.ItemIcon = Picture("Item Icon", card.transform, null, -124, -97, 120, 120);
                view.ItemName = Label("Item Name", card.transform, font, "", Ink, 28, 79, -66, 281, 46);
                view.Quantity = Label("Quantity", card.transform, font, "", Ink, 23, 79, -115, 263, 37);
                view.Total = Label("Total", card.transform, font, "", Ink, 28, 0, -198, 435, 51);
                view.Sell = WholesaleInstaller.Wood(card.transform, font, "Sell Order", "完成交易", -116, -271, 200, 58, 26);
                view.Decline = WholesaleInstaller.Wood(card.transform, font, "Decline Order", "拒绝订单", 116, -271, 200, 58, 26);

                var prompt = Picture("Decline Confirmation", card.transform, parchment, 0, -161, 471, 281);
                prompt.preserveAspect = false;
                prompt.raycastTarget = true;
                view.Confirmation = HiddenGroup(prompt.gameObject);
                Label("Question", prompt.transform, font, "确认拒绝这份订单？", Ink, 27, 0, 51, 390, 45);
                Label("Explanation", prompt.transform, font, "顾客将离开店铺", Ink, 20, 0, 6, 360, 33);
                view.Confirm = WholesaleInstaller.Wood(prompt.transform, font, "Confirm Decline", "确认拒绝", -97, -60, 156, 49, 22);
                view.Cancel = WholesaleInstaller.Wood(prompt.transform, font, "Cancel Decline", "取消", 97, -60, 156, 49, 22);
                return PrefabUtility.SaveAsPrefabAsset(card.gameObject, Root + "Prefabs/UI/CustomerOrderCard.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(card.gameObject); }
        }

        private static void AddToScene(ShopInventory inventory, CustomerRoster roster, WholesaleCatalog catalog,
            GameObject prefab, TMP_FontAsset font)
        {
            var screen = inventory.Screen;
            var content = screen.transform.Find("Screen");
            var overlay = Rect("Customer Orders", screen.transform, 0, 0, 0, 0);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.SetSiblingIndex(inventory.Overlay.transform.GetSiblingIndex());

            var view = overlay.gameObject.AddComponent<CustomerOrdersView>();
            view.Screen = screen;
            view.Inventory = inventory;
            view.Roster = roster;
            view.Catalog = catalog;
            screen.Customers = view;
            EditorUtility.SetDirty(screen);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, overlay);
            instance.name = "Customer Order Card";
            SetRect((RectTransform)instance.transform, 563, -9, 555, 704);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            view.Card = instance.GetComponent<CustomerOrderCard>();
            view.Progress = Label("Customer Progress", overlay, font, "", Ivory, 23, -142, 453, 450, 39);
            view.DayStatus = Label("Day Status", overlay, font, "", Ivory, 22, -142, 416, 460, 37);
            view.ToggleButton = WholesaleInstaller.Wood(overlay, font, "Toggle Order Card", "收起订单", 127, 450, 126, 53, 22);
            view.ToggleLabel = view.ToggleButton.GetComponentInChildren<TMP_Text>();
            view.EndButton = content.Find("Shop Action 4").GetComponent<Button>();
            ClearClick(view.EndButton);

            var toast = Picture("Order Feedback", overlay, Art("Button"), 560, 380, 445, 55);
            toast.preserveAspect = false;
            view.FeedbackGroup = HiddenGroup(toast.gameObject);
            view.Feedback = Label("Feedback", toast.transform, font, "", Ivory, 25, 0, 0, 406, 43);
        }

        private static CanvasGroup HiddenGroup(GameObject node)
        {
            var group = node.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = false;
            return group;
        }

        private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/" + name + ".png")
            ?? throw new FileNotFoundException(name);
    }
}
