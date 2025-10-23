using UnityEngine;

public class Bell : MonoBehaviour, IBulletHittable
{
    [Header("Bell Logic")]
    public float radius = 6f;
    public LayerMask enemyMask = ~0;
    public float cooldown = 15.0f;

    float _lastUse = -999f;
    bool _firstUse = true;

    [Header("Pulse FX")]
    public Color PulseColor = new Color(0.25f, 0.8f, 1f, 0.9f);
    public float PulseEndRadius = 6f;
    public float PulseDuration = 0.35f;
    public bool IsOnCooldown => !_firstUse && Time.time < _lastUse + cooldown;


    public void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal, GameObject projectile)
    {
        if (!_firstUse && Time.time < _lastUse + cooldown) return;
        _firstUse = false;
        _lastUse = Time.time;

        ExpandingSphereFX.SpawnAtObject(transform, PulseColor, PulseEndRadius, PulseDuration);

        // DEBUG START
        int maskVal = enemyMask.value;
        Debug.Log($"[Bell] OverlapSphere start at {transform.position}, r={radius}, mask={maskVal}");
        // Use Collide to include triggers regardless of project setting
        var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
        Debug.Log($"[Bell] Found {colliders.Length} colliders");
        // DEBUG END

        foreach (var c in colliders)
        {
            var zc = c.GetComponent<ZombieController>()
                   ?? c.GetComponentInParent<ZombieController>()
                   ?? c.GetComponentInChildren<ZombieController>(); // tolerant for L2 hierarchy

            if (zc != null)
            {
                Debug.Log($"[Bell] Distracting {zc.name} via {c.name}");
                zc.Distracted(transform);
            }
            else
            {
                // Helpful once: tells you which collider didn’t resolve to a zombie controller
                // Debug.Log($"[Bell] No ZombieController on/near {c.name}");
            }
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
