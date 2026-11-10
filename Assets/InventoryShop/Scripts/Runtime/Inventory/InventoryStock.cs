using System;
using System.Collections.Generic;

namespace InventoryShop
{
    public enum StockArea { Backpack, Shelf }
    public enum StockMove { Moved, Partial, Swapped, Rejected }

    public readonly struct SlotAddress : IEquatable<SlotAddress>
    {
        public readonly StockArea Area;
        public readonly int Index;
        public SlotAddress(StockArea area, int index) { Area = area; Index = index; }
        public bool Equals(SlotAddress other) => Area == other.Area && Index == other.Index;
    }

    public readonly struct ItemStack
    {
        public readonly ItemDefinition Item;
        public readonly int Quantity;
        public bool IsEmpty => Item == null;

        public ItemStack(ItemDefinition item, int quantity)
        {
            if (item == null || quantity < 1 || quantity > item.StackLimit)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            Item = item;
            Quantity = quantity;
        }
    }

    public sealed class InventoryStock
    {
        public const int BackpackCapacity = 20;
        public const int ShelfCapacity = 6;

        private readonly ItemStack[] backpack = new ItemStack[BackpackCapacity];
        private readonly ItemStack[] shelf = new ItemStack[ShelfCapacity];
        public event Action Changed;

        public InventoryStock(IReadOnlyList<ItemStack> initialBackpack, IReadOnlyList<ItemStack> initialShelf)
        {
            Initialize(backpack, initialBackpack);
            Initialize(shelf, initialShelf);
        }

        public ItemStack Get(SlotAddress address) => Slots(address.Area)[address.Index];

        public StockMove Move(SlotAddress source, SlotAddress target)
        {
            if (source.Equals(target) || !IsValid(source) || !IsValid(target)) return StockMove.Rejected;
            ItemStack from = Get(source);
            ItemStack to = Get(target);
            if (from.IsEmpty) return StockMove.Rejected;

            if (to.IsEmpty)
                return Apply(source, target, default, from, StockMove.Moved);

            if (from.Item.Id != to.Item.Id)
            {
                if (source.Area != target.Area) return StockMove.Rejected;
                return Apply(source, target, to, from, StockMove.Swapped);
            }

            int amount = Math.Min(from.Quantity, to.Item.StackLimit - to.Quantity);
            if (amount <= 0) return StockMove.Rejected;
            int remainder = from.Quantity - amount;
            return Apply(source, target, remainder == 0 ? default : new ItemStack(from.Item, remainder),
                new ItemStack(to.Item, to.Quantity + amount), remainder == 0 ? StockMove.Moved : StockMove.Partial);
        }

        internal bool TryReceivePurchase(ItemDefinition item, int quantity)
        {
            long capacity = 0;
            foreach (var stack in backpack)
            {
                if (stack.IsEmpty) capacity += item.StackLimit;
                else if (stack.Item.Id == item.Id) capacity += stack.Item.StackLimit - stack.Quantity;
            }
            if (capacity < quantity) return false;

            for (int i = 0; i < backpack.Length && quantity > 0; i++)
            {
                var stack = backpack[i];
                if (stack.IsEmpty || stack.Item.Id != item.Id) continue;
                int amount = Math.Min(quantity, stack.Item.StackLimit - stack.Quantity);
                backpack[i] = new ItemStack(stack.Item, stack.Quantity + amount);
                quantity -= amount;
            }
            for (int i = 0; i < backpack.Length && quantity > 0; i++)
            {
                if (!backpack[i].IsEmpty) continue;
                int amount = Math.Min(quantity, item.StackLimit);
                backpack[i] = new ItemStack(item, amount);
                quantity -= amount;
            }
            return true;
        }

        internal bool TryReleaseSale(ItemDefinition item, int quantity)
        {
            if (quantity < 1) return false;
            long available = 0;
            foreach (var stack in shelf)
                if (!stack.IsEmpty && stack.Item.Id == item.Id) available += stack.Quantity;
            if (available < quantity) return false;

            for (int i = 0; i < shelf.Length && quantity > 0; i++)
            {
                var stack = shelf[i];
                if (stack.IsEmpty || stack.Item.Id != item.Id) continue;
                int sold = Math.Min(quantity, stack.Quantity);
                int remainder = stack.Quantity - sold;
                shelf[i] = remainder == 0 ? default : new ItemStack(stack.Item, remainder);
                quantity -= sold;
            }
            return true;
        }

        internal void NotifyChanged() => Changed?.Invoke();

        private StockMove Apply(SlotAddress source, SlotAddress target, ItemStack from, ItemStack to, StockMove result)
        {
            Slots(source.Area)[source.Index] = from;
            Slots(target.Area)[target.Index] = to;
            Changed?.Invoke();
            return result;
        }

        private ItemStack[] Slots(StockArea area) => area == StockArea.Backpack ? backpack : shelf;

        private bool IsValid(SlotAddress address)
            => (address.Area == StockArea.Backpack || address.Area == StockArea.Shelf) &&
                address.Index >= 0 && address.Index < Slots(address.Area).Length;

        private static void Initialize(ItemStack[] slots, IReadOnlyList<ItemStack> initial)
        {
            if (initial.Count > slots.Length) throw new ArgumentException("Initial inventory exceeds capacity.");
            for (int index = 0; index < initial.Count; index++) slots[index] = initial[index];
        }
    }
}
