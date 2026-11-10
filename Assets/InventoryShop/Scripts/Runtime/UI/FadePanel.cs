using System.Collections;
using UnityEngine;

namespace InventoryShop
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FadePanel : MonoBehaviour
    {
        private CanvasGroup Group => GetComponent<CanvasGroup>();
        private Coroutine fadeRoutine;

        public void SetImmediate(bool visible)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            Group.alpha = visible ? 1f : 0f;
            Group.interactable = Group.blocksRaycasts = visible;
        }

        public void Show() => Animate(true);
        public void Hide() => Animate(false);

        private void Animate(bool visible)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            Group.interactable = Group.blocksRaycasts = visible;
            fadeRoutine = StartCoroutine(Fade(visible ? 1f : 0f));
        }

        private IEnumerator Fade(float target)
        {
            while (!Mathf.Approximately(Group.alpha, target))
            {
                Group.alpha = Mathf.MoveTowards(Group.alpha, target, Time.unscaledDeltaTime / 0.24f);
                yield return null;
            }
            fadeRoutine = null;
        }
    }
}
