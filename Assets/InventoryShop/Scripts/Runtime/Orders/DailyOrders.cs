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
            var offers = new List<(ItemDefinition item, int quantity)>
            {
                (items.Single(item => item.Id == "frost-crystal"), 2),
                (items.Single(item => item.Id == "traveler-scroll"), 2),
                (items.Single(item => item.Id == "adventurer-dagger"), 1)
            };
            var everyday = items.Where(item => !offers.Any(offer => offer.item == item)).ToArray();
            while (offers.Count < DailyLimit)
            {
                var item = everyday[random.Next(everyday.Length)];
                offers.Add((item, random.Next(1, Math.Min(3, item.StackLimit) + 1)));
            }
            var orders = new CustomerOrder[DailyLimit];
            for (int i = 0; i < DailyLimit; i++)
            {
                int pick = random.Next(i, customers.Length);
                (customers[i], customers[pick]) = (customers[pick], customers[i]);
                int offer = random.Next(offers.Count);
                var request = offers[offer];
                offers.RemoveAt(offer);
                orders[i] = new CustomerOrder(customers[i], request.item, request.quantity);
            }
            Orders = Array.AsReadOnly(orders);
        }

        private DailyOrders(CustomerOrder[] orders) => Orders = Array.AsReadOnly(orders);

        internal static DailyOrders Restore(CustomerOrder[] orders, bool ended)
        {
            var day = new DailyOrders(orders) { Ended = ended };
            bool waiting = false;
            foreach (var order in orders)
            {
                if (order.State == OrderState.Waiting) waiting = true;
                else if (waiting) throw new ArgumentException("Processed order after a waiting order.");
                else if (order.State == OrderState.Sold) day.Sold++;
                else day.Rejected++;
            }
            if (ended && !day.Complete) throw new ArgumentException("Incomplete settled day.");
            return day;
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
