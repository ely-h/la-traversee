using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public string playerId;

    private NetworkManager netManager;
    private bool isInfected = false;

    void Start()
    {
        netManager = FindObjectOfType<NetworkManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInfected && other.CompareTag("Enemy"))
        {
            isInfected = true;
            Debug.Log($"Joueur {playerId} est infecté !");

            GetComponent<SpriteRenderer>().color = new Color(0.31f, 0.41f, 0.13f);

            // Changer le tag du joueur pour qu'il soit traité comme un ennemi
            gameObject.tag = "Enemy";

            // Dire au serv node.js que joueur est infecté
            if (netManager != null && netManager.socket != null)
            {
                netManager.socket.Emit("playerInfected", new { id = playerId });
            }
        }
    }
}