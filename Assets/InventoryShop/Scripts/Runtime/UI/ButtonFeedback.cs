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
        private float submitUntil;

        private void Update()
        {
            bool pressed = held || Time.unscaledTime < submitUntil;
            bool highlighted = hovered || focused || Marked;
            Surface.sprite = pressed ? Pressed : highlighted ? Hover : Normal;
            if (SelectionOutline != null) SelectionOutline.enabled = Marked;
            float target = pressed ? 0.96f : highlighted ? 1.045f : 1f;
            float scale = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 0.65f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDisable()
        {
            hovered = focused = held = false;
            submitUntil = 0f;
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
