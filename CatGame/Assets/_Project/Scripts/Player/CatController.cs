using UnityEngine;

namespace CatGame
{
    [RequireComponent(typeof(CharacterController), typeof(CatInput))]
    public class CatController : MonoBehaviour
    {
        static readonly int SpeedParam = Animator.StringToHash("Speed");
        static readonly int GroundedParam = Animator.StringToHash("Grounded");

        [Header("Movement")]
        [Tooltip("A cat's walk; much faster and the gait reads as a trot.")]
        [SerializeField] float walkSpeed = 1.6f;
        [SerializeField] float runSpeed = 7f;
        [SerializeField] float acceleration = 30f;
        [SerializeField] float turnSpeed = 720f;

        [Header("Jumping")]
        [SerializeField] float jumpHeight = 1.5f;
        [SerializeField] float gravity = -30f;
        [Tooltip("Grace period after walking off a ledge during which a jump still works.")]
        [SerializeField] float coyoteTime = 0.12f;

        [Header("References")]
        [Tooltip("Movement is relative to this. Defaults to the main camera.")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("Optional. Needs a float 'Speed' and a bool 'Grounded' parameter.")]
        [SerializeField] Animator animator;

        CharacterController body;
        CatInput input;
        Vector3 planarVelocity;
        float verticalVelocity;
        float lastGroundedTime = float.NegativeInfinity;

        public float SpeedMultiplier { get; set; } = 1f;
        // How the body actually moved last frame, after collisions (zero when walking into a wall).
        public Vector3 Velocity => body.velocity;
        public bool IsGrounded => body.isGrounded;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            input = GetComponent<CatInput>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            Vector3 moveDirection = GetCameraRelativeInput();
            float targetSpeed = (input.Run ? runSpeed : walkSpeed) * SpeedMultiplier;
            planarVelocity = Vector3.MoveTowards(planarVelocity, moveDirection * targetSpeed, acceleration * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            UpdateVertical();

            CollisionFlags flags = body.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;

            if (animator != null)
            {
                animator.SetFloat(SpeedParam, planarVelocity.magnitude);
                animator.SetBool(GroundedParam, body.isGrounded);
            }
        }

        Vector3 GetCameraRelativeInput()
        {
            Vector2 stick = Vector2.ClampMagnitude(input.Move, 1f);
            if (cameraTransform == null)
                return new Vector3(stick.x, 0f, stick.y);

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return forward * stick.y + right * stick.x;
        }

        void UpdateVertical()
        {
            if (body.isGrounded)
            {
                lastGroundedTime = Time.time;
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
            }

            if (input.JumpPressed && Time.time - lastGroundedTime <= coyoteTime)
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
                lastGroundedTime = float.NegativeInfinity;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
