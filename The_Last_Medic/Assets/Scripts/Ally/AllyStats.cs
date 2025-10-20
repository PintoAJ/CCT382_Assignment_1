using UnityEngine;

public class AllyStats : CharacterStats
{

    public enum AllyState {DEAD, HIDDEN, COMBAT}
    [SerializeField] private int damage;
    public float attackSpeed;
    public float viewRadius;
    public AllyState state = AllyState.DEAD;
    public int lives = 3;

    public void DealDamage(CharacterStats statsToDamage)
    {
        statsToDamage.TakeDamage(damage);
    }

    public int AttackStrength()
    {
        return damage;
    }

    public override void Die()
    {
        base.Die();
        state = AllyState.DEAD;

        if (lives == 0)
        {
            lm.AllyDown();
            lm.AddScore(-300);
        }
    }

    public void Revive()
    {
        if (lives > 0)
        {
            lives--;

            if (lm.getOrder() == true)
            {
                state = AllyState.COMBAT;
            }
            else {
                state = AllyState.HIDDEN;
            }
            SetHealthTo(maxHealth);
            isDead = false;
        }
    }

    public void ReceiveOrder(string mode)
    {
        if (state != AllyState.DEAD)
        {
            // ordered to attack
            if (mode == "Attack")
            {
                state = AllyState.COMBAT;
            }

            // ordered to standdown
            else if (mode == "StandDown")
            {
                state = AllyState.HIDDEN;
            }
        }
    }

    public override void InitVariables()
    {
        maxHealth = 30;
        SetHealthTo(0);
        isDead = true;
        lives = 3;

        damage = 10;
        attackSpeed = 1.5f;
        viewRadius = 30;
    }
}
