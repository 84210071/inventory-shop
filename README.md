# InventoryShop · 背包商店经营

面向 PC 横屏的纯 UI 奇幻杂货铺。项目使用 Unity **2022.3.55f1c1**、uGUI **1.0.0** 和 TextMeshPro **3.0.7**。当前已接入第二阶段背包与货架库存。

## 运行第一阶段

1. 在 Unity Hub 中用项目指定版本打开仓库并等待资源导入。
2. 打开 `Assets/Scenes/MainMenu.unity`，将 Game View 设为 16:9 并点击 Play。
3. 测试设置和返回，再进入商店。点击四种商品查看图鉴，用底部背包入口打开库存，其他三个入口显示开发中提示，最后返回主菜单。
4. Windows 构建从 MainMenu 启动，Shop 排在第二。退出按钮在构建中关闭程序，在编辑器 Play Mode 中显示说明。

金币 500 和第 1 天仍为展示数据，四件商品已接入独立库存。设置面板可开关，尚无可保存的选项。

## 项目结构

| 目录 | 内容 |
| --- | --- |
| `Assets/Scenes` | MainMenu、Shop 和原有 SampleScene |
| `Assets/InventoryShop/Art` | 场景插画、界面纹理、透明商品图标 |
| `Assets/InventoryShop/Fonts` | 中文字体子集、字形清单、TMP SDF 资源及 OFL |
| `Assets/InventoryShop/Prefabs/UI` | WoodButton、ShelfSlot、BackpackSlot |
| `Assets/InventoryShop/Data` | 物品定义及开局库存 |
| `Assets/InventoryShop/Scripts/Runtime/UI` | 场景导航、面板动画、按钮和货架反馈 |
| `Assets/InventoryShop/Scripts/Editor` | 第一阶段资源生成器 |
| `Assets/TextMesh Pro` | Unity 随包提供的 TMP Essential Resources |

场景和 Prefab 已生成。第二阶段只在现有 Shop 场景中增量安装背包，MainMenu 和原美术未改动。**Tools → InventoryShop → Install Backpack UI** 用于迁移第一阶段的 Shop 场景，已安装时会直接返回。早期的 **Generate Phase One** 会重建第一阶段场景，仅用于第一阶段基线。
字体来自 Noto Sans CJK SC，按第一阶段文案子集化并改名为 InventoryShop Chinese，授权见 `Fonts/OFL.txt`。新增汉字时，同时扩充 `Glyphs.txt` 和字体源文件，并重建 `ShopChinese.asset`。已生成的静态图集覆盖当前界面和演示文案。

## 美术制作

使用内置 ImageGen 生成下列美术。插画裁切至 1920×1080。按钮和货格从原图派生亮色悬停、暗色按压版本。透明图集切分为六张保留 alpha 通道的 PNG。

| 输出 | 生成提示摘要 |
| --- | --- |
| MainMenu | 宽幅蓝色暮光下的奇幻杂货铺外观，右侧为暖黄色玻璃窗，左侧是暗色树林和石墙，精细手绘，无文字 |
| Shop | 暖色奇幻药剂铺内景，琥珀灯笼、木梁、边缘陈列药瓶和卷轴，中央压暗，无文字 |
| Panel | 竖向正视羊皮纸面板，深木框与古金角花，浅色纸面留空 |
| Button | 横向正视胡桃木按钮，古金斜边与角花，无文字 |
| Slot | 正视方形木质货架格，内侧压暗，黄铜边框和下缘台面，格内留空 |
| Potion、Scroll、Crystal、Herb、Coin、Emblem | 透明三列两行图集，红药水、卷轴、青色晶石、绿色香草、星纹金币、钥匙与羽毛徽章 |

## UI 代码入口

- `PhaseOneBuilder` 创建 Canvas、Canvas Scaler、EventSystem 和 Prefab 实例，用持久化 UnityEvent 绑定按钮与商品索引。
- `ShopScreen` 管理场景淡入淡出、模态面板和商品详情。用 CanvasGroup 控制遮罩的透明度与点击拦截。
- `ButtonFeedback` 从 EventSystem 获取悬停、焦点、按压和键盘提交事件，在 Update 中更新 Sprite 和缩放。
- `FadePanel` 用非缩放时间和协程渐变面板透明度，切换时中断旧协程。隐藏面板同时停用交互和射线拦截。

## 验证记录

- 使用同版本 Unity 在隔离副本生成场景和 Prefab，并完成 Windows x64 构建。
- Windows Player 烟雾测试覆盖主菜单与商店往返、悬停与按压、设置开关、四种商品详情、选中状态、四个开发中提示和退出。两个场景均在 1280×720、1600×900、1920×1080 检查控件边界与文字溢出。
- Unity Editor 批处理 Play Mode 验证了主菜单淡入及编辑器退出提示。原项目的交互式 Game View 截取超时，交付的截图来自 Unity Windows Player。

## 第二阶段库存规则

点击商店底部的背包按钮打开右侧 5×4 窗口。左侧货架仍可操作。悬停物品可看品质、采购价、售价和当前数量。关闭背包后，点击货架可继续使用原商品图鉴。

| 物品 | 采购价 | 售价 | 堆叠上限 | 背包开局 | 货架开局 |
| --- | ---: | ---: | ---: | --- | ---: |
| 赤焰药水 | 24 | 45 | 10 | 7、6 两叠 | 2 |
| 旅人卷轴 | 15 | 28 | 8 | 3 | 1 |
| 霜蓝晶石 | 75 | 120 | 5 | 2 | 1 |
| 月露草 | 9 | 18 | 20 | 12 | 4 |

在同一区域拖到不同物品上会交换位置。跨区域拖拽可放入空位或合并到同类堆叠。目标已满或被其他物品占用时会提示，超出同类堆叠上限的余量留在源格。库存跨主菜单往返保留，退出程序后重新使用开局配置。

- `ItemDefinition` 与 `StartingStock` 是 ScriptableObject。前者定义静态属性，后者指定开局两侧库存。
- `InventoryStock` 根据格位地址处理移动、交换和合并，统一更新两个格位后才通知 UI。
- `ShopInventory` 绑定背包和货架。每次拖拽只消费一次释放事件。`StockSlotView` 渲染模型。`InventoryOverlay` 负责弹窗、Tooltip、拖拽预览与提示。

第二阶段在独立 Unity 项目副本中完成 Windows x64 构建和运行检查。78 项断言通过，覆盖移动、交换、完全与部分合并、上架取回、满容量、无效拖放、逐物品数量守恒和场景往返。1280×720、1600×900、1920×1080 均检查了 26 个格子的射线可达性和 TMP 文字溢出。
