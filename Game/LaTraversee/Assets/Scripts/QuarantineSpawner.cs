using UnityEngine;
using System.Collections;

public class QuarantineSpawner : MonoBehaviour
{
    public GameObject quarantinePrefab;
    public float tailleZone = 2f;

    private const float MAP_X_MIN = -17f;
    private const float MAP_X_MAX = 17f;
    private const float MAP_Y_MIN = -9.5f;
    private const float MAP_Y_MAX = 5f;

    void Start()
    {
        Debug.Log("Le Spawner est sur l'objet : " + gameObject.name);
        StartCoroutine(CycleZone());
    }

    private IEnumerator CycleZone()
    {
        while (true)
        {
            float marge = tailleZone / 2f;
            float X = Random.Range(MAP_X_MIN + marge, MAP_X_MAX - marge);
            float Y = Random.Range(MAP_Y_MIN + marge, MAP_Y_MAX - marge);
            Vector3 randomPosition = new Vector3(X, Y, 0);
            GameObject tempZone = Instantiate(quarantinePrefab, randomPosition, Quaternion.identity);
            tempZone.transform.localScale = new Vector3(tailleZone, tailleZone, 1);
            Debug.Log("ZONE apparue !");
            yield return new WaitForSeconds(5f);

            Destroy(tempZone);
            Debug.Log("ZONE disparue !");

            yield return new WaitForSeconds(5f);
        }
    }
}