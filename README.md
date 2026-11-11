# InventoryShop · 背包商店经营

在雾灯杂货铺接待旅人。观察订单，从批发商进货，把货物拖上货架，再把商品卖给顾客。使用 Unity 2022.3.55f1c1、uGUI、TextMeshPro、C# 和本地 JSON，面向 Windows x86_64。

## 游戏规则

- 开局有 500 金币，背包和货架为空。正式版取消了赠送商品，因此初始库存成本为 0。累计净利润始终等于累计销售收入减去累计采购支出。未卖出的采购也计入当期支出。
- 每天有五位顾客。订单随机安排，包含两颗神秘宝石、两份魔法卷轴、一把冒险匕首，以及两份随机商品。顾客没有等待时限。必须成交或确认拒绝全部五单才能结算。
- 第五天结算时，累计净利润达到 1000 金币即胜利。胜负只判定一次。结果页可重新挑战，或带着已有库存和金币进入第六天继续自由经营。
- 三份固定商品订单的日利润合计 320 金币，另两单每单利润至少 7 金币。逐单按需进货时，最大单笔采购为 300 金币，500 启动金币足够。五天全部成交的利润下限为 1670 金币。
- 需要复现时，在新 Play 会话前给 `CustomerRoster.asset` 勾选 **Use Fixed Seed** 并填写 **117**。购买当前订单需要的数量并全部成交可胜利。每天拒绝五单且不采购，五天后利润为 0，进入失败结果。

## 操作

1. 从主菜单选择新游戏或继续游戏。损坏的存档会显示提示并禁用继续游戏。
2. 点击采购，滚动左侧商品列表，在右侧调整数量并确认。采购先进背包。采购前会同时验证金币和容量。
3. 打开背包，把商品拖上货架。可以移动到空格、合并同类堆叠，或在同一区域交换不同商品。悬停查看价格与数量。库存不足时可以先补货再成交。
4. 点击订单卡的完成交易或确认拒绝。完成五单后点击结束营业，查看当天采购、销售、净利润和订单统计。第五天可查看挑战结果，其他天可直接进入下一天。
5. 经营统计可查看累计金额、今日进度和挑战目标。设置里可调整音量。商店设置里的保存游戏会写入当前进度，每日结算、进入结果页及开启下一天也会自动保存。

单槽 JSON 为 `Application.persistentDataPath/inventory-shop-v1.json`。Windows 默认在用户的 LocalLow 下，以公司名 InventoryShop 和游戏名称为目录。存档保存经营阶段、挑战结果、每日统计基线、当前订单以及背包和货架的物品 ID 与数量。新游戏建立独立的新会话，下一次手动或自动保存时更新单槽。

## 打开与构建

- 在 Unity Hub 用 **2022.3.55f1c1** 打开工程，打开 `Assets/Scenes/MainMenu.unity`，在 Game View 选择 16:9 并 Play。场景和资源引用已保存。
- 选择 **Tools → InventoryShop → Build Windows V1**。场景入口为 MainMenu，随后加载 Shop。Windows 构建输出到 `Builds/Windows/InventoryShop.exe`，默认窗口 1600×900、可调整大小，应用图标沿用钥匙与羽毛徽记。分发时保留 exe 同目录的数据与运行库。
- **Install V1 UI** 是从第四阶段场景迁移的工具。可重复执行。平时直接打开已配置场景。
- `Builds/` 在 Git 中忽略，`Builds/Portfolio/` 保存 Unity Windows Player 的实际画面。

## 主要代码

| 入口 | 职责 |
| --- | --- |
| `InventoryStock`、`ShopInventory` | 双区域库存、堆叠与拖放、库存界面绑定 |
| `ShopEconomy`、`DailyOrders` | 原子采购与出售、货架跨格扣除、每日订单推进 |
| `BusinessCycle`、`BusinessView` | 每日数据基线、五日判定、结算、统计和结果动画 |
| `GameSession`、`MenuFlow` | 会话切换、单槽 JSON 校验、临时文件写入和存档恢复 |
| `ShopScreen`、`FadePanel` | 场景过渡、模态窗口焦点与射线管理 |
| `GameAudio`、`VolumeControls` | 跨场景背景循环、短音效与本地音量 |
| `ReleaseInstaller` | 场景增量绑定、发布设置和 Windows 构建 |
| `ReleaseSmoke` | 使用独立存档文件的 Windows 自动化验证 |

美术沿用项目已有的 ImageGen 奇幻商店插画、物品和头像。应用图标复用原有徽记。中文字体为 Noto Sans CJK SC 子集，授权在 `Assets/InventoryShop/Fonts/OFL.txt`。音效和背景音乐为本项目原创程序合成 WAV，位于 `Assets/InventoryShop/Resources/ShopAudio`。

## 回归验证

`ReleaseInstaller.BuildSmoke` 构建带验证代码的 Windows Player，需设置 `INVENTORY_BUILD_PATH` 为测试 exe 的绝对路径。依次以 `-inventory-win`、`-inventory-restore`、`-inventory-fail`、`-inventory-corrupt` 启动。测试使用独立的 smoke 存档。正式发布使用 **Build Windows V1**。

最终运行结果为 199 项断言通过，四个 Windows 测试进程均以 0 退出。种子 117 的胜利流程在第 1 天额外采购并保留 1 件木材，第 5 天实际净利润为 2051。全拒绝流程为 0。另验算 100 个固定种子，按单进货均可支付且达到目标，最低利润 1812。已验证实际 UI 采购、拖拽上架和出售，跨日保留库存，结算自动保存，独立进程恢复，以及损坏存档的禁用提示。1280×720、1600×900、1920×1080 的商店按钮与货架可命中。

正式 Windows x86_64 构建成功，并启动验证。自动验证使用 EventSystem 模拟输入。实体鼠标手感、不同音频设备的听感和非 16:9 窗口属于人工验收项目。
