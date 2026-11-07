using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new(0f, 0.6f, 0f);
        [SerializeField] float distance = 4f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 70f;
        [SerializeField] float mouseSensitivity = 0.15f;
        [SerializeField] float gamepadSensitivity = 160f;
        [SerializeField] float followSharpness = 20f;

        [Header("Collision")]
        [SerializeField] float collisionRadius = 0.2f;
        [SerializeField] LayerMask collisionLayers = ~0;

        readonly RaycastHit[] hits = new RaycastHit[8];
        InputAction mouseLook;
        InputAction stickLook;
        float yaw;
        float pitch = 15f;
        Vector3 pivot;

        void Awake()
        {
            mouseLook = new InputAction("MouseLook", InputActionType.Value, "<Mouse>/delta");
            stickLook = new InputAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
        }

        void OnEnable()
        {
            mouseLook.Enable();
            stickLook.Enable();
            SetCursorLocked(true);
        }

        void OnDisable()
        {
            mouseLook.Disable();
            stickLook.Disable();
            SetCursorLocked(false);
        }

        void OnDestroy()
        {
            mouseLook.Dispose();
            stickLook.Dispose();
        }

        void Start()
        {
            if (target == null)
                return;
            yaw = target.eulerAngles.y;
            pivot = target.position + pivotOffset;
        }

        void LateUpdate()
        {
            // Re-lock in LateUpdate so gameplay Updates this frame still see the cursor as free.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);

            if (target == null)
                return;

            Vector2 look = stickLook.ReadValue<Vector2>() * (gamepadSensitivity * Time.deltaTime);
            if (Cursor.lockState == CursorLockMode.Locked)
                look += mouseLook.ReadValue<Vector2>() * mouseSensitivity;

            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            pivot = Vector3.Lerp(pivot, target.position + pivotOffset, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            transform.SetPositionAndRotation(pivot + back * GetUnobstructedDistance(back), rotation);
        }

        float GetUnobstructedDistance(Vector3 direction)
        {
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, direction, hits, distance, collisionLayers, QueryTriggerInteraction.Ignore);
            float closest = distance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(target))
                    continue;
                closest = Mathf.Min(closest, hits[i].distance);
            }
            return closest;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
