using Unity.VisualScripting;
using UnityEngine;

public class AllyController : MonoBehaviour
{

    private float timeOfLastAttack = 0;
    private Animator anim = null;
    private AllyStats stats = null;
    [SerializeField] private Transform target;
    public LayerMask enemyMask;
    private bool justRevived = false;
    private Vector3 pos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetReferences();
        pos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!stats.IsDead())
        {
            if (justRevived)
            {
                justRevived = false;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(Vector3.zero), 90);
                transform.position = Vector3.MoveTowards(transform.position, pos, 1);
            }


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

                if (stats.state == AllyStats.AllyState.COMBAT)
                {
                    if (Time.time >= timeOfLastAttack + stats.attackSpeed)
                    {
                        timeOfLastAttack = Time.time;
                        CharacterStats targetStats = target.GetComponent<CharacterStats>();
                        bool killConfirmed = targetStats.HealthLeft() <= stats.AttackStrength();
                        AttackTarget(targetStats);

                        /* If this doesn't work, replace with try-catch statement */
                        /* Alternatively, change Zombie's Die() method so DestroyGameObject() is subsituted with Deactivate (make intangible)*/
                        if (killConfirmed)
                        {
                            target = null;
                        }

                    }
                }
            }
        }
        else
        {
            justRevived = true;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(new Vector3(90, 0, 0)), 90);
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(pos.x, pos.y + 0.5f, pos.z), 0.5f);
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
