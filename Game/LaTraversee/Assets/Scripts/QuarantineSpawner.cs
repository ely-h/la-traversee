using UnityEngine;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;

    void Start()
    {
        InvokeRepeating("SpawnZone", 2f, 10f);
    }

    void SpawnZone()
    {
        Debug.Log("ZONE !");
        Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
        Instantiate(quarantinePrefab, randomPos, Quaternion.identity);
    }
}