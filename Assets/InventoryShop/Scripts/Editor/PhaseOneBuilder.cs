using System;
using System.IO;
using System.Linq;
using InventoryShop;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace InventoryShop.Editor
{
    public static class PhaseOneBuilder
    {
        private const string Root = "Assets/InventoryShop/";
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string ShopPath = "Assets/Scenes/Shop.unity";
        private static readonly Color Ivory = new Color32(255, 235, 185, 255);
        private static readonly Color Ink = new Color32(64, 34, 21, 255);

        [MenuItem("Tools/InventoryShop/Generate Phase One")]
        public static void Generate()
        {
            if (Application.isBatchMode && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            if (SceneManager.GetSceneByPath(MenuPath).isLoaded || SceneManager.GetSceneByPath(ShopPath).isLoaded)
                throw new InvalidOperationException("Close MainMenu and Shop before regenerating them.");

            string[] textures =
            {
                "Backgrounds/MainMenu", "Backgrounds/Shop", "UI/Panel",
                "UI/Button", "UI/ButtonHover", "UI/ButtonPressed", "UI/Slot", "UI/SlotHover", "UI/SlotPressed",
                "Icons/Potion", "Icons/Scroll", "Icons/Crystal", "Icons/Herb", "Icons/Coin", "Icons/Emblem"
            };
            foreach (string name in textures)
            {
                var importer = AssetImporter.GetAtPath(Root + "Art/" + name + ".png") as TextureImporter;
                if (importer == null) throw new FileNotFoundException("Missing texture " + name);
                if (importer.textureType == TextureImporterType.Sprite &&
                    importer.spriteImportMode == SpriteImportMode.Single &&
                    importer.alphaIsTransparency && !importer.mipmapEnabled &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var font = LoadFont();
            var button = SaveControl("WoodButton", "Button", font);
            var slot = SaveControl("ShelfSlot", "Slot", font);
            BuildScene(MenuPath, font, button, slot);
            BuildScene(ShopPath, font, button, slot);
            var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != MenuPath && s.path != ShopPath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(ShopPath, true)
            }.Concat(otherScenes).ToArray();
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset LoadFont()
        {
            const string path = Root + "Fonts/ShopChinese.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                if (existing.material == null || existing.atlasTexture == null)
                    throw new InvalidOperationException("ShopChinese.asset is incomplete. Restore the generated font asset before proceeding.");
                return existing;
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/ShopChinese.otf");
            var font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (font == null) throw new InvalidOperationException("Check the TMP resources and Chinese source font.");
            string characters = File.ReadAllText(Root + "Fonts/Glyphs.txt");
            if (!font.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("Missing glyphs: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            var material = font.material;
            var atlas = font.atlasTexture;
            material.name = "Shop Chinese SDF Material";
            atlas.name = "Shop Chinese SDF Atlas";
            atlas.Apply(false, true);
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(atlas, font);
            AssetDatabase.AddObjectToAsset(material, font);
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static GameObject SaveControl(string name, string texture, TMP_FontAsset font)
        {
            var rect = Rect(name, null, 0, 0, 360, 80);
            try
            {
                var image = rect.gameObject.AddComponent<Image>();
                var feedback = rect.gameObject.AddComponent<ButtonFeedback>();
                feedback.Surface = image;
                feedback.Normal = Art("UI/" + texture);
                feedback.Hover = Art("UI/" + texture + "Hover");
                feedback.Pressed = Art("UI/" + texture + "Pressed");
                image.sprite = feedback.Normal;
                var button = rect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = image;
                if (texture == "Button")
                    Label("Label", rect, font, "确认", Ivory, 29, 0, 0, 360, 64);
                else
                {
                    var outline = rect.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color32(239, 185, 91, 210);
                    outline.effectDistance = new Vector2(3, 3);
                    outline.enabled = false;
                    feedback.SelectionOutline = outline;
                }
                return PrefabUtility.SaveAsPrefabAsset(rect.gameObject, Root + "Prefabs/UI/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(rect.gameObject); }
        }

        private static void BuildScene(string path, TMP_FontAsset font, GameObject button, GameObject slot)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var camera = new GameObject("UI Camera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 0;
                camera.gameObject.tag = "MainCamera";
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                var canvas = new GameObject("InventoryShop Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                bool menu = path == MenuPath;
                var backdrop = ImageAt("Illustration", canvas.transform, Art(menu ? "Backgrounds/MainMenu" : "Backgrounds/Shop"), 0, 0, 1920, 1080);
                Stretch(backdrop.rectTransform);
                backdrop.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                backdrop.GetComponent<AspectRatioFitter>().aspectRatio = 16f / 9f;
                if (!menu) backdrop.color = new Color(0.62f, 0.53f, 0.46f);
                var content = Rect("Screen", canvas.transform, 0, 0, 1920, 1080);
                var screen = canvas.AddComponent<ShopScreen>();

                if (menu) BuildMenu(content, font, button, screen);
                else BuildShop(content, font, button, slot, screen);

                screen.Settings = Modal("Settings", canvas.transform, font, button, screen, "设置",
                    "界面设置\n当前使用全屏比例自适应布局", out _, out screen.SettingsClose);
                screen.Notice = Modal("Notice", canvas.transform, font, button, screen, "店铺告示",
                    "功能开发中", out screen.NoticeText, out screen.NoticeClose);
                var curtain = Shade("Scene Curtain", canvas.transform, Color.black);
                screen.Curtain = curtain.gameObject.AddComponent<CanvasGroup>();

                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("Could not save " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildMenu(RectTransform root, TMP_FontAsset font, GameObject button, ShopScreen screen)
        {
            var panel = ImageAt("Menu Parchment", root, Art("UI/Panel"), -520, 0, 690, 788);
            Label("Kicker", panel.transform, font, "雾灯杂货铺", Ink, 28, 0, 260, 530, 45);
            ImageAt("Shop Emblem", panel.transform, Art("Icons/Emblem"), 0, 165, 125, 125, true);
            Label("Title", panel.transform, font, "背包商店经营", Ink, 72, 0, 62, 590, 118);
            Label("Subtitle", panel.transform, font, "一盏灯，一间铺，一段旅程。", Ink, 26, 0, -22, 530, 50);
            screen.FirstButton = Wood(button, panel.transform, "Enter Shop", "进入商店", 0, -110, 390, 78, screen.EnterShop);
            Wood(button, panel.transform, "Settings", "设置", 0, -206, 390, 78, screen.OpenSettings);
            Wood(button, panel.transform, "Quit", "退出游戏", 0, -302, 390, 78, screen.Quit);
        }

        private static void BuildShop(RectTransform root, TMP_FontAsset font, GameObject button, GameObject slot, ShopScreen screen)
        {
            Label("Shop Name", root, font, "雾灯杂货铺", Ivory, 52, -665, 451, 450, 82);
            ImageAt("Coin", root, Art("Icons/Coin"), 245, 450, 54, 54, true);
            Label("Balance", root, font, "500", Ivory, 36, 324, 450, 96, 55);
            Label("Day", root, font, "第 1 天", Ivory, 33, 481, 450, 178, 55);
            Wood(button, root, "Settings", "设置", 654, 450, 140, 60, screen.OpenSettings);
            Wood(button, root, "Return", "返回主菜单", 815, 450, 165, 60, screen.ReturnToMenu);

            Label("Shelf Heading", root, font, "店内货架", Ivory, 39, -371, 341, 440, 62);
            var shelf = Rect("Shelf", root, -365, -25, 1040, 650);
            screen.Products = new[]
            {
                Product("赤焰药水", "精良", "以火莓和晨露熬制。玻璃瓶触手微温，适合寒夜里的远行者。", 45, "Potion"),
                Product("旅人卷轴", "普通", "一卷束以红绳的路书。边角标记着旅人们熟知的歇脚处。", 28, "Scroll"),
                Product("霜蓝晶石", "稀有", "北境矿脉出产的晶石。光照下，晶面浮起浅蓝的波纹。", 120, "Crystal"),
                Product("月露草", "普通", "在月光下采摘的香草。晾干后仍带着雨后森林的清香。", 18, "Herb")
            };
            for (int index = 0; index < 6; index++)
            {
                var cell = Place(slot, shelf, "Shelf Slot " + (index + 1), (index % 3 - 1) * 323, 157 - index / 3 * 313, 282, 282);
                if (index >= screen.Products.Length)
                {
                    cell.gameObject.GetComponent<Image>().raycastTarget = false;
                    cell.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(cell);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(cell.GetComponent<Image>());
                    Label("Empty", cell.transform, font, "空货位", Ivory, 24, 0, -71, 220, 45);
                    continue;
                }

                int productIndex = index;
                var product = screen.Products[index];
                product.Slot = cell.GetComponent<ButtonFeedback>();
                ImageAt("Product Art", cell.transform, product.Icon, 0, 21, 166, 166, true);
                Label("Product Label", cell.transform, font, product.Name, Ivory, 29, 0, -88, 238, 48);
                UnityEventTools.AddIntPersistentListener(cell.onClick, screen.ShowProduct, productIndex);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cell);
            }
            screen.FirstButton = screen.Products[0].Slot.GetComponent<Button>();

            var idle = ImageAt("Ledger Back", root, Art("UI/Panel"), 563, -9, 555, 704);
            ImageAt("Ledger Emblem", idle.transform, Art("Icons/Emblem"), 0, 60, 160, 160, true);
            Label("Ledger", idle.transform, font, "商品图鉴", Ink, 42, 0, -61, 410, 64);
            Label("Hint", idle.transform, font, "选择货架商品查看详情", Ink, 25, 0, -131, 410, 48);
            var detail = ImageAt("Product Details", root, Art("UI/Panel"), 563, -9, 555, 704);
            screen.Details = detail.gameObject.AddComponent<FadePanel>();
            screen.Details.SetImmediate(false);
            screen.ProductName = Label("Name", detail.transform, font, "", Ink, 44, 0, 238, 450, 65);
            screen.ProductIcon = ImageAt("Icon", detail.transform, null, 0, 95, 190, 190, true);
            screen.Quality = Label("Quality", detail.transform, font, "", new Color32(113, 68, 28, 255), 27, 0, -23, 430, 52);
            screen.Description = Label("Description", detail.transform, font, "", Ink, 26, 0, -113, 405, 112);
            ImageAt("Coin", detail.transform, Art("Icons/Coin"), -78, -195, 35, 35, true);
            screen.Price = Label("Price", detail.transform, font, "", Ink, 29, 24, -195, 155, 48);
            Wood(button, detail.transform, "Close Details", "合上图鉴", 0, -268, 235, 59, screen.CloseDetails);

            string[] entries = { "背包", "采购", "经营统计", "结束营业" };
            for (int i = 0; i < entries.Length; i++)
                Wood(button, root, "Shop Action " + (i + 1), entries[i], (i - 1.5f) * 347, -447, 315, 77, screen.ShowComingSoon);
        }

        private static ShopScreen.Product Product(string name, string quality, string description, int price, string icon)
            => new ShopScreen.Product { Name = name, Quality = quality, Description = description, Price = price, Icon = Art("Icons/" + icon) };

        private static FadePanel Modal(string name, Transform parent, TMP_FontAsset font, GameObject button,
            ShopScreen screen, string heading, string body, out TMP_Text text, out Button close)
        {
            var shade = Shade(name, parent, new Color(0.09f, 0.05f, 0.03f, 0.82f));
            var fade = shade.gameObject.AddComponent<FadePanel>();
            var panel = ImageAt("Parchment", shade.transform, Art("UI/Panel"), 0, 0, 620, 490);
            Label("Heading", panel.transform, font, heading, Ink, 47, 0, 123, 470, 76);
            text = Label("Body", panel.transform, font, body, Ink, 29, 0, 12, 460, 119);
            close = Wood(button, panel.transform, "Close", "关闭", 0, -128, 280, 68, screen.CloseModal);
            close.navigation = new Navigation { mode = Navigation.Mode.None };
            PrefabUtility.RecordPrefabInstancePropertyModifications(close);
            fade.SetImmediate(false);
            return fade;
        }

        private static Button Wood(GameObject prefab, Transform parent, string name, string text,
            float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        {
            var button = Place(prefab, parent, name, x, y, w, h);
            var label = button.GetComponentInChildren<TMP_Text>();
            Stretch(label.rectTransform);
            label.text = text;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            return button;
        }

        private static Button Place(GameObject prefab, Transform parent, string name, float x, float y, float w, float h)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            SetRect((RectTransform)instance.transform, x, y, w, h);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance.GetComponent<Button>();
        }

        private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string text,
            Color color, float size, float x, float y, float w, float h)
        {
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.color = color;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static Image ImageAt(string name, Transform parent, Sprite sprite,
            float x, float y, float w, float h, bool preserveAspect = false)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static Image Shade(string name, Transform parent, Color color)
        {
            var shade = ImageAt(name, parent, null, 0, 0, 0, 0);
            Stretch(shade.rectTransform);
            shade.color = color;
            shade.raycastTarget = true;
            return shade;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            SetRect(rect, x, y, w, h);
            return rect;
        }

        private static void SetRect(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite Art(string name)
            => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/" + name + ".png")
               ?? throw new FileNotFoundException("Missing sprite " + name);
    }
}
