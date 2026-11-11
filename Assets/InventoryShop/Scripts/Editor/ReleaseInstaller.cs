using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static InventoryShop.Editor.InventoryInstaller;
using static InventoryShop.Editor.WholesaleInstaller;

namespace InventoryShop.Editor
{
    public static class ReleaseInstaller
    {
        private const string Root = "Assets/InventoryShop/";
        public static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Shop.unity" };
        private static readonly Color Ink = new Color32(62, 35, 22, 255);
        private static readonly Color Gold = new Color32(255, 231, 177, 255);

        [MenuItem("Tools/InventoryShop/Install V1 UI")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var font = LoadFont("ReleaseChinese", "ReleaseGlyphs");
            var catalog = AssetDatabase.LoadAssetAtPath<WholesaleCatalog>(Root + "Data/WholesaleCatalog.asset");
            var roster = AssetDatabase.LoadAssetAtPath<CustomerRoster>(Root + "Data/CustomerRoster.asset");
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var screen = scene.GetRootGameObjects().Select(root => root.GetComponent<ShopScreen>()).First(s => s != null);
                var scaler = screen.GetComponent<CanvasScaler>();
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                if (screen.Settings.GetComponent<VolumeControls>() == null)
                {
                    AddSettings(screen, font);
                    if (path == Scenes[0]) AddMenu(screen, font, catalog, roster);
                    else AddBusiness(screen, font);
                }
                if (screen.Business != null) PolishShop(screen, font);
                EditorSceneManager.SaveScene(scene);
            }
            var stock = AssetDatabase.LoadAssetAtPath<StartingStock>(Root + "Data/StartingStock.asset");
            var data = new SerializedObject(stock);
            data.FindProperty("backpack").arraySize = 0;
            data.FindProperty("shelf").arraySize = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            PlayerSettings.companyName = "InventoryShop";
            PlayerSettings.productName = "InventoryShop 背包商店经营";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Art/Icons/Emblem.png");
            if (icon != null)
            {
                int count = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone).Length;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, Enumerable.Repeat(icon, count).ToArray());
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/InventoryShop/Build Windows V1")]
        public static void Build()
        {
            string path = System.Environment.GetEnvironmentVariable("INVENTORY_BUILD_PATH");
            if (string.IsNullOrEmpty(path)) path = "Builds/Windows/InventoryShop.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(Scenes, path, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Windows build failed: " + report.summary.result);
        }

        public static void BuildSmoke()
        {
            string path = System.Environment.GetEnvironmentVariable("INVENTORY_BUILD_PATH");
            if (string.IsNullOrEmpty(path)) throw new System.InvalidOperationException("Set INVENTORY_BUILD_PATH.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = path, target = BuildTarget.StandaloneWindows64,
                extraScriptingDefines = new[] { "INVENTORYSHOP_SMOKE" }
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("Smoke build failed.");
        }

        private static void PolishShop(ShopScreen screen, TMP_FontAsset font)
        {
            var view = screen.Business;
            if (view.ManualSaveStatus == null)
            {
                var close = (RectTransform)screen.SettingsClose.transform;
                close.anchoredPosition = new Vector2(0, -151);
                PrefabUtility.RecordPrefabInstancePropertyModifications(close);
                view.ManualSaveStatus = Label("Manual Save Status", close.parent, font, "", Ink, 19, 0, -98, 300, 29);
                EditorUtility.SetDirty(view);
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(view.Card))
            {
                const string prefab = Root + "Prefabs/UI/BusinessSummary.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(view.Card.gameObject, prefab, InteractionMode.AutomatedAction);
            }

            var tip = screen.Inventory.Overlay;
            tip.TooltipDescription.alignment = TextAlignmentOptions.TopLeft;
            tip.TooltipDescription.fontSize = 21;
            tip.TooltipPrice.fontStyle |= FontStyles.Bold;
            foreach (var card in screen.GetComponentInChildren<WholesalePanel>().Cards)
            {
                card.UnitPrice.color = Gold;
                PrefabUtility.RecordPrefabInstancePropertyModifications(card.UnitPrice);
            }
        }

        private static void AddMenu(ShopScreen screen, TMP_FontAsset font, WholesaleCatalog catalog, CustomerRoster roster)
        {
            var button = screen.FirstButton;
            var panel = button.transform.parent;
            var flow = screen.gameObject.AddComponent<MenuFlow>();
            flow.Screen = screen;
            flow.Catalog = catalog;
            flow.Roster = roster;
            flow.NewButton = button;
            ClearClick(button);
            MoveMenuButton(button, font, "新游戏", -88);
            flow.ContinueButton = Wood(panel, font, "Continue Game", "继续游戏", 0, -159, 390, 61, 27);
            MoveMenuButton(panel.Find("Settings").GetComponent<Button>(), font, "设置", -231);
            MoveMenuButton(panel.Find("Quit").GetComponent<Button>(), font, "退出游戏", -303);
            flow.SaveStatus = Label("Save Error", panel, font, "", Ink, 18, 0, -350, 500, 32);
        }

        private static void MoveMenuButton(Button button, TMP_FontAsset font, string title, int y)
        {
            var rect = (RectTransform)button.transform;
            SetRect(rect, 0, y, 390, 61);
            var label = button.GetComponentInChildren<TMP_Text>();
            label.font = font;
            label.text = title;
            label.fontSize = 27;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }

        private static void AddSettings(ShopScreen screen, TMP_FontAsset font)
        {
            var panel = screen.SettingsClose.transform.parent;
            var text = panel.GetComponentsInChildren<TMP_Text>().First(t => t.text.Contains("界面设置"));
            text.text = "";
            var controls = screen.Settings.gameObject.AddComponent<VolumeControls>();
            controls.Lower = Wood(panel, font, "Lower Volume", "减", -117, 22, 91, 51, 23);
            controls.Raise = Wood(panel, font, "Raise Volume", "加", 117, 22, 91, 51, 23);
            controls.Value = Label("Volume", panel, font, "", Ink, 24, 0, 22, 140, 40);
        }

        private static void AddBusiness(ShopScreen screen, TMP_FontAsset font)
        {
            var content = screen.transform.Find("Screen");
            var shade = Picture("Business Modal", screen.transform, null, 0, 0, 0, 0);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            shade.rectTransform.SetSiblingIndex(screen.Curtain.transform.GetSiblingIndex());
            shade.color = new Color(0.08f, 0.043f, 0.024f, 0.83f);
            shade.raycastTarget = true;
            shade.gameObject.AddComponent<CanvasGroup>();
            var modal = shade.gameObject.AddComponent<FadePanel>();
            modal.SetImmediate(false);

            var view = shade.gameObject.AddComponent<BusinessView>();
            screen.Business = view;
            view.Screen = screen;
            view.Customers = screen.Customers;
            view.Modal = modal;
            var panel = Picture("Business Parchment", shade.transform,
                AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Panel.png"), 0, 0, 788, 860);
            panel.preserveAspect = false;
            view.Card = panel.rectTransform;
            view.DayLabel = content.Find("Day").GetComponent<TMP_Text>();
            view.StatsButton = content.Find("Shop Action 3").GetComponent<Button>();
            ClearClick(view.StatsButton);
            view.Heading = Label("Business Title", panel.transform, font, "", Ink, 45, 0, 319, 530, 68);
            view.Subtitle = Label("Business Subtitle", panel.transform, font, "", Ink, 24, 0, 251, 555, 43);
            view.Crest = Picture("Result Crest", panel.transform,
                AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Icons/Emblem.png"), 0, 169, 90, 90);
            view.Rows = Label("Stat Labels", panel.transform, font, "", Ink, 27, -121, -29, 275, 324);
            view.Values = Label("Stat Values", panel.transform, font, "", Ink, 27, 134, -29, 250, 324);
            view.Rows.alignment = TextAlignmentOptions.TopLeft;
            view.Values.alignment = TextAlignmentOptions.TopRight;
            view.Rows.lineSpacing = view.Values.lineSpacing = 10;
            view.Rules = Label("Business Rules", panel.transform, font, "", Ink, 22, 0, -230, 620, 41);
            view.Close = Wood(panel.transform, font, "Close Business", "关闭", 0, -312, 250, 61, 27);
            view.Primary = Wood(panel.transform, font, "Business Primary", "下一天", 0, -310, 291, 61, 27);
            view.PrimaryText = view.Primary.GetComponentInChildren<TMP_Text>();
            view.Restart = Wood(panel.transform, font, "Restart Challenge", "重新开始", 0, -371, 239, 49, 22);
            view.SaveButton = Wood(screen.SettingsClose.transform.parent, font, "Save Game", "保存游戏", 0, -50, 215, 55, 25);
            view.SaveStatus = Label("Save Status", content, font, "", Gold, 21, -663, -367, 310, 37);
        }
    }
}
