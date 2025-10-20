using UnityEngine;

public class MageZombieStats : ZombieStats
{
    public override void Die()
    {
        base.Die();
        lm.AddScore(400);
    }
}