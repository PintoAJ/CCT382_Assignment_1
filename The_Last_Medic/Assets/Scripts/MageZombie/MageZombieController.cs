using UnityEngine;

public class MageZombieController : ZombieController
{
    public GameObject mageOrb;

    protected override void AttackTarget(Transform target)
    {
        // spawn 3 units above the mage’s position
        Vector3 spawnPos = transform.position + Vector3.up * 2f;

        // aim at the elevated point on the target (3 units up)
        Vector3 toTarget = ((target.position + Vector3.up * 2f) - spawnPos).normalized;
        Quaternion rot = toTarget.sqrMagnitude > 0f ? Quaternion.LookRotation(toTarget) : transform.rotation;

        GameObject newMageOrb = Instantiate(mageOrb, spawnPos, rot);
        var orb = newMageOrb.GetComponent<MageOrb>();
        orb.Init(target);
    }
}
