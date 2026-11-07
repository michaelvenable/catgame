using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatGame
{
    [RequireComponent(typeof(CatInput), typeof(CatController))]
    public class CatCombat : MonoBehaviour
    {
        static readonly int AttackParam = Animator.StringToHash("Attack");

        [SerializeField] float damage = 10f;
        [Tooltip("Delay between pressing attack and the hit landing, to line up with the swipe animation.")]
        [SerializeField] float windup = 0.12f;
        [SerializeField] float recovery = 0.3f;
        [SerializeField] float hitRadius = 0.6f;
        [SerializeField] Vector3 hitOffset = new(0f, 0.4f, 0.7f);
        [SerializeField] float knockback = 5f;
        [Tooltip("Movement speed multiplier while attacking.")]
        [SerializeField] float attackMoveMultiplier = 0.3f;
        [SerializeField] LayerMask hitLayers = ~0;
        [Tooltip("Optional. Needs an 'Attack' trigger parameter.")]
        [SerializeField] Animator animator;

        readonly Collider[] hitBuffer = new Collider[16];
        readonly HashSet<Health> alreadyHit = new();
        CatInput input;
        CatController controller;
        bool attacking;

        void Awake()
        {
            input = GetComponent<CatInput>();
            controller = GetComponent<CatController>();
        }

        void Update()
        {
            if (!attacking && input.AttackPressed)
                StartCoroutine(Attack());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            attacking = false;
            controller.SpeedMultiplier = 1f;
        }

        IEnumerator Attack()
        {
            attacking = true;
            controller.SpeedMultiplier = attackMoveMultiplier;
            if (animator != null)
                animator.SetTrigger(AttackParam);

            yield return new WaitForSeconds(windup);
            DealDamage();
            yield return new WaitForSeconds(recovery);

            controller.SpeedMultiplier = 1f;
            attacking = false;
        }

        void DealDamage()
        {
            Vector3 center = transform.TransformPoint(hitOffset);
            int count = Physics.OverlapSphereNonAlloc(center, hitRadius, hitBuffer, hitLayers, QueryTriggerInteraction.Ignore);
            alreadyHit.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider hit = hitBuffer[i];
                if (hit.transform.IsChildOf(transform))
                    continue;

                Health health = hit.GetComponentInParent<Health>();
                if (health == null || !alreadyHit.Add(health))
                    continue;

                health.TakeDamage(damage);

                if (hit.attachedRigidbody != null)
                {
                    Vector3 push = hit.transform.position - transform.position;
                    push.y = 0f;
                    hit.attachedRigidbody.AddForce((push.normalized + Vector3.up * 0.3f) * knockback, ForceMode.Impulse);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Palette.Accent;
            Gizmos.DrawWireSphere(transform.TransformPoint(hitOffset), hitRadius);
        }
    }
}
