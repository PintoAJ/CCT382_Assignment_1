using UnityEngine;

public class ZombieStats : CharacterStats
{

    [SerializeField] private int damage;
    public float attackSpeed;
    [SerializeField] private bool canAttack;

    public void DealDamage(CharacterStats statsToDamage)
    {
        statsToDamage.TakeDamage(damage);
    }

    public override void Die()
    {
        base.Die();
        Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitVariables();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void InitVariables()
    {
        maxHealth = 25;
        SetHealthTo(maxHealth);
        isDead = false;

        damage = 10;
        attackSpeed = 1.5f;
        canAttack = true;
    }
}
