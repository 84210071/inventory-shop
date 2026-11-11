using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryShop
{
    public sealed class VolumeControls : MonoBehaviour
    {
        public Button Lower, Raise;
        public TMP_Text Value;

        private void Start()
        {
            Lower.onClick.AddListener(() => Change(-0.1f));
            Raise.onClick.AddListener(() => Change(0.1f));
            Change(0);
        }

        private void Change(float amount)
        {
            GameAudio.Volume = Mathf.Clamp01(GameAudio.Volume + amount);
            Value.text = "音量 " + Mathf.RoundToInt(GameAudio.Volume * 100) + "%";
        }
    }
}
