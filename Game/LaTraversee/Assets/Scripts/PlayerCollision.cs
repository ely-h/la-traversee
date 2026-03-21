using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public string playerId;

    private NetworkManager netManager;
    private bool isInfected = false;
    public bool isSafe = false;

    void Start()
    {
        netManager = FindObjectOfType<NetworkManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isSafe)
            return;

        // Gestion de l'infection du joueur
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
                netManager.CheckZombiesWin();

            }

        }
        // Gestion de l'arrivée au bunker
        if (!isInfected && other.CompareTag("Bunker"))
        {
            isSafe = true;
            Debug.Log($"Joueur {playerId} a atteint le bunker !");

            // Effet visuel : on rend le joueur à moitié transparent pour montrer qu'il est intouchable
            Color currentColor = GetComponent<SpriteRenderer>().color;
            currentColor.a = 0.5f;
            GetComponent<SpriteRenderer>().color = currentColor;
        }
    }
}