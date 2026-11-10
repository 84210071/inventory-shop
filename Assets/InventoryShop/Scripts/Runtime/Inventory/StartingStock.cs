using System;
using System.Linq;
using UnityEngine;

namespace InventoryShop
{
    [CreateAssetMenu(menuName = "InventoryShop/Starting Stock")]
    public sealed class StartingStock : ScriptableObject
    {
        [Serializable]
        private struct Seed
        {
            public ItemDefinition item;
            public int quantity;
            public ItemStack Create() => item == null ? default : new ItemStack(item, quantity);
        }

        [SerializeField] private Seed[] backpack, shelf;

        public InventoryStock Create() => new InventoryStock(
            backpack.Select(seed => seed.Create()).ToArray(),
            shelf.Select(seed => seed.Create()).ToArray());
    }
}
