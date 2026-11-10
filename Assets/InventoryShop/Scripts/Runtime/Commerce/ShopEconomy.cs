using System;

namespace InventoryShop
{
    public enum PurchaseResult { Success, InsufficientGold, BackpackFull, Invalid }

    public sealed class ShopEconomy
    {
        private bool purchasing;

        public InventoryStock Stock { get; }
        public int Gold { get; private set; } = 500;
        public long TotalPurchaseSpend { get; private set; }
        public event Action GoldChanged;

        public ShopEconomy(InventoryStock stock) => Stock = stock ?? throw new ArgumentNullException(nameof(stock));

        public PurchaseResult Purchase(ItemDefinition item, int quantity)
        {
            if (purchasing || item == null || quantity < 1 || item.PurchasePrice < 0 || item.StackLimit < 1)
                return PurchaseResult.Invalid;

            long cost = (long)item.PurchasePrice * quantity;
            if (cost > Gold) return PurchaseResult.InsufficientGold;

            purchasing = true;
            try
            {
                if (!Stock.TryReceivePurchase(item, quantity)) return PurchaseResult.BackpackFull;
                Gold -= (int)cost;
                TotalPurchaseSpend += cost;
                Stock.NotifyChanged();
                GoldChanged?.Invoke();
                return PurchaseResult.Success;
            }
            finally
            {
                purchasing = false;
            }
        }
    }
}
