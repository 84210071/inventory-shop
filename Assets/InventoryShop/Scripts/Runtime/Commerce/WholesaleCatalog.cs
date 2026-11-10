using System.Collections.Generic;
using UnityEngine;

namespace InventoryShop
{
    public sealed class WholesaleCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items;
        public IReadOnlyList<ItemDefinition> Items => items;
    }
}
