using System;
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
    public LayerMask allyMask;

    public List<Transform> waypoints;
    private int waypointIndex = 0;
    private float waitTime;

    private void Start()
    {
        GetReferences();
        waitTime = UnityEngine.Random.Range(1, 4);
        Debug.Log("waypoint index at start:" + waypointIndex);
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    private void Update()
    {
        bool targetSpotted = false;

        // determine if any Ally is valid target
        Collider[] alliesInRange = Physics.OverlapSphere(transform.position, stats.viewRadius, allyMask);

        // chase ally if possible
        for (int i = 0; i < alliesInRange.Length; i++)
        {
            Transform ally = alliesInRange[i].transform;
            // check if ally is in combat mode
            // GetComponent() for AllyStats, check InCombat()
            // set targetSpotted to true
            MoveToTarget();
        }

        // otherwise patrol
        if (!targetSpotted)
        {
            Patrol();
        }
    }
    
    public void AssignRoute(Transform[] route)
    {
        waypoints = new List<Transform>(route);
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

    private void Patrol()
    {
        Debug.Log("waypoint index at start:" + waypointIndex);
        agent.SetDestination(waypoints[waypointIndex].position);

        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[waypointIndex].position);
        if (distanceToWaypoint <= agent.stoppingDistance)
        {
            if (waitTime <= 0)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Count;
                agent.SetDestination(waypoints[waypointIndex].position);

                anim.SetFloat("Speed", 1f, 0.3f, Time.deltaTime);
                waitTime = UnityEngine.Random.Range(1, 4);
            }

            else
            {
                anim.SetFloat("Speed", 0f);
                waitTime -= Time.deltaTime;
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
        stats.DealDamage(statsToDamage);
    }

    private void GetReferences()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<ZombieStats>();
    }
}
