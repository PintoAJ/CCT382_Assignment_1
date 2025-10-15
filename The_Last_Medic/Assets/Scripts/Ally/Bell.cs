using UnityEngine;

public class Bell : MonoBehaviour
{
    public float radius;
    public LayerMask enemyMask;
    public float cooldown;
    public float lastUse = 0;
    private bool firstUse = true;

    public void DistractEnemies()
    {
        if (firstUse || Time.time >= lastUse + cooldown)
        {
            firstUse = false;
            lastUse = Time.time;
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, enemyMask);

            foreach (Collider c in colliders)
            {
                ZombieController zc = c.gameObject.GetComponent<ZombieController>();
                zc.Distracted(transform);
            }
        }
    }
}