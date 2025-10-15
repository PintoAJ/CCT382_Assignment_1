using UnityEngine;

public class MageOrb : MonoBehaviour
{
    public Vector3 direction;
    public Transform dest;
    public float speed = 5f;
    public int damage = 10;

    public void Init(Transform target)
    {
        dest = target;
        direction = (target.position - transform.position).normalized;
    }
    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, direction, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, dest.position) < 0.3)
        {
            CharacterStats targetStats = dest.GetComponent<CharacterStats>();
            targetStats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}