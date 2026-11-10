# InventoryShop

面向 PC 的 Unity 2D 奇幻杂货铺经营游戏。核心操作在 uGUI 中完成，文本使用 TextMeshPro。当前提交是项目初始化基线。

## 环境与能力

- 使用 Unity Hub 打开项目根目录，编辑器版本为 `2022.3.55f1c1`。
- 项目已声明 uGUI `1.0.0` 和 TextMeshPro `3.0.7`。首次创建 TMP 文本时，如提示缺少基础资源，执行 **Window → TextMeshPro → Import TMP Essential Resources**。
- 当前工作环境提供图片生成工具。本阶段尚未生成或导入正式美术。进入界面制作阶段后，将生成的背景、面板纹理、物品图标和装饰 PNG 存入下方对应目录。
- Unity 编辑器已安装。后续使用 `Assets/InventoryShop/Scripts/Editor` 下的生成器，通过 `MenuItem` 在编辑器内执行，并提供可供 `-batchmode -executeMethod` 调用的静态入口。批处理使用未被其他 Unity 实例打开的项目副本。

## 资源目录

| 路径（相对于 Assets） | 用途 |
| --- | --- |
| `Scenes/` | Unity 场景，当前保留模板的 SampleScene |
| `InventoryShop/Art/Backgrounds/` | 主菜单和商店背景 |
| `InventoryShop/Art/Icons/` | 物品、金币及操作图标 |
| `InventoryShop/Art/UI/` | 羊皮纸、木框、按钮纹理和装饰 |
| `InventoryShop/Fonts/` | 项目字体、TMP 字体资产及授权文件 |
| `InventoryShop/Animations/UI/` | UI 动画资产 |
| `InventoryShop/Prefabs/UI/` | 按钮、弹窗、面板及货架卡片 |
| `InventoryShop/Scripts/Runtime/UI/` | 界面控制器、导航及交互反馈 |
| `InventoryShop/Scripts/Editor/` | 场景与 Prefab 生成工具 |

空目录用 `.gitkeep` 保留。新增或移动 Unity 资源时一起提交对应的 `.meta` 文件。

## 下一阶段的界面方案

1. **公共基础**。生成屏幕空间 Canvas、GraphicRaycaster 和一个 EventSystem。Canvas Scaler 使用 Scale With Screen Size，参考分辨率为 1920×1080，宽高匹配值为 0.5。使用锚点及布局组件验证常见横屏分辨率。共享按钮、面板和排版规则，使用暖木纹、羊皮纸、暗金描边与深棕色文字。面板采用可九宫格拉伸的纹理，中文 TMP 字体确认覆盖所需字形。
2. **主菜单**。以暖光杂货铺插画作为背景，标题和操作集中在可读性稳定的羊皮纸区域。“进入商店”打开商店场景，“退出”结束桌面版应用。将主菜单设为首个构建场景，验证鼠标与键盘导航。
3. **商店主界面**。顶部保留店名、金币展示和返回入口，中央放货架展示区，侧边放物品详情面板。使用演示内容验证布局、信息层级和详情切换。背包、采购和顾客订单安排在后续独立阶段。
4. **反馈和过渡**。按钮支持悬停、键盘选中、按压和释放反馈。场景切换使用短淡出，详情面板使用 CanvasGroup 淡入淡出，隐藏时关闭射线拦截。动画使用非缩放时间，避免暂停状态下卡住。
5. **自动化与验收**。生成器通过 AssetDatabase、PrefabUtility 和 EditorSceneManager 写入明确的资源路径，重复运行时避免重复对象或覆盖手工资产。先验证场景互相可达、控制台无报错、长文本不会溢出，再验收视觉和交互。

## 验证当前初始化

在 Unity Hub 中使用指定版本打开仓库，等待导入完成。在 Project 窗口确认 `Assets/InventoryShop` 的目录可见，在 Package Manager 中核对 uGUI 和 TextMeshPro。打开 `Assets/Scenes/SampleScene.unity` 并进入 Play Mode，检查 Console 中有无导入或编译错误。此时运行的是 Unity 模板场景，主菜单和商店界面将在后续阶段加入。
