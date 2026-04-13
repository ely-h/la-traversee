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

    public void Infect()
    {
        // Gestion de l'infection - bloquée si en quarantaine ou déjà infecté/safe
        if (isSafe || isInfected || isInQuarantine) return;

        isInfected = true;
        Debug.Log($"Joueur {playerId} est infecté !");

        PlayerSpriteController sprCtrl = GetComponent<PlayerSpriteController>();
        if (sprCtrl != null) sprCtrl.SetState(PlayerState.Infected);
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

    // Gestion des collisions solides entre les joueurs
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Infect();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isSafe) return;

        // Sécurité au cas où un zombie Trigger touche le joueur
        if (other.CompareTag("Enemy"))
        {
            Infect();
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