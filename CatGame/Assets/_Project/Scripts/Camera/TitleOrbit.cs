using UnityEngine;

namespace CatGame
{
    public class TitleOrbit : MonoBehaviour
    {
        [SerializeField] Transform focus;
        [SerializeField] float radius = 4.2f;
        [SerializeField] float height = 1.4f;
        [SerializeField] float lookHeight = 0.9f;
        [SerializeField] float degreesPerSecond = 5f;
        [Tooltip("Starting angle around the focus; 180 is in front of it.")]
        [SerializeField] float startAngle = 155f;
        [Tooltip("Yaw applied after aiming, pushing the focus off-center toward a rule-of-thirds line.")]
        [SerializeField] float framingYaw = 14f;

        float angle;

        void OnEnable()
        {
            if (focus != null)
                angle = focus.eulerAngles.y + startAngle;
        }

        void LateUpdate()
        {
            if (focus == null)
                return;

            angle += degreesPerSecond * Time.unscaledDeltaTime;
            Vector3 position = focus.position + Quaternion.Euler(0f, angle, 0f) * Vector3.back * radius + Vector3.up * height;
            Vector3 lookAt = focus.position + Vector3.up * lookHeight;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position) * Quaternion.Euler(0f, framingYaw, 0f));
        }
    }
}
