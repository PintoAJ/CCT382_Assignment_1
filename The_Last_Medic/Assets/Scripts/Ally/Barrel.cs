using UnityEngine;

public class Barrel : MonoBehaviour, IBulletHittable
{
    [Header("Damage")]
    public int Damage = 20;
    public float DamageRadius = 4.0f;
    public LayerMask EnemyMask = ~0;

    [Header("Pulse FX")]
    public Color PulseColor = new Color(1f, 0.8f, 0.25f, 0.9f);
    public float PulseEndRadius = 4.0f;
    public float PulseDuration = 0.35f;

    [Header("Explosion (optional)")]
    public bool ApplyExplosionForce = true;
    public float ExplosionForce = 450f;
    public float ExplosionRadius = 4.0f;
    public float UpwardsModifier = 0.25f;
    public LayerMask PhysicsLayers = ~0;

    [Header("Usage")]
    public bool OneShot = true;
    bool _used;

    public void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal, GameObject projectile)
    {
        if (OneShot && _used) return;
        _used = true;

        // visible pulse at barrel center
        ExpandingSphereFX.SpawnAtObject(transform, PulseColor, PulseEndRadius, PulseDuration);

        // damage others (not self)
        var hits = Physics.OverlapSphere(transform.position, DamageRadius, EnemyMask);
        foreach (var c in hits)
        {
            // skip self / same root
            if (c.transform.root == transform.root) continue;

            var stats = c.GetComponent<CharacterStats>() ?? c.GetComponentInParent<CharacterStats>();
            if (stats != null) stats.TakeDamage(Damage);
        }

        // optional physics kick (skip self)
        if (ApplyExplosionForce)
        {
            var cols = Physics.OverlapSphere(transform.position, ExplosionRadius, PhysicsLayers);
            foreach (var col in cols)
            {
                if (col.attachedRigidbody == null) continue;
                if (col.transform.root == transform.root) continue; // don't launch the barrel itself
                col.attachedRigidbody.AddExplosionForce(
                    ExplosionForce, transform.position, ExplosionRadius, UpwardsModifier, ForceMode.Impulse
                );
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, DamageRadius);

        if (ApplyExplosionForce)
        {
            Gizmos.color = new Color(1f, 0.8f, 0.25f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }
#endif
}
