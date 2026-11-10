using UnityEngine;

namespace InventoryShop
{
    [CreateAssetMenu(menuName = "InventoryShop/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id, displayName, quality;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int purchasePrice, salePrice;
        [SerializeField, Min(1)] private int stackLimit = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public string Quality => quality;
        public string Description => description;
        public Sprite Icon => icon;
        public int PurchasePrice => purchasePrice;
        public int SalePrice => salePrice;
        public int StackLimit => stackLimit;
    }
}
