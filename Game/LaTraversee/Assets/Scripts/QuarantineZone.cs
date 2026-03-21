using UnityEngine;

public class QuarantineZone : MonoBehaviour
{
    // des qu'un perso entre dans le carre ça se declenche
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("ce joueur est en zone quarantaine : " + other.name);
        if (other.CompareTag("Player")) {//tester couleur
            other.GetComponent<SpriteRenderer>().color = Color.cyan;
        }
    }
    // des qu'un perso sort
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("ce joueur est en zone quarantaine : " + other.name);
        if (other.CompareTag("Player")) {//test couleur
            other.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
}