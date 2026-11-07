using UnityEngine;

namespace CatGame
{
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] Color flashColor = Palette.Accent;
        [SerializeField] float duration = 0.1f;

        Health health;
        Renderer[] renderers;
        Color[] originalColors;
        float flashEndTime;
        bool flashing;

        void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].material.color;
        }

        void OnEnable() => health.onDamaged.AddListener(Flash);
        void OnDisable() => health.onDamaged.RemoveListener(Flash);

        void Update()
        {
            if (!flashing || Time.time < flashEndTime)
                return;

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = originalColors[i];
            flashing = false;
        }

        void Flash(float _)
        {
            foreach (Renderer r in renderers)
                r.material.color = flashColor;
            flashEndTime = Time.time + duration;
            flashing = true;
        }
    }
}
