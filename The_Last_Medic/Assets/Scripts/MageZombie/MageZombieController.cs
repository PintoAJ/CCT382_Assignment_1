using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MageZombieController : ZombieController
{

    public GameObject mageOrb;

    protected override void AttackTarget(Transform target)
    {
        Vector3 pos = new Vector3(transform.position.x, 3, transform.position.z);
        GameObject newMageOrb = Instantiate(mageOrb, pos, transform.rotation);
        MageOrb script = newMageOrb.GetComponent<MageOrb>();
        script.Init(target);
    }
}