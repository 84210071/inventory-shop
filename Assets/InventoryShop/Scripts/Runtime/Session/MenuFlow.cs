using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class MenuFlow : MonoBehaviour
    {
        public ShopScreen Screen;
        public WholesaleCatalog Catalog;
        public CustomerRoster Roster;
        public Button NewButton, ContinueButton;
        public TMP_Text SaveStatus;

        private void Start()
        {
            ContinueButton.interactable = GameSession.TryRead(Catalog, Roster, out _);
            if (GameSession.HasSave && !ContinueButton.interactable)
                SaveStatus.text = "存档无法读取，可开始新游戏";
            NewButton.onClick.AddListener(() =>
            {
                if (!Screen.CanUseInventory) return;
                GameSession.NewGame();
                Screen.EnterShop();
            });
            ContinueButton.onClick.AddListener(() =>
            {
                if (!Screen.CanUseInventory) return;
                if (!GameSession.TryRead(Catalog, Roster, out var economy))
                {
                    SaveStatus.text = "存档无法读取，可开始新游戏";
                    ContinueButton.interactable = false;
                    return;
                }
                GameSession.Resume(economy);
                Screen.EnterShop();
            });
        }
    }
}
