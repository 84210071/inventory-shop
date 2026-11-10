using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class StockSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image Icon;
        public TMP_Text Quantity, Caption;
        public ButtonFeedback Feedback;

        private ShopInventory owner;
        private RectTransform visual;
        private Vector2 origin;
        private float feedbackUntil;


        public SlotAddress Address { get; private set; }

        public void Bind(ShopInventory inventory, SlotAddress address)
        {
            owner = inventory;
            Address = address;
            visual = Icon.rectTransform;
            origin = visual.anchoredPosition;
            GetComponent<Button>().onClick.AddListener(Select);
        }

        public void Render(ItemStack stack, bool selected)
        {
            Icon.enabled = !stack.IsEmpty;
            Icon.sprite = stack.IsEmpty ? null : stack.Item.Icon;
            Quantity.text = stack.IsEmpty ? "" : stack.Quantity.ToString();
            if (Caption != null) Caption.text = stack.IsEmpty ? "空货位" : stack.Item.DisplayName;
            Feedback.Marked = selected;
        }

        public void Pulse(bool failed)
        {
            if (failed) feedbackUntil = Time.unscaledTime + 0.24f;
            else Feedback.Pulse();
        }

        private void LateUpdate()
        {
            if (visual == null) return;
            float strength = Mathf.Clamp01((feedbackUntil - Time.unscaledTime) / 0.24f);
            visual.anchoredPosition = origin + Vector2.right * (Mathf.Sin(strength * 29) * strength * 12);
        }

        private void Select() => owner.Select(this);
        public void OnPointerEnter(PointerEventData e) => owner.Hover(this, e);
        public void OnPointerExit(PointerEventData e) => owner.Unhover(this);
        public void OnBeginDrag(PointerEventData e) => owner.BeginDrag(this, e);
        public void OnDrag(PointerEventData e) => owner.Drag(this, e);
        public void OnEndDrag(PointerEventData e) => owner.EndDrag(this, e);
    }
}
