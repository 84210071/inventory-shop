using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class InventoryOverlay : MonoBehaviour
    {
        public CanvasGroup Window, Tooltip, Status;
        public TMP_Text TooltipName, TooltipMeta, TooltipDescription, TooltipPrice, StatusText;
        public Image TooltipIcon, DragIcon;

        private RectTransform canvas;
        private float openness;
        private float statusUntil;
        private bool tooltipVisible;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            Window.alpha = Tooltip.alpha = Status.alpha = 0;
            Window.blocksRaycasts = Window.interactable = false;
            DragIcon.enabled = false;
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            Window.blocksRaycasts = Window.interactable = open;
            HideTooltip();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            openness = Mathf.MoveTowards(openness, IsOpen ? 1 : 0, dt / (IsOpen ? 0.28f : 0.17f));
            Window.alpha = openness;
            float back = openness - 1;
            float overshoot = 1 + 2.7f * back * back * back + 1.7f * back * back;
            float scale = Mathf.LerpUnclamped(0.88f, 1, IsOpen ? overshoot : openness);
            Window.transform.localScale = Vector3.one * scale;
            Tooltip.alpha = Mathf.MoveTowards(Tooltip.alpha, tooltipVisible ? 1 : 0, dt / 0.10f);
            Status.alpha = Mathf.MoveTowards(Status.alpha, Time.unscaledTime < statusUntil ? 1 : 0, dt / 0.15f);
        }

        public void ShowTooltip(ItemStack stack, Vector2 screenPosition)
        {
            TooltipIcon.sprite = stack.Item.Icon;
            TooltipName.text = stack.Item.DisplayName;
            TooltipMeta.text = stack.Item.Quality + "  ·  数量 " + stack.Quantity;
            TooltipDescription.text = stack.Item.Description;
            TooltipPrice.text = "采购 " + stack.Item.PurchasePrice + "  ·  出售 " + stack.Item.SalePrice + " 金币";
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPosition, null, out Vector2 point);
            var rect = (RectTransform)Tooltip.transform;
            Vector2 half = rect.sizeDelta * 0.5f;
            float x = point.x + half.x + 32;
            if (x + half.x > canvas.rect.xMax) x = point.x - half.x - 32;
            rect.anchoredPosition = new Vector2(
                Mathf.Clamp(x, canvas.rect.xMin + half.x, canvas.rect.xMax - half.x),
                Mathf.Clamp(point.y + 36, canvas.rect.yMin + half.y, canvas.rect.yMax - half.y));
            tooltipVisible = true;
        }

        public void HideTooltip() => tooltipVisible = false;

        public void ShowDrag(Sprite icon, Vector2 position)
        {
            DragIcon.sprite = icon;
            DragIcon.enabled = true;
            MoveDrag(position);
        }

        public void MoveDrag(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPosition, null, out Vector2 point);
            DragIcon.rectTransform.anchoredPosition = point;
        }

        public void HideDrag() => DragIcon.enabled = false;

        public void Reject(string message)
        {
            StatusText.text = message;
            statusUntil = Time.unscaledTime + 1.2f;
        }
    }
}
