using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Minimal first-person controller: move, jump, mouse look.
    /// Works with PlayerInputHandler + PlayerWeaponsManager.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")]
        public Camera PlayerCamera;

        [Header("Movement")]
        public float MoveSpeed = 5f;
        public float SprintSpeed = 8f;
        public float JumpForce = 5f;
        public float Gravity = -9.81f;

        [Header("Mouse Look")]
        public float LookSensitivity = 1f;
        public float VerticalClamp = 89f;
        [Range(0.1f, 1f)] public float AimingRotationMultiplier = 0.6f;

        public bool IsGrounded { get; private set; }

        // Compatibility fields used by Weapon bobbing (keep these props)
        public float MaxSpeedOnGround => MoveSpeed;
        public float SprintSpeedModifier => SprintSpeed / Mathf.Max(0.01f, MoveSpeed);
        public float GravityDownForce => -Gravity;

        public float RotationMultiplier => _weapons && _weapons.IsAiming ? AimingRotationMultiplier : 1f;

        CharacterController _controller;
        PlayerInputHandler _input;
        PlayerWeaponsManager _weapons;
        float _verticalVelocity;
        float _cameraPitch;

        void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputHandler>();
            _weapons = GetComponent<PlayerWeaponsManager>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            HandleLook();
            HandleMovement();
        }

        void HandleLook()
        {
            float yaw = _input.GetLookInputsHorizontal() * LookSensitivity * RotationMultiplier;
            transform.Rotate(Vector3.up * yaw, Space.Self);

            float pitchDelta = _input.GetLookInputsVertical() * LookSensitivity * RotationMultiplier;
            _cameraPitch = Mathf.Clamp(_cameraPitch - pitchDelta, -VerticalClamp, VerticalClamp);

            if (PlayerCamera)
                PlayerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        void HandleMovement()
        {
            IsGrounded = _controller.isGrounded;

            Vector3 inputMove = _input.GetMoveInput();           // x,z in [-1,1]
            Vector3 worldMove = transform.TransformDirection(inputMove);

            float speed = _input.GetSprintInputHeld() ? SprintSpeed : MoveSpeed;

            if (IsGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f; // small stick to ground
                if (_input.GetJumpInputDown()) _verticalVelocity = JumpForce;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 velocity = worldMove * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        // Kept for compatibility with older code that might call this
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }
    }
}
