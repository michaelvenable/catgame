using UnityEngine;

namespace CatGame
{
    public class CatIdleMotion : MonoBehaviour
    {
        [SerializeField] Transform torso;
        [SerializeField] Transform tailPivot;
        [SerializeField] float bobAmount = 0.01f;
        [SerializeField] float bobFrequency = 0.6f;
        [SerializeField] float tailSwayDegrees = 8f;
        [SerializeField] float tailSwayFrequency = 0.3f;

        Vector3 torsoBase;
        Quaternion tailBase;

        void Awake()
        {
            if (torso != null)
                torsoBase = torso.localPosition;
            if (tailPivot != null)
                tailBase = tailPivot.localRotation;
        }

        void Update()
        {
            const float TwoPi = Mathf.PI * 2f;
            float t = Time.time;
            if (torso != null)
                torso.localPosition = torsoBase + Vector3.up * (Mathf.Sin(t * bobFrequency * TwoPi) * bobAmount);
            if (tailPivot != null)
                tailPivot.localRotation = tailBase * Quaternion.Euler(0f, Mathf.Sin(t * tailSwayFrequency * TwoPi) * tailSwayDegrees, 0f);
        }
    }
}
