using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{

    [SerializeField] Transform[] spawners_1 = null;
    [SerializeField] Transform[] route_1 = null;

    [SerializeField] Transform[] spawners_2 = null;
    [SerializeField] Transform[] route_2 = null;

    [SerializeField] Transform[] spawners_3 = null;
    [SerializeField] Transform[] route_3 = null;

    [SerializeField] GameObject zombie;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnZombie(spawners_1, route_1);
        SpawnZombie(spawners_2, route_2);
        SpawnZombie(spawners_3, route_3);
    }
    
    private void SpawnZombie(Transform[] spawners, Transform[] route)
    {
        foreach (Transform t in spawners)
        {
            GameObject newZombie = Instantiate(zombie, t.position, t.rotation);
            ZombieController zc = newZombie.GetComponent<ZombieController>();
            zc.AssignRoute(route);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
