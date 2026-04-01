using UnityEngine;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;

    void Start()
    {
        // 1. On vérifie que le script démarre bien
        Debug.Log("Le Spawner est ACTIF sur l'objet : " + gameObject.name);

        // 2. On lance le chrono (2s de délai, puis toutes les 10s)
        InvokeRepeating("SpawnZone", 10f,10f);
    }

    void SpawnZone()
    {
        Debug.Log("ZONE !");
        GameObject tempZone =Instantiate(quarantinePrefab, Vector3.zero, Quaternion.identity);
        Destroy(tempZone, 5f);
    }
}