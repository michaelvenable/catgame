using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame
{
    public enum GameState { Title, Playing }

    public class GameFlow : MonoBehaviour
    {
        [SerializeField] CatInput playerInput;
        [SerializeField] ThirdPersonCamera gameplayCamera;
        [SerializeField] TitleOrbit titleCamera;
        [SerializeField] CanvasGroup titleScreen;
        [SerializeField] ScreenFader fader;
        [SerializeField] float fadeDuration = 0.3f;

        public GameState State { get; private set; }
        bool transitioning;

        IEnumerator Start()
        {
            EnterTitle();
            transitioning = true;
            fader.Snap(1f);
            yield return fader.Fade(0f, fadeDuration);
            transitioning = false;
        }

        void Update()
        {
            if (State == GameState.Title && Application.isFocused && AnyStartPressed())
                StartGame();
        }

        public void StartGame()
        {
            if (State != GameState.Title || transitioning)
                return;
            StartCoroutine(BeginPlay());
        }

        void EnterTitle()
        {
            State = GameState.Title;
            playerInput.enabled = false;
            gameplayCamera.enabled = false;
            titleCamera.enabled = true;
            titleScreen.alpha = 1f;
        }

        IEnumerator BeginPlay()
        {
            transitioning = true;
            yield return fader.Fade(1f, fadeDuration);

            titleScreen.alpha = 0f;
            titleCamera.enabled = false;
            gameplayCamera.enabled = true;
            playerInput.enabled = true;
            State = GameState.Playing;
            Debug.Log("[GameFlow] Title -> Playing");

            yield return fader.Fade(0f, fadeDuration);
            transitioning = false;
        }

        static bool AnyStartPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                foreach (var key in keyboard.allKeys)
                {
                    if (key != null && key.wasPressedThisFrame && !IsShortcutKey(key.keyCode))
                        return true;
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            var pad = Gamepad.current;
            return pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame);
        }

        // Modifiers and window-switching keys come from OS shortcuts, not a deliberate "press any key".
        static bool IsShortcutKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LeftMeta or Key.RightMeta or Key.Tab or Key.Escape;
    }
}
