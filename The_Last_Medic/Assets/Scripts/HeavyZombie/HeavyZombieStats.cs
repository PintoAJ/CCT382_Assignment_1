using UnityEngine;

public class HeavyZombieStats : ZombieStats
{
    public override void InitVariables()
    {
        maxHealth = 50;
        SetHealthTo(maxHealth);
        isDead = false;

        damage = 15;
        attackSpeed = 2f;
        viewRadius = 25;
    }

    public override void Die()
    {
        base.Die();
        lm.AddScore(400);
    }
}