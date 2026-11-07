using UnityEngine;
using UnityEngine.Events;

namespace CatGame
{
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHealth = 30f;
        [SerializeField] bool destroyOnDeath = true;

        public UnityEvent<float> onDamaged = new();
        public UnityEvent onDied = new();

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        void Awake() => Current = maxHealth;

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            Current = Mathf.Max(0f, Current - amount);
            onDamaged.Invoke(amount);

            if (!IsDead)
                return;

            onDied.Invoke();
            if (destroyOnDeath)
                Destroy(gameObject, 0.15f);
        }
    }
}
