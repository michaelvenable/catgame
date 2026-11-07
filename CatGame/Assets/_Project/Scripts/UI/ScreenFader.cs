using System.Collections;
using UnityEngine;

namespace CatGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        CanvasGroup group;

        void Awake() => group = GetComponent<CanvasGroup>();

        public void Snap(float alpha) => group.alpha = alpha;

        public IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
