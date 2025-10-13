using Unity.VisualScripting;
using UnityEngine;

public class AllyController : MonoBehaviour
{

    private float timeOfLastAttack = 0;
    private Animator anim = null;
    private AllyStats stats = null;
    [SerializeField] private Transform target;
    public LayerMask enemyMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetReferences();
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, stats.viewRadius, enemyMask);
        float min_dist = Mathf.Infinity;

        foreach (Collider c in enemiesInRange)
        {
            //GameObject enemy = c.gameObject;
            RaycastHit hit;
            Vector3 direction = (c.transform.position - transform.position).normalized;

            if (Physics.Raycast(transform.position, direction, out hit, stats.viewRadius))
            {

                // check if Ally has direct line of sight on closest Enemy
                if (hit.collider.transform == c.transform &&
                    Vector3.Distance(transform.position, hit.collider.transform.position) < min_dist)
                {
                    target = c.transform;
                }
            }
        }

        if (target != null)
        {
            RotateToTarget();
        }

        if (stats.state == AllyStats.AllyState.COMBAT)
        { 
            if (Time.time >= timeOfLastAttack + stats.attackSpeed)
            {
                timeOfLastAttack = Time.time;
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                AttackTarget(targetStats);
            }
        }
    }

    private void RotateToTarget()
    {
        Vector3 direction = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = rotation;
    }

    private void AttackTarget(CharacterStats statsToDamage)
    {
        anim.SetTrigger("Attack");
        stats.DealDamage(statsToDamage);
    }
    
    private void GetReferences()
    {
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<AllyStats>();
    }
}
