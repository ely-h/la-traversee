using UnityEngine;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;
    public float tailleZone = 2f;

    void Start()
    {
        // 1. On vérifie que le script démarre bien
        Debug.Log("Le Spawner est sur l'objet : " + gameObject.name);

        // 2. On lance le chrono (10s de délai, puis toutes les 10s)
        InvokeRepeating("SpawnZone", 10f,10f);
    }

    void SpawnZone()
    {
        Debug.Log("ZONE !");

        //position aleatoire
        float X = Random.Range(-2f, 2f);
        float Y = Random.Range(-2f, 2f);
        Vector3 randomPosition = new Vector3(X, Y, 0);
        GameObject tempZone =Instantiate(quarantinePrefab, randomPosition, Quaternion.identity);
        tempZone.transform.localScale = new Vector3(tailleZone, tailleZone, 1);

        Destroy(tempZone, 5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
       //joueur entre
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le joueur safe");
            player.isSafe = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // joueur sort
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le joueur est SORTI de la zone !");
            player.isSafe = false;
        }
    }
}