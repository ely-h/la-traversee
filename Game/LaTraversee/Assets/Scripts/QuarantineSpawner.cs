using UnityEngine;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;
    public float tailleZone = 2f;

    void Start()
    {
        Debug.Log("Le Spawner est sur l'objet : " + gameObject.name);
        InvokeRepeating("SpawnZone", 10f, 10f);
    }

    void SpawnZone()
    {
        Debug.Log("ZONE !");
        float X = Random.Range(-2f, 2f);
        float Y = Random.Range(-2f, 2f);
        Vector3 randomPosition = new Vector3(X, Y, 0);
        GameObject tempZone = Instantiate(quarantinePrefab, randomPosition, Quaternion.identity);
        tempZone.transform.localScale = new Vector3(tailleZone, tailleZone, 1);
        Destroy(tempZone, 5f);
    }
}