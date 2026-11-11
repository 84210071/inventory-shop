using System;

namespace InventoryShop
{
    public enum PurchaseResult { Success, InsufficientGold, BackpackFull, Invalid }

    public sealed partial class ShopEconomy
    {
        private bool transacting;

        public InventoryStock Stock { get; }
        public int Gold { get; private set; } = 500;
        public long TotalPurchaseSpend { get; private set; }
        public long TotalSalesRevenue { get; private set; }
        public DailyOrders CustomerDay { get; private set; }
        public event Action GoldChanged;

        public ShopEconomy(InventoryStock stock) => Stock = stock ?? throw new ArgumentNullException(nameof(stock));

        public DailyOrders InitializeCustomers(CustomerRoster roster, WholesaleCatalog catalog)
            => CustomerDay ??= roster.Create(catalog, Day);

        public SaleResult Sell(CustomerOrder order)
        {
            if (Phase != BusinessPhase.Trading || transacting || CustomerDay == null || !CustomerDay.IsCurrent(order) ||
                order.Total < 0 || order.Total > int.MaxValue - (long)Gold)
                return SaleResult.Invalid;

            transacting = true;
            try
            {
                if (!Stock.TryReleaseSale(order.Item, order.Quantity)) return SaleResult.OutOfStock;
                Gold += (int)order.Total;
                TotalSalesRevenue += order.Total;
                CustomerDay.ResolveCurrent(OrderState.Sold);
                Stock.NotifyChanged();
                GoldChanged?.Invoke();
                return SaleResult.Success;
            }
            finally
            {
                transacting = false;
            }
        }

        public bool Reject(CustomerOrder order)
        {
            if (Phase != BusinessPhase.Trading || transacting || CustomerDay == null || !CustomerDay.IsCurrent(order)) return false;
            CustomerDay.ResolveCurrent(OrderState.Rejected);
            return true;
        }

        public PurchaseResult Purchase(ItemDefinition item, int quantity)
        {
            if (Phase != BusinessPhase.Trading || transacting || item == null || quantity < 1 || item.PurchasePrice < 0 || item.StackLimit < 1)
                return PurchaseResult.Invalid;

            long cost = (long)item.PurchasePrice * quantity;
            if (cost > Gold) return PurchaseResult.InsufficientGold;

            transacting = true;
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
                transacting = false;
            }
        }
    }
}
