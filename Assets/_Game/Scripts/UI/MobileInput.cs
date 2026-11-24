using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchLookZone lookZone;
        [SerializeField] private JumpButton jumpButton;
        [SerializeField] private ActionButton actionButton;
        [SerializeField] private ActionButton sprintButton;

        private bool wasGameplayLocked;

        private void OnEnable()
        {
            InputService.Current = this;
            wasGameplayLocked = GameplayInputGate.IsLocked;
            if (wasGameplayLocked) ClearPendingInput();
        }

        private void Update()
        {
            bool locked = GameplayInputGate.IsLocked;
            if (locked && !wasGameplayLocked)
                ClearPendingInput();
            wasGameplayLocked = locked;
        }

        public void ClearPendingInput()
        {
            joystick?.ResetInput();
            lookZone?.ClearInput();
            jumpButton?.ClearInput();
            actionButton?.ResetInput();
            sprintButton?.ResetInput();
        }

        private void OnDisable()
        {
            ClearPendingInput();
            if (ReferenceEquals(InputService.Current, this))
                InputService.Current = null;
        }

        public Vector2 Move
        {
            get
            {
                if (GameplayInputGate.IsLocked) return Vector2.zero;
                Vector2 touchMove = joystick != null ? joystick.Value : Vector2.zero;
                return touchMove.sqrMagnitude > 0.0001f
                    ? Vector2.ClampMagnitude(touchMove, 1f)
                    : ReadKeyboardMove();
            }
        }

        public Vector2 ConsumeLook()
        {
            Vector2 delta = lookZone != null ? lookZone.Consume() : Vector2.zero;
            return GameplayInputGate.IsLocked ? Vector2.zero : delta;
        }

        public bool ConsumeJump()
        {
            bool jump = jumpButton != null && jumpButton.Consume();
            Keyboard keyboard = Keyboard.current;
            bool keyPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            return !GameplayInputGate.IsLocked && (jump || keyPressed);
        }

        public bool SprintHeld
        {
            get
            {
                if (GameplayInputGate.IsLocked) return false;
                Keyboard keyboard = Keyboard.current;
                return (sprintButton != null && sprintButton.Held) ||
                       (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed));
            }
        }

        public bool ActionHeld
        {
            get
            {
                if (GameplayInputGate.IsLocked) return false;
                Keyboard keyboard = Keyboard.current;
                return (actionButton != null && actionButton.Held) ||
                       (keyboard != null && keyboard.eKey.isPressed);
            }
        }

        public bool ConsumeAction()
        {
            bool action = actionButton != null && actionButton.Consume();
            Keyboard keyboard = Keyboard.current;
            bool keyPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            return !GameplayInputGate.IsLocked && (action || keyPressed);
        }

        private static Vector2 ReadKeyboardMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 value = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
            return Vector2.ClampMagnitude(value, 1f);
        }
    }
}
