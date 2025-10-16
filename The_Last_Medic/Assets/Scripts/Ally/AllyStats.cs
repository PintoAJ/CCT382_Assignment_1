using UnityEngine;

public class AllyStats : CharacterStats
{

    public enum AllyState {DEAD, HIDDEN, COMBAT}
    [SerializeField] private int damage;
    public float attackSpeed;
    public float viewRadius;
    public AllyState state = AllyState.DEAD;

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

        // consider rotating model to lie flat on ground 
        // alternatively, find death animation
    }

    public void Revive()
    {
        state = AllyState.HIDDEN;
        SetHealthTo(maxHealth);
        isDead = false;

        // undo dying animation/changes
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        InitVariables();
    }

    public override void InitVariables()
    {
        maxHealth = 30;
        SetHealthTo(0);
        isDead = true;

        damage = 10;
        attackSpeed = 1.5f;
        viewRadius = 50;
    }
}
