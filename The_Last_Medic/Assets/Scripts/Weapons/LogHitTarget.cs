using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Debug helper: hook this up to ProjectileDetect.OnFirstHit in the Inspector
    /// to log the object name and hit point/normal.
    /// </summary>
    public class LogHitTarget : MonoBehaviour
    {
        public void OnProjectileFirstHit(GameObject hitObject, Vector3 point, Vector3 normal)
        {
            string name = hitObject ? hitObject.name : "(null)";
            Debug.Log($"[Projectile] Hit {name} at {point} | normal {normal}");
        }
    }
}
