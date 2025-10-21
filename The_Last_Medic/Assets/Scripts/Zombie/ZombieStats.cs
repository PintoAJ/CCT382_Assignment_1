using UnityEngine;

public class ZombieStats : CharacterStats
{

    public int damage;
    public float attackSpeed;
    public float viewRadius;
    public float viewAngle;

    public void DealDamage(CharacterStats statsToDamage)
    {
        statsToDamage.TakeDamage(damage);
    }

    public override void Die()
    {
        base.Die();
        lm.ZombieDown();
        lm.AddScore(100);
    }

    public override void InitVariables()
    {
        maxHealth = 10;
        SetHealthTo(maxHealth);
        isDead = false;

        damage = 10;
        attackSpeed = 1.5f;
        viewRadius = 15;
        viewAngle = 45;
    }
}
