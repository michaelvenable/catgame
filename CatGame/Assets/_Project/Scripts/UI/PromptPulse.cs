using UnityEngine;

namespace CatGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PromptPulse : MonoBehaviour
    {
        [SerializeField] float period = 1.6f;
        [SerializeField] float minAlpha = 0.5f;
        [SerializeField] float maxAlpha = 1f;

        CanvasGroup group;

        void Awake() => group = GetComponent<CanvasGroup>();

        void Update()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period);
            group.alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
        }
    }
}
