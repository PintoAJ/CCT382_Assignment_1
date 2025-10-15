using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    private float timeOfLastAttack = 0;
    private bool hasStopped = false;
    private bool isDistracted = false;
    public Transform distraction;
    public float attentionSpan = 10;

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
        //Debug.Log("waypoint index at start:" + waypointIndex);
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    protected virtual void Update()
    {
        if (!isDistracted)
        {
            bool openCombat = false;
            bool targetSpotted = false;
            float min_dist = Mathf.Infinity;

            // determine if any Ally is valid target
            Collider[] alliesInRange = Physics.OverlapSphere(transform.position, stats.viewRadius, allyMask);

            // chase ally if possible
            foreach (Collider c in alliesInRange)
            {
                Transform ally = c.transform;
                Vector3 direction = (c.transform.position - transform.position).normalized;
                AllyStats allyStats = c.transform.GetComponent<AllyStats>();

                // Ally in open combat, easier to spot
                if (allyStats.state == AllyStats.AllyState.COMBAT)
                {
                    if (Vector3.Distance(c.transform.position, transform.position) < min_dist)
                    {
                        target = c.transform;
                        targetSpotted = true;
                        openCombat = true;
                        min_dist = Vector3.Distance(c.transform.position, transform.position);
                    }
                }

                // Idle patrolling, harder to spot Ally
                else if (allyStats.state == AllyStats.AllyState.HIDDEN)
                {
                    if (Vector3.Angle(transform.forward, direction) < stats.viewAngle &&
                        Vector3.Distance(c.transform.position, transform.position) < min_dist &&
                        !openCombat)
                    {
                        target = c.transform;
                        targetSpotted = true;
                        min_dist = Vector3.Distance(c.transform.position, transform.position);
                    }
                }

            }

            if (targetSpotted)
            {
                MoveToTarget();
            }
            else
            {
                Patrol();
            }
        }
        else
        {
            RotateToTarget();

            if (attentionSpan > 0)
            {
                attentionSpan -= Time.time;
            }
            else
            {
                isDistracted = false;
                attentionSpan = 10;
            }
        }
    }
    
    public void Distracted(Transform source)
    {
        isDistracted = true;
        distraction = source;
    }
    
    public void AssignRoute(Transform[] route)
    {
        foreach (Transform t in route)
        {
            waypoints.Add(t);
        }
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
                AttackTarget(target);
            }
        }
        else if (hasStopped)
        {
            hasStopped = false;
        }
    }

    private void Patrol()
    {
        //Debug.Log("waypoint index at start:" + waypointIndex);
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
        
        if (isDistracted)
        {
            direction = distraction.position - transform.position;
        }
        
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = rotation;
    }

    protected virtual void AttackTarget(Transform target)
    {
        CharacterStats targetStats = target.GetComponent<CharacterStats>();
        stats.DealDamage(targetStats);
    }

    private void GetReferences()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<ZombieStats>();
    }
}
