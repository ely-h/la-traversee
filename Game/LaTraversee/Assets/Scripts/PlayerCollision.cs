using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public string playerId;

    private NetworkManager netManager;
    private bool isInfected = false;
    public bool isSafe = false;
    public bool isInQuarantine = false;

    void Start()
    {
        netManager = FindObjectOfType<NetworkManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isSafe) return;

        // Gestion de l'infection - bloquée si en quarantaine
        if (!isInfected && !isInQuarantine && other.CompareTag("Enemy"))
        {
            isInfected = true;
            Debug.Log($"Joueur {playerId} est infecté !");

            GetComponent<SpriteRenderer>().color = new Color(0.31f, 0.41f, 0.13f);
            gameObject.tag = "Enemy";

            // Camera shake de ta branche
            CameraShake shakeEngine = Camera.main.GetComponent<CameraShake>();
            if (shakeEngine != null) shakeEngine.TriggerShake();

            if (netManager != null && netManager.socket != null)
            {
                netManager.socket.Emit("playerInfected", new { id = playerId });
                netManager.CheckZombiesWin();
            }
        }

        // Gestion du bunker - toujours active
        if (!isInfected && other.CompareTag("Bunker"))
        {
            isSafe = true;
            Debug.Log($"Joueur {playerId} a atteint le bunker !");
            Color currentColor = GetComponent<SpriteRenderer>().color;
            currentColor.a = 0.5f;
            GetComponent<SpriteRenderer>().color = currentColor;

            // Env au serv node.js que le joueur est en sécurité
            if (netManager != null && netManager.socket != null)
            {
                netManager.socket.Emit("playerSafe", new { id = playerId });
            }
        }
    }
}