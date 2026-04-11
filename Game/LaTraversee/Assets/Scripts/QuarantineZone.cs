using UnityEngine;
public class QuarantineZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            RepousserZombie(other);
            return;
        }
        if (other.CompareTag("Player"))
        {
            Debug.Log("Survivant en sécurité dans la quarantaine !");
            PlayerCollision col = other.GetComponent<PlayerCollision>();
            if (col != null) col.isInQuarantine = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            RepousserZombie(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le survivant est sorti de la quarantaine !");
            PlayerCollision col = other.GetComponent<PlayerCollision>();
            if (col != null) col.isInQuarantine = false;
        }
    }

    private void RepousserZombie(Collider2D other)
    {
        Vector2 direction = (other.transform.position - transform.position).normalized;
        Collider2D col = GetComponent<Collider2D>();
        Vector2 closestPoint = col.ClosestPoint(other.transform.position);
        other.transform.position = (Vector2)transform.position + direction * (Vector2.Distance(transform.position, closestPoint) + 0.1f);
    }
}