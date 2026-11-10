using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryShop
{
    public enum OrderState { Waiting, Sold, Rejected }
    public enum SaleResult { Success, OutOfStock, Invalid }

    public sealed class CustomerOrder
    {
        public CustomerRoster.Customer Customer { get; }
        public ItemDefinition Item { get; }
        public int Quantity { get; }
        public long Total { get; }
        public OrderState State { get; private set; }

        internal CustomerOrder(CustomerRoster.Customer customer, ItemDefinition item, int quantity)
        {
            Customer = customer;
            Item = item;
            Quantity = quantity;
            Total = (long)item.SalePrice * quantity;
        }

        internal void Resolve(OrderState state) => State = state;
    }

    public sealed class DailyOrders
    {
        public const int DailyLimit = 5;

        public IReadOnlyList<CustomerOrder> Orders { get; }
        public int Sold { get; private set; }
        public int Rejected { get; private set; }
        public int Processed => Sold + Rejected;
        public bool Ended { get; private set; }
        public bool Complete => Processed == DailyLimit;
        public CustomerOrder Current => Complete ? null : Orders[Processed];

        public DailyOrders(IReadOnlyList<CustomerRoster.Customer> roster, IReadOnlyList<ItemDefinition> items, int seed)
        {
            if (roster.Count < DailyLimit || items.Count == 0)
                throw new ArgumentException("A customer day needs five customers and a nonempty catalog.");

            var random = new Random(seed);
            var customers = roster.ToArray();
            var orders = new CustomerOrder[DailyLimit];
            for (int i = 0; i < DailyLimit; i++)
            {
                int pick = random.Next(i, customers.Length);
                (customers[i], customers[pick]) = (customers[pick], customers[i]);
                ItemDefinition item = items[random.Next(items.Count)];
                int quantity = random.Next(1, Math.Min(3, item.StackLimit) + 1);
                orders[i] = new CustomerOrder(customers[i], item, quantity);
            }
            Orders = Array.AsReadOnly(orders);
        }

        internal bool IsCurrent(CustomerOrder order) => order != null && order == Current && order.State == OrderState.Waiting;

        internal void ResolveCurrent(OrderState state)
        {
            Current.Resolve(state);
            if (state == OrderState.Sold) Sold++;
            else Rejected++;
        }

        public bool EndTrading()
        {
            if (!Complete || Ended) return false;
            Ended = true;
            return true;
        }
    }
}
