using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame
{
    public class CatInput : MonoBehaviour
    {
        InputAction move;
        InputAction run;
        InputAction jump;
        InputAction attack;

        public Vector2 Move => move.ReadValue<Vector2>();
        public bool Run => run.IsPressed();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool AttackPressed => attack.WasPressedThisFrame();

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            run.AddBinding("<Gamepad>/leftStickPress");

            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");
        }

        void OnEnable()
        {
            move.Enable();
            run.Enable();
            jump.Enable();
            attack.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            run.Disable();
            jump.Disable();
            attack.Disable();
        }

        void OnDestroy()
        {
            move.Dispose();
            run.Dispose();
            jump.Dispose();
            attack.Dispose();
        }
    }
}
