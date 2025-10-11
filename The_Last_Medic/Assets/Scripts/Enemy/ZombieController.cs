using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    private float timeOfLastAttack = 0;
    private bool hasStopped = false;
    private NavMeshAgent agent = null;
    private Animator anim = null; 
    private ZombieStats stats = null;
    [SerializeField] private Transform target;

    private void Start()
    {
        GetReferences();
    }

    private void Update()
    {
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        agent.SetDestination(target.position);
        anim.SetFloat("Speed", 1f, 0.3f, Time.deltaTime);
        RotateToTarget();

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= agent.stoppingDistance)
        {
            anim.SetFloat("Speed", 0f);

            // attack 
            if (!hasStopped)
            {
                hasStopped = true;
                timeOfLastAttack = Time.time;
            }

            if (Time.time >= timeOfLastAttack + stats.attackSpeed)
            {
                timeOfLastAttack = Time.time;
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                AttackTarget(targetStats);
            }
        }
        else if (hasStopped)
        {
            hasStopped = false;
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
        stats.DealDamage(statsToDamage);
    }

    private void GetReferences()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<ZombieStats>();
    }
}
