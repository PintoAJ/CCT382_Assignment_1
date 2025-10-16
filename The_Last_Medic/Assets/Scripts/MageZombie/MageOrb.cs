using UnityEngine;

public class MageOrb : MonoBehaviour
{
    public Transform dest;
    public float speed = 5f;
    public int damage = 10;
    public float hitRadius = 0.3f;

    // Height offset to aim above the target (e.g., head or chest level)
    public float targetHeightOffset = 2f;

    public void Init(Transform target)
    {
        dest = target;
    }

    void Update()
    {
        if (dest == null)
        {
            Destroy(gameObject);
            return;
        }

        // Target 3 units above their origin
        Vector3 targetPos = dest.position + Vector3.up * targetHeightOffset;

        // Move toward that elevated point
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Face movement direction
        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        // Check for hit radius
        if (Vector3.Distance(transform.position, targetPos) <= hitRadius)
        {
            var targetStats = dest.GetComponent<CharacterStats>() ?? dest.GetComponentInParent<CharacterStats>();
            if (targetStats != null)
                targetStats.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
