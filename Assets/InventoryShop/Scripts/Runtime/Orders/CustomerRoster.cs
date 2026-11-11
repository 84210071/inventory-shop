using System;
using System.Linq;
using UnityEngine;

namespace InventoryShop
{
    public sealed class CustomerRoster : ScriptableObject
    {
        [Serializable]
        public struct Customer
        {
            public string Name;
            public Sprite Portrait;
            public string Greeting;
        }

        [SerializeField] private Customer[] customers;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 2026;

        public Customer Find(string name) => customers.Single(customer => customer.Name == name);

        public DailyOrders Create(WholesaleCatalog catalog, int day) => new DailyOrders(customers, catalog.Items,
            useFixedSeed ? unchecked(seed + day - 1) : UnityEngine.Random.Range(0, int.MaxValue));
    }
}
