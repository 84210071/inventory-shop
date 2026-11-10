using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class WholesaleCard : MonoBehaviour
    {
        public Button Button;
        public ButtonFeedback Feedback;
        public Image Icon;
        public TMP_Text Title, Quality, UnitPrice;

        public void SetItem(ItemDefinition item)
        {
            Icon.sprite = item.Icon;
            Title.text = item.DisplayName;
            Quality.text = item.Quality;
            UnitPrice.text = item.PurchasePrice + " 金币";
        }
    }
}
