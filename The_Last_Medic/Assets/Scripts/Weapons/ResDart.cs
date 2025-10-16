using UnityEngine;
using Unity.FPS.Gameplay; // for ProjectileDetect if it's in that namespace

/// <summary>
/// Res(urrection) dart: never deals damage. On impact:
/// - Sphere check for Allies and call Revive()
/// - That's it. It relies on IBulletHittable for Bell/Barrel behavior.
/// </summary>
[RequireComponent(typeof(ProjectileDetect))]
public class ResDart : MonoBehaviour
{
    [Header("Revive Settings")]
    [Tooltip("Radius around impact point to look for allies to revive.")]
    public float ReviveRadius = 3.0f;

    [Tooltip("Layers that contain allies.")]
    public LayerMask AllyMask;

    [Tooltip("If true, also heal non-dead allies to full on hit.")]
    public bool HealLivingAllies = false;

    ProjectileDetect _detector;

    void OnEnable()
    {
        _detector = GetComponent<ProjectileDetect>();
        _detector.OnFirstHitDetailed += HandleImpact;
    }

    void OnDisable()
    {
        if (_detector != null)
            _detector.OnFirstHitDetailed -= HandleImpact;
    }

    void HandleImpact(RaycastHit hit)
    {
        ReviveAlliesAround(hit.point);
        // Dart should be destroyed by ProjectileDetect if DestroyOnHit is set.
        // If not, uncomment:
        // Destroy(gameObject);
    }

    void ReviveAlliesAround(Vector3 center)
    {
        var cols = Physics.OverlapSphere(center, ReviveRadius, AllyMask);
        foreach (var c in cols)
        {
            var allyStats = c.GetComponentInParent<AllyStats>() ?? c.GetComponent<AllyStats>();
            if (allyStats == null) continue;

            // Only revive if dead; optionally heal living allies
            if (allyStats.state == AllyStats.AllyState.DEAD)
            {
                allyStats.Revive(); // Your AllyStats already implements this
            }
            else if (HealLivingAllies)
            {
                // allyStats.SetHealthTo(allyStats.MaxHealth); // Use the field directly
            }
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.3f);
        // Approximate where the dart currently is (during play impact we use the hit point)
        Gizmos.DrawSphere(transform.position, ReviveRadius);
    }
#endif
}
