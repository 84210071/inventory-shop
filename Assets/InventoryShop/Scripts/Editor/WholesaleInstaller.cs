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
    public static class WholesaleInstaller
    {
        private const string Root = "Assets/InventoryShop/";
        private const string ShopPath = "Assets/Scenes/Shop.unity";
        private static readonly Color Ivory = new Color32(255, 233, 177, 255);
        private static readonly Color Ink = new Color32(63, 34, 20, 255);

        [MenuItem("Tools/InventoryShop/Install Wholesale UI")]
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
                if (inventory.GetComponentInChildren<WholesalePanel>(true) != null)
                {
                    Debug.Log("InventoryShop wholesale UI is already installed.");
                    return;
                }

                foreach (string name in new[] { "ManaPotion", "Timber", "IronOre", "Dagger", "SilverNecklace" })
                    ImportIcon(name);
                var font = LoadFont("WholesaleChinese", "WholesaleGlyphs");
                var catalog = CreateCatalog();
                var row = CreateCard(font);
                BuildPanel(inventory, catalog, row, font);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Shop scene.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ImportIcon(string name)
        {
            string path = Root + "Art/Icons/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency &&
                importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static WholesaleCatalog CreateCatalog()
        {
            var items = new[]
            {
                Define("ember-potion", "小型治疗药水", "普通", "以晨露和火莓熬制的温和药剂，适合冒险途中随身携带。", "Potion", 20, 35, 10),
                Define("mana-potion", "魔力药水", "精良", "深蓝药液泛着细小的光点，是施法者常备的补给。", "ManaPotion", 25, 40, 10),
                Define("timber", "木材", "普通", "纹理紧实的木段，晾干后可用于修缮和制作。", "Timber", 8, 15, 20),
                Define("iron-ore", "铁矿石", "普通", "灰黑岩块上散着银灰色矿斑，铁匠会买下它。", "IronOre", 15, 28, 20),
                Define("adventurer-dagger", "冒险匕首", "精良", "皮革缠柄的小巧匕首，适合绑在旅行者的腰带上。", "Dagger", 70, 110, 1),
                Define("traveler-scroll", "魔法卷轴", "精良", "红绳束起的古老卷轴，纸面绘着细密的符文。", "Scroll", 80, 130, 5),
                Define("silver-necklace", "银制项链", "稀有", "银链垂着一枚弯月吊坠，在灯光下闪烁。", "SilverNecklace", 120, 200, 1),
                Define("frost-crystal", "神秘宝石", "稀有", "晶石内部仿佛封存着一缕蓝光，来历无人知晓。", "Crystal", 150, 240, 5)
            };
            const string path = Root + "Data/WholesaleCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<WholesaleCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WholesaleCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("items");
            entries.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static ItemDefinition Define(string id, string name, string quality, string description,
            string icon, int purchase, int sale, int limit)
        {
            string path = Root + "Data/" + id + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            var data = new SerializedObject(item);
            data.FindProperty("id").stringValue = id;
            data.FindProperty("displayName").stringValue = name;
            data.FindProperty("quality").stringValue = quality;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("icon").objectReferenceValue = Art("Icons/" + icon);
            data.FindProperty("purchasePrice").intValue = purchase;
            data.FindProperty("salePrice").intValue = sale;
            data.FindProperty("stackLimit").intValue = limit;
            data.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static GameObject CreateCard(TMP_FontAsset font)
        {
            var node = Rect("WholesaleCard", null, 0, 0, 530, 90);
            try
            {
                var image = node.gameObject.AddComponent<Image>();
                image.sprite = Art("UI/Button");
                var button = node.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var outline = node.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(243, 191, 93, 245);
                outline.effectDistance = new Vector2(3, 3);
                outline.enabled = false;
                var feedback = node.gameObject.AddComponent<ButtonFeedback>();
                feedback.Surface = image;
                feedback.Normal = image.sprite;
                feedback.Hover = Art("UI/ButtonHover");
                feedback.Pressed = Art("UI/ButtonPressed");
                feedback.SelectionOutline = outline;

                var card = node.gameObject.AddComponent<WholesaleCard>();
                card.Button = button;
                card.Feedback = feedback;
                card.Icon = Picture("Icon", node, null, -208, 0, 72, 72);
                card.Title = Label("Name", node, font, "", Ivory, 26, -15, 15, 290, 41);
                card.Quality = Label("Quality", node, font, "", Ivory, 19, -15, -18, 290, 29);
                card.Title.alignment = card.Quality.alignment = TextAlignmentOptions.MidlineLeft;
                card.UnitPrice = Label("Price", node, font, "", Ivory, 22, 169, 0, 139, 40);
                return PrefabUtility.SaveAsPrefabAsset(node.gameObject, Root + "Prefabs/UI/WholesaleCard.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(node.gameObject); }
        }

        private static void BuildPanel(ShopInventory inventory, WholesaleCatalog catalog, GameObject row, TMP_FontAsset font)
        {
            var screen = inventory.Screen;
            var content = screen.transform.Find("Screen");
            var shade = Picture("Wholesale Modal", screen.transform, null, 0, 0, 0, 0);
            Stretch(shade.rectTransform);
            shade.rectTransform.SetSiblingIndex(screen.Settings.transform.GetSiblingIndex());
            shade.color = new Color(0.07f, 0.036f, 0.02f, 0.82f);
            shade.raycastTarget = true;
            var view = shade.gameObject.AddComponent<WholesalePanel>();
            view.Screen = screen;
            view.Inventory = inventory;
            view.Catalog = catalog;
            view.Modal = shade.gameObject.AddComponent<CanvasGroup>();
            view.Modal.alpha = 0;
            view.Modal.interactable = view.Modal.blocksRaycasts = false;
            view.Panel = Rect("Wholesale Panels", shade.transform, 0, 0, 1280, 880);
            view.OpenButton = content.Find("Shop Action 2").GetComponent<Button>();
            ClearClick(view.OpenButton);
            view.TopGold = content.Find("Balance").GetComponent<TMP_Text>();
            foreach (var text in new[]
            {
                screen.ProductName, screen.Quality, screen.Description, screen.Price,
                inventory.Overlay.TooltipName, inventory.Overlay.TooltipMeta,
                inventory.Overlay.TooltipDescription, inventory.Overlay.TooltipPrice
            }.Concat(inventory.ShelfSlots.Select(slot => slot.Caption)))
            {
                text.font = font;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            var parchment = Art("UI/Panel");
            var list = Picture("Merchant List", view.Panel, parchment, -292, 0, 687, 880);
            list.preserveAspect = false;
            Label("Title", list.transform, font, "批发商采购", Ink, 43, 0, 345, 450, 63);
            Label("Subtitle", list.transform, font, "挑选货品，补充店铺库存", Ink, 23, 0, 286, 500, 40);
            Label("List Footer", list.transform, font, "滚动查看全部货品", Ink, 22, 0, -348, 500, 38);
            view.Cards = BuildList(list.transform, row, catalog.Items.Count);

            var detail = Picture("Order Details", view.Panel, parchment, 346, 0, 591, 880);
            detail.preserveAspect = false;
            Picture("Coin", detail.transform, Art("Icons/Coin"), -171, 338, 43, 43);
            view.Gold = Label("Gold", detail.transform, font, "500", Ink, 29, -96, 338, 104, 49);
            view.CloseButton = Wood(detail.transform, font, "Close Wholesale", "关闭", 156, 339, 142, 54, 25);
            view.ItemName = Label("Item Name", detail.transform, font, "", Ink, 37, 0, 248, 460, 62);
            view.ItemIcon = Picture("Item Icon", detail.transform, null, 0, 137, 145, 145);
            view.Quality = Label("Quality", detail.transform, font, "", Ink, 24, 0, 43, 400, 39);
            view.Description = Label("Description", detail.transform, font, "", Ink, 23, 0, -16, 445, 74);
            view.ItemTerms = Label("Terms", detail.transform, font, "", Ink, 20, 0, -80, 433, 36);
            view.MinusButton = Wood(detail.transform, font, "Decrease Quantity", "减", -124, -153, 94, 53, 25);
            view.Quantity = Label("Quantity", detail.transform, font, "1", Ink, 31, 0, -153, 100, 49);
            view.PlusButton = Wood(detail.transform, font, "Increase Quantity", "加", 124, -153, 94, 53, 25);
            view.Total = Label("Total", detail.transform, font, "", Ink, 28, 0, -216, 445, 49);
            view.BuyButton = Wood(detail.transform, font, "Buy Stock", "购买", 0, -277, 290, 62, 29);

            var toast = Picture("Purchase Feedback", detail.transform, Art("UI/Button"), 0, -351, 418, 57);
            toast.preserveAspect = false;
            view.Status = Label("Status", toast.transform, font, "", Ivory, 22, 0, 0, 380, 42);
            view.Message = toast.gameObject.AddComponent<CanvasGroup>();
            view.Message.alpha = 0;
            view.Message.blocksRaycasts = view.Message.interactable = false;
        }

        private static WholesaleCard[] BuildList(Transform parent, GameObject row, int count)
        {
            var frame = Rect("Catalog Scroll View", parent, -5, -19, 572, 535);
            var scroll = frame.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", frame, -9, 0, 552, 535);
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Catalog Rows", viewport, 0, 0, 552, count * 99 + 16);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 52;

            var rail = Picture("Catalog Scrollbar", frame, Art("UI/Slot"), 278, 0, 12, 532);
            rail.preserveAspect = false;
            rail.raycastTarget = true;
            var bar = rail.gameObject.AddComponent<Scrollbar>();
            var slide = Rect("Sliding Area", rail.transform, 0, 0, 0, 0);
            Stretch(slide);
            var handle = Picture("Thumb", slide, Art("UI/ButtonHover"), 0, 0, 12, 80);
            Stretch(handle.rectTransform);
            handle.preserveAspect = false;
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.size = 0.55f;
            bar.value = 1;
            scroll.verticalScrollbar = bar;

            var cards = new WholesaleCard[count];
            for (int i = 0; i < count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(row, content);
                instance.name = "Wholesale Card " + (i + 1);
                var rect = (RectTransform)instance.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(0, -53 - 99 * i);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                cards[i] = instance.GetComponent<WholesaleCard>();
            }
            scroll.verticalNormalizedPosition = 1;
            return cards;
        }

        private static Button Wood(Transform parent, TMP_FontAsset font, string name, string caption,
            float x, float y, float width, float height, float fontSize)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/WoodButton.prefab");
            var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            node.name = name;
            SetRect((RectTransform)node.transform, x, y, width, height);
            var label = node.GetComponentInChildren<TMP_Text>();
            Stretch(label.rectTransform);
            label.font = font;
            label.fontSize = fontSize;
            label.text = caption;
            foreach (var changed in new UnityEngine.Object[] { node, node.transform, label.rectTransform, label })
                PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            return node.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite Art(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/" + path + ".png")
            ?? throw new FileNotFoundException(path);
    }
}
