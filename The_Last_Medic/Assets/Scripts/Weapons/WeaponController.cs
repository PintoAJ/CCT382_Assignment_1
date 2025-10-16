using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Minimal weapon: spawns a projectile from a muzzle when fired.
    /// No reload, no spread, no ammo. Works with PlayerWeaponsManager and ProjectileDetect.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Where the projectile spawns from")]
        public Transform MuzzleTransform;

        [Tooltip("Projectile prefab that this weapon fires (must have ProjectileBase on it)")]
        public GameObject ProjectilePrefab;

        [Header("Timing")]
        [Tooltip("Time between shots (seconds). 0.8 ≈ slow sniper")]
        public float FireRate = 0.8f;

        [Header("Optional Visuals")]
        [Tooltip("Optional muzzle flash object that will be toggled briefly on fire")]
        public GameObject MuzzleFlash;

        // Populated by PlayerWeaponsManager when the weapon is spawned
        public GameObject Owner { get; set; }

        float _lastFireTime = Mathf.NegativeInfinity;

        /// <summary>
        /// Called by PlayerWeaponsManager. Returns true if a shot was fired.
        /// </summary>
        public bool HandleShoot(Transform aimFrom)
        {
            if (!CanFire()) return false;
            if (ProjectilePrefab == null || MuzzleTransform == null) return false;

            _lastFireTime = Time.time;

            // Spawn projectile GO and fetch its ProjectileBase
            Quaternion rotation = aimFrom != null ? aimFrom.rotation : MuzzleTransform.rotation;
            GameObject projGO = Instantiate(ProjectilePrefab, MuzzleTransform.position, rotation);

            var projectile = projGO.GetComponent<ProjectileBase>();
            if (projectile == null)
            {
                Debug.LogError("[WeaponController] ProjectilePrefab has no ProjectileBase component.", this);
                return false;
            }

            // Wire projectile data
            projectile.Owner = Owner != null ? Owner : gameObject;
            projectile.InitialPosition = MuzzleTransform.position;
            projectile.InheritedMuzzleVelocity = Vector3.zero; // set if you later add weapon sway/rigidbody velocity
            projectile.Shoot();

            // Optional flash
            if (MuzzleFlash != null)
            {
                MuzzleFlash.SetActive(false);
                MuzzleFlash.SetActive(true); // quick toggle to restart any particle/anim
            }

            return true;
        }

        public void ShowWeapon(bool show)
        {
            gameObject.SetActive(show);
        }

        bool CanFire()
        {
            if (FireRate <= 0f) return true;
            return Time.time >= _lastFireTime + FireRate;
        }
    }
}
