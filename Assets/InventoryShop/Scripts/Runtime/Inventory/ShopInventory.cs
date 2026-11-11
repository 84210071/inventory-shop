using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class ShopInventory : MonoBehaviour
    {
        public StartingStock InitialStock;
        public ShopScreen Screen;
        public InventoryOverlay Overlay;
        public Button BackpackButton, CloseButton;
        public StockSlotView[] ShelfSlots, BackpackSlots;

        private StockSlotView selected, hovered, dragSource;
        private int dragPointer;

        public ShopEconomy Economy { get; private set; }
        public InventoryStock Stock => Economy?.Stock;


        private void Awake()
        {
            Economy = GameSession.GetOrStart(InitialStock);
            Bind(ShelfSlots, StockArea.Shelf);
            Bind(BackpackSlots, StockArea.Backpack);
            BackpackButton.onClick.AddListener(ToggleBackpack);
            CloseButton.onClick.AddListener(CloseBackpack);
            Stock.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Stock != null) Stock.Changed -= Refresh;
        }

        private void Bind(StockSlotView[] slots, StockArea area)
        {
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(this, new SlotAddress(area, i));
        }

        private void ToggleBackpack()
        {
            if (Overlay.IsOpen) CloseBackpack();
            else if (Screen.CanUseInventory)
            {
                Screen.CloseDetails();
                Overlay.SetOpen(true);
                GameAudio.Play("Open");
                CloseButton.Select();
            }
        }

        public void SuspendForWholesale()
        {
            CloseBackpack();
            CancelDrag();
            hovered = null;
            Overlay.HideTooltip();
        }

        private void CloseBackpack()
        {
            if (!Overlay.IsOpen) return;
            CancelDrag();
            Overlay.SetOpen(false);
            GameAudio.Play("Close");
            if (selected != null && selected.Address.Area == StockArea.Backpack) selected = null;
            hovered = null;
            Refresh();
            BackpackButton.Select();
        }

        public void Select(StockSlotView slot)
        {
            if (!CanUse(slot) || dragSource != null) return;
            ItemStack stack = Stock.Get(slot.Address);
            if (stack.IsEmpty) return;
            selected = slot;
            Refresh();
            if (!Overlay.IsOpen && slot.Address.Area == StockArea.Shelf)
                Screen.ShowInventoryProduct(stack.Item);
        }

        public void ClearSelection()
        {
            selected = null;
            Refresh();
        }

        public void Hover(StockSlotView slot, PointerEventData e)
        {
            if (!CanUse(slot) || dragSource != null) return;
            hovered = slot;
            ItemStack stack = Stock.Get(slot.Address);
            if (!stack.IsEmpty) Overlay.ShowTooltip(stack, e.position);
        }

        public void Unhover(StockSlotView slot)
        {
            if (hovered != slot) return;
            hovered = null;
            Overlay.HideTooltip();
        }

        public void BeginDrag(StockSlotView slot, PointerEventData e)
        {
            if (dragSource != null || e.button != PointerEventData.InputButton.Left || !CanUse(slot)) return;
            ItemStack stack = Stock.Get(slot.Address);
            if (stack.IsEmpty) return;
            dragSource = slot;
            dragPointer = e.pointerId;
            selected = slot;
            Refresh();
            Overlay.HideTooltip();
            GameAudio.Play("Drag");
            Overlay.ShowDrag(stack.Item.Icon, e.position);
        }

        public void Drag(StockSlotView slot, PointerEventData e)
        {
            if (OwnsDrag(slot, e)) Overlay.MoveDrag(e.position);
        }

        public void EndDrag(StockSlotView slot, PointerEventData e)
        {
            if (!OwnsDrag(slot, e)) return;
            CancelDrag();
            var hit = e.pointerCurrentRaycast.gameObject;
            var target = hit == null ? null : hit.GetComponentInParent<StockSlotView>();
            if (target == null || !CanUse(slot) || !CanUse(target) || target == slot)
            {
                slot.Pulse(true);
                GameAudio.Play("Error");
                Overlay.Reject("请放到有效货位");
                return;
            }

            var result = Stock.Move(slot.Address, target.Address);
            if (result == StockMove.Rejected)
            {
                slot.Pulse(true);
                GameAudio.Play("Error");
                Overlay.Reject("货位已占用或堆叠已满");
                return;
            }

            selected = target;
            Refresh();
            target.Pulse(false);
            GameAudio.Play("Place");
            if (result == StockMove.Partial) slot.Pulse(false);
            if (!Overlay.IsOpen) Screen.ShowInventoryProduct(Stock.Get(target.Address).Item);
        }

        private bool OwnsDrag(StockSlotView slot, PointerEventData e)
            => dragSource == slot && dragPointer == e.pointerId;

        private void CancelDrag()
        {
            dragSource = null;
            Overlay.HideDrag();
        }

        private bool CanUse(StockSlotView slot)
            => Screen.CanUseInventory && (slot.Address.Area == StockArea.Shelf || Overlay.IsOpen);

        private void Refresh()
        {
            foreach (var view in ShelfSlots) view.Render(Stock.Get(view.Address), view == selected);
            foreach (var view in BackpackSlots) view.Render(Stock.Get(view.Address), view == selected);
        }
    }
}
