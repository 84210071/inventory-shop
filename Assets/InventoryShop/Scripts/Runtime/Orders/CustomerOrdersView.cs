using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class CustomerOrdersView : MonoBehaviour
    {
        public ShopScreen Screen;
        public ShopInventory Inventory;
        public CustomerRoster Roster;
        public WholesaleCatalog Catalog;
        public CustomerOrderCard Card;
        public Button ToggleButton, EndButton;
        public TMP_Text ToggleLabel, Progress, DayStatus, Feedback;
        public CanvasGroup FeedbackGroup;

        private ShopEconomy economy;
        private DailyOrders day;
        private CustomerOrder shown;
        private RectTransform cardRect, feedbackRect;
        private Vector2 cardHome, feedbackHome;
        private float appearance, feedbackUntil;
        private bool expanded = true, resolving, confirming, sold, rising;

        private bool Available => Screen.CanUseInventory && !Inventory.Overlay.IsOpen;
        public bool WantsCard => day != null && expanded && (shown != null || resolving) && Available;
        private bool CanResolve => WantsCard && appearance > 0.95f && !resolving && shown != null;

        private void Start()
        {
            economy = Inventory.Economy;
            day = economy.InitializeCustomers(Roster, Catalog);
            cardRect = (RectTransform)Card.transform;
            cardHome = cardRect.anchoredPosition;
            feedbackRect = (RectTransform)FeedbackGroup.transform;
            feedbackHome = feedbackRect.anchoredPosition;
            ToggleButton.onClick.AddListener(Toggle);
            EndButton.onClick.AddListener(EndTrading);
            Card.Sell.onClick.AddListener(TrySell);
            Card.Decline.onClick.AddListener(AskDecline);
            Card.Confirm.onClick.AddListener(Decline);
            Card.Cancel.onClick.AddListener(() => confirming = false);
            ShowCurrent();
        }

        private void Update()
        {
            if (day == null) return;
            float dt = Time.unscaledDeltaTime;
            bool show = WantsCard && !resolving;
            if (!Available)
            {
                appearance = 0f;
                confirming = false;
            }
            else
                appearance = Mathf.MoveTowards(appearance, show ? 1f : 0f, dt / (show ? 0.33f : 0.28f));

            float ease = 1f - Mathf.Pow(1f - appearance, 3);
            cardRect.anchoredPosition = cardHome + Vector2.right * ((1f - ease) * 485f);
            Card.Surface.alpha = appearance;
            Card.Surface.blocksRaycasts = show && appearance > 0.95f;
            Card.Surface.interactable = Card.Surface.blocksRaycasts;
            Card.PortraitFade.alpha = Mathf.Clamp01((appearance - 0.12f) / 0.76f);
            float iconScale = resolving && sold ? Mathf.Lerp(0.36f, 1f, appearance) :
                Mathf.Lerp(0.80f, 1f, ease) + Mathf.Sin(appearance * Mathf.PI) * 0.10f;
            Card.ItemIcon.rectTransform.localScale = Vector3.one * iconScale;

            Card.Confirmation.alpha = Mathf.MoveTowards(Card.Confirmation.alpha, confirming ? 1f : 0f, dt / 0.13f);
            Card.Confirmation.blocksRaycasts = Card.Confirmation.interactable = confirming;
            Card.Sell.interactable = Card.Decline.interactable = CanResolve && !confirming;
            ToggleButton.interactable = Available && !day.Complete && !resolving;
            EndButton.interactable = !day.Ended && !resolving;

            float remaining = feedbackUntil - Time.unscaledTime;
            bool feedbackVisible = remaining > 0f && Screen.CanUseInventory;
            FeedbackGroup.alpha = Mathf.MoveTowards(FeedbackGroup.alpha, feedbackVisible ? 1f : 0f, dt / 0.15f);
            float travel = rising ? Mathf.Clamp01(1f - remaining / 1.45f) * 52f : 0f;
            feedbackRect.anchoredPosition = feedbackHome + Vector2.up * travel;
        }

        public void RefreshDay()
        {
            day = economy.CustomerDay;
            expanded = true;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            shown = day.Current;
            if (shown != null) Card.Bind(shown, day.Processed + 1);
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            Progress.text = "已处理顾客 " + day.Processed + "/" + DailyOrders.DailyLimit +
                "  ·  成功 " + day.Sold + "  ·  拒绝 " + day.Rejected;
            ToggleLabel.text = expanded ? "收起订单" : "查看订单";
            DayStatus.text = day.Ended ? "今日营业已结束" : day.Complete ? "今日营业已完成" : "";
        }

        private void Toggle()
        {
            if (!Available || resolving || day.Complete) return;
            if (!expanded) Screen.CloseDetails();
            expanded = !expanded;
            confirming = false;
            RefreshProgress();
        }

        private void AskDecline()
        {
            if (CanResolve) confirming = true;
        }

        private void Decline()
        {
            if (!confirming || !CanResolve || !economy.Reject(shown)) return;
            confirming = false;
            GameAudio.Play("Close");
            ShowFeedback("顾客已离开", false, false);
            StartCoroutine(Depart(false));
        }

        private void TrySell()
        {
            if (!CanResolve || confirming) return;
            long total = shown.Total;
            switch (economy.Sell(shown))
            {
                case SaleResult.Success:
                    GameAudio.Play("Sale");
                    ShowFeedback("交易成功  +" + total + " 金币", true, true);
                    StartCoroutine(Depart(true));
                    break;
                case SaleResult.OutOfStock:
                    GameAudio.Play("Error");
                    ShowFeedback("货架库存不足", false, false);
                    break;
                default:
                    ShowFeedback("无法完成交易", false, false);
                    break;
            }
        }

        private IEnumerator Depart(bool successful)
        {
            resolving = true;
            sold = successful;
            RefreshProgress();
            yield return new WaitForSecondsRealtime(0.31f);
            ShowCurrent();
            resolving = false;
        }

        private void EndTrading()
        {
            if (!Screen.CanUseInventory || resolving || day.Ended) return;
            if (!day.Complete)
            {
                ShowFeedback("还有未处理订单", false, false);
                return;
            }
            Screen.Business.FinishDay();
        }

        private void ShowFeedback(string message, bool positive, bool floatUp)
        {
            Feedback.text = message;
            Feedback.color = positive ? new Color32(255, 228, 140, 255) : new Color32(255, 175, 139, 255);
            rising = floatUp;
            feedbackUntil = Time.unscaledTime + 1.45f;
            FeedbackGroup.alpha = 0f;
        }
    }
}
