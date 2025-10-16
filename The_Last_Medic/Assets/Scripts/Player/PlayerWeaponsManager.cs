using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerWeaponsManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Main player camera (used for shooting direction)")]
        public Camera PlayerCamera;

        [Tooltip("Parent transform where the weapon model is attached")]
        public Transform WeaponSocket;

        [Tooltip("Weapon prefab to use (assign your sniper prefab here)")]
        public GameObject EquippedWeaponPrefab;

        [Tooltip("Layer for first-person weapon visuals")]
        public LayerMask FpsWeaponLayer;

        [Header("Aiming")]
        public bool IsAiming { get; private set; }

        private PlayerInputHandler _input;
        private WeaponController _activeWeapon;

        public UnityAction<WeaponController> OnWeaponFired;

        void Start()
        {
            _input = GetComponent<PlayerInputHandler>();

            if (EquippedWeaponPrefab != null && WeaponSocket != null)
            {
                // Instantiate prefab under the socket
                GameObject weaponGO = Instantiate(EquippedWeaponPrefab, WeaponSocket);

                // Get the controller (on root or in children)
                _activeWeapon = weaponGO.GetComponent<WeaponController>();
                if (_activeWeapon == null)
                    _activeWeapon = weaponGO.GetComponentInChildren<WeaponController>(true);

                if (_activeWeapon != null)
                {
                    _activeWeapon.Owner = gameObject;
                    _activeWeapon.ShowWeapon(true);

                    // Apply weapon layer to the whole hierarchy (optional)
                    if (FpsWeaponLayer.value != 0)
                    {
                        int layerIndex = Mathf.RoundToInt(Mathf.Log(FpsWeaponLayer.value, 2));
                        foreach (Transform t in weaponGO.GetComponentsInChildren<Transform>(true))
                            t.gameObject.layer = layerIndex;
                    }
                }
                else
                {
                    Debug.LogError("[PlayerWeaponsManager] The equipped weapon prefab has no WeaponController component.", this);
                }
            }
            else
            {
                Debug.LogWarning("[PlayerWeaponsManager] Missing EquippedWeaponPrefab or WeaponSocket.", this);
            }
        }

        void Update()
        {
            if (_activeWeapon == null || PlayerCamera == null)
                return;

            // Aim (right mouse)
            IsAiming = _input.GetAimInputHeld();

            // Shoot (left mouse)
            if (_input.GetFireInputDown())
            {
                bool fired = _activeWeapon.HandleShoot(PlayerCamera.transform);
                if (fired)
                    OnWeaponFired?.Invoke(_activeWeapon);
            }
        }
    }
}
