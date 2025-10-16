using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Minimal projectile that moves forward and reports the FIRST thing it hits.
    /// No damage, no VFX. Uses a swept sphere between frames to avoid tunneling.
    /// </summary>
    [RequireComponent(typeof(ProjectileBase))]
    public class ProjectileDetect : MonoBehaviour
    {
        [Header("Transforms")]
        [Tooltip("Previous-frame reference point (usually the projectile root)")]
        public Transform Root;

        [Tooltip("Front-most point (tip) of the projectile")]
        public Transform Tip;

        [Header("Movement")]
        [Tooltip("Meters per second")]
        public float Speed = 300f;

        [Tooltip("Add muzzle velocity from the weapon (optional)")]
        public bool InheritMuzzleVelocity = false;

        [Header("Collision")]
        [Tooltip("Radius of the swept sphere cast")]
        public float Radius = 0.02f;

        [Tooltip("Layers this projectile can hit")]
        public LayerMask HitLayers = ~0;

        [Tooltip("Ignore trigger colliders (recommended)")]
        public bool IgnoreTriggers = true;

        [Header("Events")]
        [Tooltip("Invoked once on the first valid hit: (hitObject, hitPoint, hitNormal)")]
        public HitUnityEvent OnFirstHit;

        /// <summary>
        /// Inspector-friendly event payload: (hitObject, hitPoint, hitNormal).
        /// </summary>
        [System.Serializable]
        public class HitUnityEvent : UnityEngine.Events.UnityEvent<GameObject, Vector3, Vector3> { }

        // C# event with full RaycastHit details (subscribe from code if you need it)
        public event System.Action<RaycastHit> OnFirstHitDetailed;

        // ---------- DEBUG (NEW) ----------
        [Header("Debug")]
        public bool LogHits = true;                  // NEW
        public bool ShowHitDecal = true;             // NEW
        public float DecalSize = 0.08f;              // NEW
        public float DecalLifetime = 3f;             // NEW
        public Color DecalColor = Color.red;         // NEW
        // ---------------------------------

        // --- private state ---
        ProjectileBase _base;
        Vector3 _lastRootPos;
        Vector3 _velocity;
        List<Collider> _ignored; // owner colliders to ignore

        const QueryTriggerInteraction QTI_Collide = QueryTriggerInteraction.Collide;
        const QueryTriggerInteraction QTI_Ignore = QueryTriggerInteraction.Ignore;

        void OnEnable()
        {
            _base = GetComponent<ProjectileBase>();
            _base.OnShoot += HandleShoot;
        }

        void OnDisable()
        {
            if (_base != null) _base.OnShoot -= HandleShoot;
        }

        void HandleShoot()
        {
            _lastRootPos = Root ? Root.position : transform.position;

            _velocity = transform.forward * Speed;
            if (InheritMuzzleVelocity)
                _velocity += _base.InheritedMuzzleVelocity;

            // Ignore owner collisions
            _ignored = new List<Collider>();
            if (_base.Owner)
            {
                var cols = _base.Owner.GetComponentsInChildren<Collider>();
                if (cols != null) _ignored.AddRange(cols);
            }
        }

        void Update()
        {
            // Move and orient
            float dt = Time.deltaTime;
            transform.position += _velocity * dt;
            if (_velocity.sqrMagnitude > 0f)
                transform.forward = _velocity.normalized;

            // Sweep between last frame and now
            Vector3 start = _lastRootPos;
            Vector3 end = Tip ? Tip.position : transform.position;
            Vector3 dir = end - start;
            float dist = dir.magnitude;

            if (dist > 0f)
            {
                var hits = Physics.SphereCastAll(
                    start,
                    Radius,
                    dir.normalized,
                    dist,
                    HitLayers,
                    IgnoreTriggers ? QTI_Ignore : QTI_Collide
                );

                RaycastHit best = default;
                bool found = false;
                float bestDist = Mathf.Infinity;

                for (int i = 0; i < hits.Length; i++)
                {
                    if (!IsValid(hits[i])) continue;
                    if (hits[i].distance < bestDist)
                    {
                        best = hits[i];
                        bestDist = hits[i].distance;
                        found = true;
                    }
                }

                if (found)
                {
                    // If we started inside a collider
                    if (LogHits) LogHit(best);
                    if (ShowHitDecal) SpawnDebugDecal(best.point, best.normal);

                    if (best.distance <= 0f)
                    {
                        best.point = Root ? Root.position : transform.position;
                        best.normal = -transform.forward;
                    }

                    // Fire events
                    OnFirstHit?.Invoke(best.collider.gameObject, best.point, best.normal);
                    OnFirstHitDetailed?.Invoke(best);

                    // Route hit handling through LogHit (logs, notifies IBulletHittable, optional FX)
                    LogHit(best);

                    // Clean up
                    if (_base.DestroyOnHit) Destroy(gameObject);
                    return;





                }
            }

            _lastRootPos = Root ? Root.position : transform.position;
        }

        bool IsValid(RaycastHit hit)
        {
            if (hit.collider == null) return false;
            if (_ignored != null && _ignored.Contains(hit.collider)) return false;
            if (IgnoreTriggers && hit.collider.isTrigger) return false;
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            var pos = Root ? Root.position : transform.position;
            Gizmos.DrawSphere(pos, Radius);
        }

        // ---------- NEW: helpers ----------
        void LogHit(RaycastHit hit)
        {
            var go = hit.collider ? hit.collider.gameObject : null;

            // 1) Console log (optional)
            if (LogHits && go != null)
            {
                Debug.Log(
                    $"Projectile hit: {go.name} (Tag: {go.tag}, Layer: {LayerMask.LayerToName(go.layer)}) @ {hit.point}",
                    go
                );
            }

            // 2) Notify any IBulletHittable on the object or its parents (Bell, Barrel, etc.)
            var hittable = hit.collider ? hit.collider.GetComponentInParent<IBulletHittable>() : null;
            if (hittable != null)
            {
                // This will trigger each object’s own response (e.g., expanding sphere, AI, explosions)
                hittable.OnBulletHit(hit.point, hit.normal, gameObject);
            }

            // 3) Optional generic debug decal so you can see the hit even on non-hittables
            if (ShowHitDecal)
                SpawnDebugDecal(hit.point, hit.normal);
        }


        void SpawnDebugDecal(Vector3 position, Vector3 normal)
        {
            var decal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            decal.transform.position = position;
            decal.transform.localScale = Vector3.one * DecalSize;
            decal.transform.rotation = Quaternion.LookRotation(normal);

            // simple visible color (works in URP/BiRP with default material)
            var r = decal.GetComponent<Renderer>();
            r.material.color = DecalColor;

            Destroy(decal.GetComponent<Collider>());
            Destroy(decal, DecalLifetime);
        }
        // ---------------------------------
    }
}
