using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace InventoryShop.Editor
{
    public static class InventoryInstaller
    {
        private const string Root = "Assets/InventoryShop/";
        private const string ShopPath = "Assets/Scenes/Shop.unity";
        private static readonly Color Ivory = new Color32(255, 235, 185, 255);
        private static readonly Color Ink = new Color32(67, 39, 24, 255);

        [MenuItem("Tools/InventoryShop/Install Backpack UI")]
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
                var screen = scene.GetRootGameObjects().Select(root => root.GetComponent<ShopScreen>())
                    .First(component => component != null);
                if (screen.GetComponent<ShopInventory>() != null)
                {
                    Debug.Log("InventoryShop backpack UI is already installed.");
                    return;
                }

                var font = LoadFont();
                var initial = CreateStock(screen.Products);
                var cellPrefab = CreateCell(font);
                var controller = screen.gameObject.AddComponent<ShopInventory>();
                controller.Screen = screen;
                controller.InitialStock = initial;
                controller.Overlay = BuildOverlay(screen, controller, cellPrefab, font);
                var content = screen.transform.Find("Screen");
                controller.BackpackButton = content.Find("Shop Action 1").GetComponent<Button>();
                ClearClick(controller.BackpackButton);
                controller.ShelfSlots = new StockSlotView[InventoryStock.ShelfCapacity];
                for (int i = 0; i < controller.ShelfSlots.Length; i++)
                    controller.ShelfSlots[i] = ConnectShelf(content.Find("Shelf/Shelf Slot " + (i + 1)), font);
                screen.Inventory = controller;
                screen.Products = Array.Empty<ShopScreen.Product>();
                EditorUtility.SetDirty(screen);
                EditorUtility.SetDirty(controller);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Shop scene.");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static StartingStock CreateStock(ShopScreen.Product[] products)
        {
            const string folder = Root + "Data";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Data");
            string path = folder + "/StartingStock.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StartingStock>(path);
            if (existing != null) return existing;
            string[] ids = { "ember-potion", "traveler-scroll", "frost-crystal", "moon-dew-herb" };
            int[] purchase = { 24, 15, 75, 9 };
            int[] limits = { 10, 8, 5, 20 };
            var definitions = new ItemDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                string itemPath = folder + "/" + ids[i] + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    var data = new SerializedObject(item);
                    data.FindProperty("id").stringValue = ids[i];
                    data.FindProperty("displayName").stringValue = products[i].Name;
                    data.FindProperty("quality").stringValue = products[i].Quality;
                    data.FindProperty("description").stringValue = products[i].Description;
                    data.FindProperty("icon").objectReferenceValue = products[i].Icon;
                    data.FindProperty("purchasePrice").intValue = purchase[i];
                    data.FindProperty("salePrice").intValue = products[i].Price;
                    data.FindProperty("stackLimit").intValue = limits[i];
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(item, itemPath);
                }
                definitions[i] = item;
            }
            var stock = ScriptableObject.CreateInstance<StartingStock>();
            var seed = new SerializedObject(stock);
            SetSeeds(seed.FindProperty("backpack"), definitions, new[] { 0, 0, 1, 2, 3 }, new[] { 7, 6, 3, 2, 12 });
            SetSeeds(seed.FindProperty("shelf"), definitions, new[] { 0, 1, 2, 3 }, new[] { 2, 1, 1, 4 });
            seed.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(stock, path);
            return stock;
        }

        private static void SetSeeds(SerializedProperty list, ItemDefinition[] items, int[] indices, int[] quantities)
        {
            list.arraySize = indices.Length;
            for (int i = 0; i < indices.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("item").objectReferenceValue = items[indices[i]];
                entry.FindPropertyRelative("quantity").intValue = quantities[i];
            }
        }

        private static TMP_FontAsset LoadFont()
        {
            const string path = Root + "Fonts/InventoryChinese.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/InventoryChinese.otf");
            var font = TMP_FontAsset.CreateFontAsset(source, 60, 7, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (!font.TryAddCharacters(File.ReadAllText(Root + "Fonts/InventoryGlyphs.txt"), out string missing))
                throw new InvalidOperationException("Missing inventory glyphs: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.material.name = "Inventory Chinese SDF Material";
            font.atlasTexture.name = "Inventory Chinese SDF Atlas";
            font.atlasTexture.Apply(false, true);
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static GameObject CreateCell(TMP_FontAsset font)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/ShelfSlot.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                // Save a distinct prefab so changes to the phase-one shelf asset remain scoped.
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = "BackpackSlot";
                SetRect((RectTransform)instance.transform, 0, 0, 93, 93);
                var view = instance.AddComponent<StockSlotView>();
                view.Feedback = instance.GetComponent<ButtonFeedback>();
                view.Icon = Picture("Item Icon", instance.transform, null, 0, 3, 71, 71);
                view.Quantity = Label("Quantity", instance.transform, font, "", Ivory, 20, 22, -29, 37, 29);
                return PrefabUtility.SaveAsPrefabAsset(instance, Root + "Prefabs/UI/BackpackSlot.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static StockSlotView ConnectShelf(Transform slot, TMP_FontAsset font)
        {
            var button = slot.GetComponent<Button>();
            ClearClick(button);
            button.enabled = true;
            var face = slot.GetComponent<Image>();
            face.raycastTarget = true;
            var view = slot.gameObject.AddComponent<StockSlotView>();
            view.Feedback = slot.GetComponent<ButtonFeedback>();
            view.Icon = slot.Find("Product Art")?.GetComponent<Image>()
                ?? Picture("Product Art", slot, null, 0, 21, 166, 166);
            view.Caption = (slot.Find("Product Label") ?? slot.Find("Empty")).GetComponent<TMP_Text>();
            view.Quantity = Label("Quantity", slot, font, "", Ivory, 26, 94, 99, 55, 40);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            PrefabUtility.RecordPrefabInstancePropertyModifications(face);
            return view;
        }

        private static void ClearClick(Button button)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        }

        private static InventoryOverlay BuildOverlay(ShopScreen screen, ShopInventory inventory, GameObject slot, TMP_FontAsset font)
        {
            var root = Rect("Inventory Overlay", screen.transform, 0, 0, 0, 0);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.SetSiblingIndex(screen.Settings.transform.GetSiblingIndex());
            var overlay = root.gameObject.AddComponent<InventoryOverlay>();
            Sprite parchment = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Panel.png");
            var window = Picture("Backpack Window", root, parchment, 563, -9, 555, 704);
            window.preserveAspect = false;
            window.raycastTarget = true;
            overlay.Window = window.gameObject.AddComponent<CanvasGroup>();
            overlay.Window.alpha = 0;
            overlay.Window.interactable = overlay.Window.blocksRaycasts = false;

            Label("Heading", window.transform, font, "背包", Ink, 40, 0, 269, 240, 61);
            Label("Subtitle", window.transform, font, "拖拽物品可整理背包或上下架", Ink, 19, 0, 214, 464, 33);
            inventory.BackpackSlots = new StockSlotView[InventoryStock.BackpackCapacity];
            for (int i = 0; i < inventory.BackpackSlots.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(slot, window.transform);
                instance.name = "Backpack Slot " + (i + 1);
                SetRect((RectTransform)instance.transform, (i % 5 - 2) * 99, 143 - i / 5 * 99, 93, 93);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                inventory.BackpackSlots[i] = instance.GetComponent<StockSlotView>();
            }

            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/WoodButton.prefab");
            var close = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, window.transform);
            close.name = "Close Backpack";
            SetRect((RectTransform)close.transform, 0, -267, 244, 56);
            var closeText = close.GetComponentInChildren<TMP_Text>();
            closeText.rectTransform.sizeDelta = new Vector2(244, 56);
            closeText.font = font;
            closeText.text = "关闭背包";
            foreach (var component in new UnityEngine.Object[] { close, close.transform, closeText.rectTransform, closeText })
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            inventory.CloseButton = close.GetComponent<Button>();

            var tooltip = Picture("Item Tooltip", root, parchment, 0, 0, 444, 317);
            tooltip.preserveAspect = false;
            overlay.Tooltip = tooltip.gameObject.AddComponent<CanvasGroup>();
            overlay.Tooltip.alpha = 0;
            overlay.Tooltip.blocksRaycasts = overlay.Tooltip.interactable = false;
            overlay.TooltipIcon = Picture("Icon", tooltip.transform, null, -130, 62, 82, 82);
            overlay.TooltipName = Label("Name", tooltip.transform, font, "", Ink, 28, 43, 83, 241, 45);
            overlay.TooltipMeta = Label("Quality and Quantity", tooltip.transform, font, "", Ink, 18, 43, 47, 241, 32);
            overlay.TooltipDescription = Label("Description", tooltip.transform, font, "", Ink, 20, 0, -22, 348, 83);
            overlay.TooltipPrice = Label("Prices", tooltip.transform, font, "", Ink, 18, 0, -97, 348, 34);

            var toast = Picture("Drop Status", root, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Button.png"), -360, -337, 445, 64);
            toast.preserveAspect = false;
            overlay.Status = toast.gameObject.AddComponent<CanvasGroup>();
            overlay.Status.alpha = 0;
            overlay.Status.blocksRaycasts = overlay.Status.interactable = false;
            overlay.StatusText = Label("Message", toast.transform, font, "", Ivory, 24, 0, 0, 395, 48);
            overlay.DragIcon = Picture("Drag Preview", root, null, 0, 0, 96, 96);
            overlay.DragIcon.color = new Color(1, 1, 1, 0.65f);
            overlay.DragIcon.enabled = false;
            return overlay;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            SetRect(rect, x, y, width, height);
            return rect;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Image Picture(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value, Color color,
            float size, float x, float y, float w, float h)
        {
            var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
