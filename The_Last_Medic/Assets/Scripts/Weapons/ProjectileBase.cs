using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Simplified but compatible base class for all projectiles.
    /// Keeps UnityEvent signature like the original FPS Template.
    /// </summary>
    public class ProjectileBase : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Automatically destroy the projectile after this many seconds")]
        public float MaxLifeTime = 5f;

        [Tooltip("Destroy projectile immediately on hit")]
        public bool DestroyOnHit = true;

        // Original FPS fields kept for naming compatibility
        public GameObject Owner { get; set; }
        public Vector3 InitialPosition { get; set; }
        public Vector3 InitialDirection { get; set; }
        public Vector3 InheritedMuzzleVelocity { get; set; }
        public float InitialCharge { get; set; }

        public UnityAction OnShoot;

        private float _spawnTime;

        public virtual void Shoot()
        {
            InitialPosition = transform.position;
            InitialDirection = transform.forward;
            _spawnTime = Time.time;
            Debug.Log($"Shoot at {transform.position} dir {transform.forward}");
            OnShoot?.Invoke();
        }

        protected virtual void Update()
        {
            if (Time.time - _spawnTime > MaxLifeTime)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (DestroyOnHit)
                Destroy(gameObject);
        }
    }
}
