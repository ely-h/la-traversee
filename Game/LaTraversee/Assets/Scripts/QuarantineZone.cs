using UnityEngine;

public class QuarantineZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le joueur est dans la zone !");
            other.GetComponent<SpriteRenderer>().color = Color.cyan;
        }
    }
}