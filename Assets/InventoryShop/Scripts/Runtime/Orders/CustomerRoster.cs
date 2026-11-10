using System;
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

        public DailyOrders Create(WholesaleCatalog catalog) => new DailyOrders(customers, catalog.Items,
            useFixedSeed ? seed : UnityEngine.Random.Range(0, int.MaxValue));
    }
}
