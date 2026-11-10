using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class CustomerOrderCard : MonoBehaviour
    {
        public CanvasGroup Surface, PortraitFade, Confirmation;
        public Image Portrait, ItemIcon;
        public TMP_Text Name, Greeting, ItemName, Quantity, Total, Position;
        public Button Sell, Decline, Confirm, Cancel;

        public void Bind(CustomerOrder order, int number)
        {
            Portrait.sprite = order.Customer.Portrait;
            ItemIcon.sprite = order.Item.Icon;
            Name.text = order.Customer.Name;
            Greeting.text = order.Customer.Greeting;
            ItemName.text = order.Item.DisplayName;
            Quantity.text = "购买数量 " + order.Quantity;
            Total.text = "订单总价 " + order.Total + " 金币";
            Position.text = "今日顾客 " + number + "/" + DailyOrders.DailyLimit;
        }
    }
}
