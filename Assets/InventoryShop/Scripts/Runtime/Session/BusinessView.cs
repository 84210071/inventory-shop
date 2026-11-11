using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class BusinessView : MonoBehaviour
    {
        public ShopScreen Screen;
        public CustomerOrdersView Customers;
        public FadePanel Modal;
        public RectTransform Card;
        public Button StatsButton, Primary, Close, Restart, SaveButton;
        public TMP_Text DayLabel, Heading, Subtitle, Rows, Values, PrimaryText, SaveStatus, ManualSaveStatus, Rules;
        public Image Crest;

        private ShopEconomy economy;
        private enum Page { Stats, Settlement, Result }
        private Page page;
        private float reveal;
        private bool resumeModal;
        private long[] amounts = new long[5];

        private void Start()
        {
            economy = Customers.Inventory.Economy;
            DayLabel.text = "第 " + economy.Day + " 天";
            Modal.SetImmediate(false);
            StatsButton.onClick.AddListener(() => Open(Page.Stats));
            Close.onClick.AddListener(Screen.CloseModal);
            Primary.onClick.AddListener(Proceed);
            Restart.onClick.AddListener(RestartGame);
            SaveButton.onClick.AddListener(() =>
            {
                bool saved = GameSession.Save();
                ManualSaveStatus.text = saved ? "游戏已保存" : "保存失败，请重试";
                GameAudio.Play(saved ? "Place" : "Error");
            });
            resumeModal = economy.Phase != BusinessPhase.Trading;
        }

        private void Update()
        {
            if (economy == null) return;
            if (resumeModal && Open(economy.Phase == BusinessPhase.Result ? Page.Result : Page.Settlement))
                resumeModal = false;
            if (!Screen.IsModalOpen(Modal)) return;
            reveal = Mathf.MoveTowards(reveal, 1f, Time.unscaledDeltaTime / 0.45f);
            float ease = 1f - Mathf.Pow(1f - reveal, 3);
            Card.localScale = Vector3.one * (0.92f + 0.08f * ease + Mathf.Sin(reveal * Mathf.PI) * 0.035f);
            Values.text = FormatValues(ease);
            Primary.interactable = reveal >= 1f;
            Restart.interactable = reveal >= 1f;
        }

        public void FinishDay()
        {
            if (!Screen.CanUseInventory || !economy.Settle()) return;
            SaveStatus.text = GameSession.Save() ? "游戏已保存" : "保存失败，请重试";
            Open(Page.Settlement);
            GameAudio.Play("Settlement");
        }

        private bool Open(Page next)
        {
            if (!Screen.CanUseInventory || (next == Page.Stats && economy.Phase != BusinessPhase.Trading)) return false;
            Customers.Inventory.SuspendForWholesale();
            Screen.CloseDetails();
            Present(next);
            Screen.OpenModal(Modal, next == Page.Stats ? Close : Primary);
            return true;
        }

        private void Present(Page next)
        {
            page = next;
            Heading.color = next == Page.Result ? (economy.Result == ChallengeResult.Won ? new Color32(101, 63, 20, 255) : new Color32(117, 48, 32, 255)) : new Color32(62, 35, 22, 255);
            reveal = 0;
            Values.text = "";
            Close.gameObject.SetActive(next == Page.Stats);
            Primary.gameObject.SetActive(next != Page.Stats);
            Restart.gameObject.SetActive(next == Page.Result);
            Crest.gameObject.SetActive(next == Page.Result);
            var day = economy.CustomerDay;
            if (next == Page.Stats)
            {
                Heading.text = "经营统计";
                Subtitle.text = "第 " + economy.Day + " 天  ·  " + (economy.Day > 5 ? "自由经营" : "五日挑战");
                Rows.text = "当前金币\n累计采购\n累计销售\n累计净利润\n目标进度\n今日成功\n今日拒绝";
                amounts[0] = economy.Gold;
                amounts[1] = economy.TotalPurchaseSpend;
                amounts[2] = economy.TotalSalesRevenue;
                amounts[3] = economy.NetProfit;
                amounts[4] = economy.NetProfit;
                Rules.text = "目标 1000 金币  ·  开局库存为空";
            }
            else if (next == Page.Settlement)
            {
                Heading.text = "当日结算";
                Subtitle.text = "第 " + economy.Day + " 天";
                Rows.text = "采购支出\n销售收入\n当日净利润\n累计净利润\n\n成功订单\n拒绝订单";
                amounts[0] = economy.DailyPurchases;
                amounts[1] = economy.DailySales;
                amounts[2] = economy.DailySales - economy.DailyPurchases;
                amounts[3] = economy.NetProfit;
                Rules.text = "净利润等于销售收入减去采购支出";
                PrimaryText.text = economy.Day == 5 ? "查看挑战结果" : "下一天";
            }
            else
            {
                bool won = economy.Result == ChallengeResult.Won;
                Heading.text = won ? "挑战胜利" : "挑战失败";
                Subtitle.text = won ? "五日经营目标已达成" : "五日经营目标未达成";
                Rows.text = "累计销售\n累计采购\n累计净利润\n挑战目标";
                amounts[0] = economy.TotalSalesRevenue;
                amounts[1] = economy.TotalPurchaseSpend;
                amounts[2] = economy.NetProfit;
                amounts[3] = ShopEconomy.ProfitTarget;
                Rules.text = "继续自由经营或重新开始挑战";
                PrimaryText.text = "继续自由经营";
            }
            Primary.interactable = Restart.interactable = false;
        }

        private string FormatValues(float ease)
        {
            string Value(int index) => System.Math.Round(amounts[index] * (double)ease).ToString();
            var day = economy.CustomerDay;
            string body = Value(0) + "\n" + Value(1) + "\n" + Value(2) + "\n" + Value(3);
            if (page == Page.Result) return body;
            return body + (page == Page.Stats ? "\n" + Value(4) + " / 1000" : "\n") +
                "\n" + day.Sold + "\n" + day.Rejected;
        }

        private void Proceed()
        {
            if (reveal < 1 || !Screen.IsModalOpen(Modal)) return;
            if (economy.ShowResult())
            {
                GameSession.Save();
                Present(Page.Result);
                GameAudio.Play(economy.Result == ChallengeResult.Won ? "Sale" : "Error");
                return;
            }
            if (!economy.Advance(Customers.Roster, Customers.Catalog)) return;
            DayLabel.text = "第 " + economy.Day + " 天";
            Customers.RefreshDay();
            Screen.CloseModal();
            SaveStatus.text = GameSession.Save() ? "游戏已保存" : "保存失败，请重试";
        }

        private void RestartGame()
        {
            if (page != Page.Result || reveal < 1 || !Screen.IsModalOpen(Modal)) return;
            Screen.CloseModal();
            GameSession.NewGame();
            Screen.RestartShop();
        }
    }
}
