using UnityEngine;

public class ZombieStats : CharacterStats
{

    public int damage;
    public float attackSpeed;
    public float viewRadius;
    public float viewAngle;
    public GameObject levelManager;

    public void DealDamage(CharacterStats statsToDamage)
    {
        statsToDamage.TakeDamage(damage);
    }

    public override void Die()
    {
        base.Die();
        LevelManager lm = levelManager.GetComponent<LevelManager>();
        lm.ZombieDown();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitVariables();
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
