using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class ShopScreen : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Product
        {
            public string Name, Quality, Description;
            public int Price;
            public Sprite Icon;
            public ButtonFeedback Slot;
        }

        public CanvasGroup Curtain;
        public FadePanel Settings, Notice, Details;
        public Button FirstButton, SettingsClose, NoticeClose;
        public TMP_Text NoticeText, ProductName, Quality, Description, Price;
        public Image ProductIcon;
        public Product[] Products;
        public ShopInventory Inventory;

        public bool CanUseInventory => !transitioning && modal == null;

        private bool transitioning;
        private FadePanel modal;
        private GameObject previousFocus;

        private IEnumerator Start()
        {
            Settings.SetImmediate(false);
            Notice.SetImmediate(false);
            if (Details != null) Details.SetImmediate(false);
            transitioning = true;
            yield return FadeCurtain(0f);
            Curtain.blocksRaycasts = false;
            transitioning = false;
            FirstButton.Select();
        }

        public void EnterShop() => Navigate("Shop");
        public void ReturnToMenu() => Navigate("MainMenu");
        public void OpenSettings() => OpenModal(Settings, SettingsClose);

        public void ShowComingSoon()
        {
            NoticeText.text = "功能开发中";
            OpenModal(Notice, NoticeClose);
        }

        public void Quit()
        {
            if (transitioning || modal != null) return;
#if UNITY_EDITOR
            NoticeText.text = "编辑器预览中\n正式版本将退出游戏";
            OpenModal(Notice, NoticeClose);
#else
            Application.Quit();
#endif
        }

        private void OpenModal(FadePanel panel, Button close)
        {
            if (transitioning || modal != null) return;
            previousFocus = EventSystem.current.currentSelectedGameObject;
            modal = panel;
            panel.Show();
            close.Select();
        }

        public void CloseModal()
        {
            if (modal == null) return;
            modal.Hide();
            modal = null;
            EventSystem.current.SetSelectedGameObject(previousFocus);
        }

        public void ShowProduct(int index)
        {
            if (transitioning || modal != null) return;
            var product = Products[index];
            foreach (var entry in Products) entry.Slot.Marked = entry == product;
            ProductName.text = product.Name;
            Quality.text = product.Quality;
            Description.text = product.Description;
            Price.text = product.Price + " 金币";
            ProductIcon.sprite = product.Icon;
            Details.Show();
        }

        public void ShowInventoryProduct(ItemDefinition item)
        {
            if (!CanUseInventory || item == null) return;
            ProductName.text = item.DisplayName;
            Quality.text = item.Quality;
            Description.text = item.Description;
            Price.text = item.SalePrice + " 金币";
            ProductIcon.sprite = item.Icon;
            Details.Show();
        }

        public void CloseDetails()
        {
            if (transitioning || modal != null) return;
            Details.Hide();
            if (Inventory != null)
            {
                Inventory.ClearSelection();
                return;
            }
            foreach (var entry in Products)
            {
                if (entry.Slot.Marked) entry.Slot.GetComponent<Button>().Select();
                entry.Slot.Marked = false;
            }
        }

        private void Navigate(string scene)
        {
            if (transitioning || modal != null) return;
            StartCoroutine(SwitchScene(scene));
        }

        private IEnumerator SwitchScene(string scene)
        {
            transitioning = true;
            Curtain.blocksRaycasts = true;
            EventSystem.current.SetSelectedGameObject(null);
            yield return FadeCurtain(1f);
            yield return SceneManager.LoadSceneAsync(scene);
        }

        private IEnumerator FadeCurtain(float target)
        {
            while (!Mathf.Approximately(Curtain.alpha, target))
            {
                Curtain.alpha = Mathf.MoveTowards(Curtain.alpha, target, Time.unscaledDeltaTime / 0.36f);
                yield return null;
            }
        }
    }
}
