using UnityEngine;
using UnityEngine.InputSystem; // New Input System

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Minimal input handler for movement, look, jump, fire, aim, sprint, crouch.
    /// No Input Actions asset required.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Look Settings")]
        [Tooltip("Sensitivity multiplier for looking around with the mouse.")]
        public float LookSensitivity = 1f;

        [Tooltip("Invert the Y-axis for mouse look.")]
        public bool InvertYAxis = false;

        [Tooltip("Invert the X-axis for mouse look.")]
        public bool InvertXAxis = false;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpPressed;
        private bool _firePressed;
        private bool _aimHeld;
        private bool _sprintHeld;
        private bool _crouchHeld;

        void Update()
        {
            // Movement (WASD)
            _moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) _moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) _moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) _moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) _moveInput.x += 1;
            _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);

            // Jump
            _jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;

            // Look (mouse delta)
            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            _lookInput = mouseDelta * LookSensitivity;
            if (InvertXAxis) _lookInput.x = -_lookInput.x;
            if (InvertYAxis) _lookInput.y = -_lookInput.y;

            // Fire / Aim / Sprint / Crouch
            _firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            _aimHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
            _sprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            _crouchHeld = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        }

        // ---- Accessors consumed by CharacterController & WeaponsManager ----

        public Vector3 GetMoveInput()
        {
            return new Vector3(_moveInput.x, 0f, _moveInput.y);
        }

        public float GetLookInputsHorizontal() => _lookInput.x;
        public float GetLookInputsVertical() => _lookInput.y;

        public bool GetJumpInputDown() => _jumpPressed;

        public bool GetFireInputHeld() => _firePressed;
        public bool GetFireInputDown() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public bool GetFireInputReleased() => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        public bool GetAimInputHeld() => _aimHeld;
        public bool GetSprintInputHeld() => _sprintHeld;

        public bool GetCrouchInputHeld() => _crouchHeld;
        public bool GetCrouchInputDown() =>
            Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame;
        public bool GetCrouchInputReleased() =>
            Keyboard.current.leftCtrlKey.wasReleasedThisFrame || Keyboard.current.rightCtrlKey.wasReleasedThisFrame;
    }
}
