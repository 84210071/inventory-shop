using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class WholesalePanel : MonoBehaviour
    {
        public ShopScreen Screen;
        public ShopInventory Inventory;
        public WholesaleCatalog Catalog;
        public CanvasGroup Modal, Message;
        public RectTransform Panel;
        public Button OpenButton, CloseButton, BuyButton, MinusButton, PlusButton;
        public WholesaleCard[] Cards;
        public TMP_Text TopGold, Gold, ItemName, Quality, Description, Quantity, Total, Status, ItemTerms;
        public Image ItemIcon;

        private ShopEconomy economy;
        private int index, quantity = 1;
        private float openness, nextPurchase, statusUntil, displayedGold;
        private int targetGold;
        private bool closing;
        public bool IsOpen { get; private set; }

        private ItemDefinition Current => Catalog.Items[index];

        private void Start()
        {
            economy = Inventory.Economy;
            displayedGold = targetGold = economy.Gold;
            economy.GoldChanged += UpdateGoldTarget;
            SetGoldText();
            OpenButton.onClick.AddListener(Open);
            CloseButton.onClick.AddListener(Close);
            BuyButton.onClick.AddListener(Buy);
            MinusButton.onClick.AddListener(() => Adjust(-1));
            PlusButton.onClick.AddListener(() => Adjust(1));
            for (int i = 0; i < Cards.Length; i++)
            {
                int captured = i;
                Cards[i].SetItem(Catalog.Items[i]);
                Cards[i].Button.onClick.AddListener(() => Select(captured));
            }
            Select(0);
        }

        private void Update()
        {
            if (economy == null) return;
            float dt = Time.unscaledDeltaTime;
            displayedGold = Mathf.MoveTowards(displayedGold, targetGold, dt * 360);
            SetGoldText();

            openness = Mathf.MoveTowards(openness, IsOpen ? 1f : 0f, dt / (IsOpen ? 0.28f : 0.19f));
            Modal.alpha = openness;
            float t = openness - 1f;
            float eased = 1f + 2.7f * t * t * t + 1.7f * t * t;
            Panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.91f, 1f, IsOpen ? eased : openness);

            if (closing && openness == 0f)
            {
                closing = false;
                Modal.blocksRaycasts = false;
                Screen.EndWholesale();
                OpenButton.Select();
            }

            BuyButton.interactable = IsOpen && Time.unscaledTime >= nextPurchase;
            Message.alpha = Mathf.MoveTowards(Message.alpha,
                IsOpen && Time.unscaledTime < statusUntil ? 1f : 0f, dt / 0.13f);
            float bounce = Mathf.Clamp01((statusUntil - Time.unscaledTime - 1.15f) / 0.30f);
            Message.transform.localScale = Vector3.one * (1f + Mathf.Sin(bounce * Mathf.PI) * 0.09f);
        }

        private void UpdateGoldTarget() => targetGold = economy.Gold;

        private void OnDestroy()
        {
            if (economy != null) economy.GoldChanged -= UpdateGoldTarget;
        }

        private void SetGoldText()
        {
            string value = Mathf.RoundToInt(displayedGold).ToString();
            if (TopGold.text != value) TopGold.text = value;
            if (Gold.text != value) Gold.text = value;
        }

        private void Open()
        {
            if (IsOpen || closing || !Screen.TryBeginWholesale()) return;
            IsOpen = true;
            GameAudio.Play("Open");
            Modal.interactable = Modal.blocksRaycasts = true;
            statusUntil = 0f;
            CloseButton.Select();
        }

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameAudio.Play("Close");
            closing = true;
            Modal.interactable = false;
        }

        private void Select(int selected)
        {
            index = selected;
            quantity = 1;
            for (int i = 0; i < Cards.Length; i++) Cards[i].Feedback.Marked = i == index;
            ItemIcon.sprite = Current.Icon;
            ItemName.text = Current.DisplayName;
            Quality.text = Current.Quality;
            Description.text = Current.Description;
            ItemTerms.text = "售价 " + Current.SalePrice + "  ·  堆叠上限 " + Current.StackLimit;
            RefreshOrder();
        }

        private void Adjust(int delta)
        {
            if (!IsOpen) return;
            quantity = Mathf.Clamp(quantity + delta, 1, MaxQuantity);
            RefreshOrder();
        }

        private int MaxQuantity => (int)System.Math.Min(int.MaxValue - 1L, (long)Current.StackLimit * InventoryStock.BackpackCapacity);

        private void RefreshOrder()
        {
            Quantity.text = quantity.ToString();
            Total.text = "合计 " + ((long)Current.PurchasePrice * quantity) + " 金币";
        }

        private void Buy()
        {
            if (!IsOpen || Time.unscaledTime < nextPurchase) return;
            nextPurchase = Time.unscaledTime + 0.25f;
            BuyButton.interactable = false;
            var result = economy.Purchase(Current, quantity);
            GameAudio.Play(result == PurchaseResult.Success ? "Buy" : "Error");
            switch (result)
            {
                case PurchaseResult.Success:
                    Status.text = "采购成功，货品已放入背包";
                    Status.color = new Color32(255, 226, 153, 255);
                    break;
                case PurchaseResult.InsufficientGold:
                    Status.text = "金币不足";
                    Status.color = new Color32(255, 173, 140, 255);
                    break;
                case PurchaseResult.BackpackFull:
                    Status.text = "背包空间不足";
                    Status.color = new Color32(255, 173, 140, 255);
                    break;
                default:
                    Status.text = "无法完成采购";
                    Status.color = new Color32(255, 173, 140, 255);
                    break;
            }
            Message.alpha = 0f;
            statusUntil = Time.unscaledTime + 1.45f;
        }
    }
}
