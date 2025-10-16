using UnityEngine;

public interface IBulletHittable
{
    // Called by ProjectileDetect when a projectile hits this object (or one of its children).
    void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal, GameObject projectile);
}
