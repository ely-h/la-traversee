using UnityEngine;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;
    public float spawnInterval = 10f;//toutes les 10 secondes
    public float zoneDuration = 5f;//la zone reste 5 secondes

    void Start()
    {
        // Lance la fonction "Spawn" de manière répétée
        InvokeRepeating("SpawnZone", 2f, spawnInterval);
    }

    void SpawnZone()
    {
        //position aléatoire
        Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
        GameObject newZone = Instantiate(quarantinePrefab, randomPos, Quaternion.identity);
        Destroy(newZone, zoneDuration);
        
        Debug.Log("ZONE !");
    }
}