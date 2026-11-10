using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public Image Surface;
        public Sprite Normal, Hover, Pressed;
        public Outline SelectionOutline;
        public bool Marked { get; set; }

        private bool hovered, focused, held;
        private float submitUntil, pulseUntil;
        private float baseScale = 1f;

        public void Pulse() => pulseUntil = Time.unscaledTime + 0.22f;

        private void Update()
        {
            bool pressed = held || Time.unscaledTime < submitUntil;
            bool highlighted = hovered || focused || Marked;
            Surface.sprite = pressed ? Pressed : highlighted ? Hover : Normal;
            if (SelectionOutline != null) SelectionOutline.enabled = Marked;
            float target = pressed ? 0.96f : highlighted ? 1.045f : 1f;
            baseScale = Mathf.MoveTowards(baseScale, target, Time.unscaledDeltaTime * 0.65f);
            float pulse = Mathf.Clamp01((pulseUntil - Time.unscaledTime) / 0.22f);
            float scale = baseScale + Mathf.Sin(pulse * Mathf.PI) * 0.045f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDisable()
        {
            hovered = focused = held = false;
            submitUntil = pulseUntil = 0f;
            baseScale = 1f;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) => hovered = false;
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) held = true; }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) held = false; }
        public void OnSelect(BaseEventData e) => focused = true;
        public void OnDeselect(BaseEventData e) => focused = false;
        public void OnSubmit(BaseEventData e) => submitUntil = Time.unscaledTime + 0.12f;
    }
}
