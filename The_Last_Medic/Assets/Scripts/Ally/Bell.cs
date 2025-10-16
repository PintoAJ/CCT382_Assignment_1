using UnityEngine;

public class Bell : MonoBehaviour, IBulletHittable
{
    [Header("Bell Logic")]
    public float radius = 6f;
    public LayerMask enemyMask = ~0;
    public float cooldown = 2.0f;

    float _lastUse = -999f;
    bool _firstUse = true;

    [Header("Pulse FX")]
    public Color PulseColor = new Color(0.25f, 0.8f, 1f, 0.9f);
    public float PulseEndRadius = 6f;
    public float PulseDuration = 0.35f;

    public void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal, GameObject projectile)
    {
        if (!_firstUse && Time.time < _lastUse + cooldown) return;
        _firstUse = false;
        _lastUse = Time.time;

        // spawn centered on the bell, sized to bell bounds, nudged toward camera
        ExpandingSphereFX.SpawnAtObject(transform, PulseColor, PulseEndRadius, PulseDuration);

        // distract nearby enemies
        var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask);
        foreach (var c in colliders)
        {
            var zc = c.GetComponent<ZombieController>() ?? c.GetComponentInParent<ZombieController>();
            if (zc != null) zc.Distracted(transform);
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
