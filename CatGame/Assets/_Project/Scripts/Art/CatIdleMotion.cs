using UnityEngine;

namespace CatGame
{
    // Runs in LateUpdate so it layers on top of the Animator's pose.
    public class CatIdleMotion : MonoBehaviour
    {
        [SerializeField] Transform torso;
        [Tooltip("Root tail bone. The sway is added to its animated pose, so an Animator must drive it every frame.")]
        [SerializeField] Transform tailPivot;
        [SerializeField] float bobAmount = 0.01f;
        [SerializeField] float bobFrequency = 0.6f;
        [SerializeField] float tailSwayDegrees = 8f;
        [SerializeField] float tailSwayFrequency = 0.3f;

        Vector3 torsoBase;

        void Awake()
        {
            if (torso != null)
                torsoBase = torso.localPosition;
        }

        void LateUpdate()
        {
            const float TwoPi = Mathf.PI * 2f;
            float t = Time.time;
            if (torso != null)
                torso.localPosition = torsoBase + Vector3.up * (Mathf.Sin(t * bobFrequency * TwoPi) * bobAmount);
            if (tailPivot != null)
            {
                float sway = Mathf.Sin(t * tailSwayFrequency * TwoPi) * tailSwayDegrees;
                tailPivot.rotation = Quaternion.AngleAxis(sway, transform.up) * tailPivot.rotation;
            }
        }
    }
}
