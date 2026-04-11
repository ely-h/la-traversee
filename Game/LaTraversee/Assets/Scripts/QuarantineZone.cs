using UnityEngine;

public class QuarantineZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bloque les zombies
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Un zombie essaie de rentrer, refusé !");
            // Repousse le zombie hors de la zone
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (other.transform.position - transform.position).normalized;
                rb.linearVelocity = direction * 5f;
            }
            return;
        }

        // Survivant entre dans la zone
        if (other.CompareTag("Player"))
        {
            Debug.Log("Survivant en sécurité dans la quarantaine !");
            PlayerCollision col = other.GetComponent<PlayerCollision>();
            if (col != null) col.isSafe = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le survivant est sorti de la quarantaine !");
            PlayerCollision col = other.GetComponent<PlayerCollision>();
            if (col != null) col.isSafe = false;
        }
    }
}