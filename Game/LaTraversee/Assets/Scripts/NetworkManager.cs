using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using TMPro;

public class ActionData
{
    public string id { get; set; }
    public string type { get; set; }
}

public class JoinData
{
    public string id { get; set; }
    public string pseudo { get; set; }
    public string color { get; set; }
}

public class MoveData
{
    public string id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
}

public class DisconnectData
{
    public string id { get; set; }
}

public class NetworkManager : MonoBehaviour
{
    public SocketIOUnity socket;
    public GameObject playerPrefab;
    public float speed = 5f;

    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector2> playerInputs = new Dictionary<string, Vector2>();
    private Dictionary<string, float> dashEndTimes = new Dictionary<string, float>();

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    public TMPro.TextMeshProUGUI chronoText; //time text UI
    public float tempsRestant = 90f;
    public bool partieEnCours = true;
    public int mancheCourante = 1;
    public bool enIntermission = false;

    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.2f;

    public TMPro.TextMeshProUGUI compteurText;


    void Start()
    {
        Debug.Log("Le script se lance bien !");

        var uri = new Uri("http://localhost:4242");
        socket = new SocketIOUnity(uri);
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        // Ecoute connexion
        socket.OnConnected += (sender, e) => {
            EnqueueMainThreadAction(() => {
                Debug.Log("Unity connecte au serveur Node.js");
            });
        };

        // Recuperation des mouvements
        socket.On("playerMove", (response) => {
            try
            {
                MoveData data = response.GetValue<MoveData>();

                EnqueueMainThreadAction(() => {
                    // Le - car le sprite allait dans le sens inverse de l'input
                    playerInputs[data.id] = new Vector2(data.x, -data.y);
                });
            }
            catch (Exception ex)
            {
                EnqueueMainThreadAction(() => {
                    Debug.LogError("Erreur reseau : " + ex.Message);
                });
            }
        });

        socket.On("playerJoin", (response) => {
            try {
                JoinData data = response.GetValue<JoinData>();
                EnqueueMainThreadAction(() => {
                    // Si joueur n'existe pas, instance son carre
                    if (!players.ContainsKey(data.id)) {
                        SpawnPlayer(data.id, data.pseudo, data.color);
                    }
                });
            } catch (Exception ex) {
                EnqueueMainThreadAction(() => { Debug.LogError("Erreur Join : " + ex.Message); });
            }
        });

        //Si un joueur se deconnecte
        socket.On("playerDisconnect", (response) => {
            try
            {
                DisconnectData data = response.GetValue<DisconnectData>();
                EnqueueMainThreadAction(() => {
                    RemovePlayer(data.id);
                });
            }
            catch (Exception ex) { }
        });

        socket.On("playerAction", (response) => {
        try {
            ActionData data = response.GetValue<ActionData>();
            if (data.type == "DASH") {
                EnqueueMainThreadAction(() => {
                    dashEndTimes[data.id] = Time.time + dashDuration;
                    Debug.Log("Dash activé pour : " + data.id);
                });
            }
        } catch (Exception ex) { Debug.LogError("Erreur Dash : " + ex.Message); }
        });
        // Lancement de la connexion
        socket.Connect();
    }

    void Update()
    {
        // Execution de la file d'attente reseau sur le thread principal
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue().Invoke();
            }
        }

        if (partieEnCours && !enIntermission)
        {
            tempsRestant -= Time.deltaTime;

            if (chronoText != null)
            {
                chronoText.text = Mathf.CeilToInt(tempsRestant).ToString();
            }

            UpdateCompteur();
            CheckAllSurvivorsSafe();

            if (tempsRestant <= 0)
            {
                if (mancheCourante == 1)
                {
                    StartCoroutine(LancerIntermission());
                }
                else
                {
                    tempsRestant = 0;
                    partieEnCours = false;
                    chronoText.text = "VICTOIRE DES SURVIVANTS !";

                    if (socket != null)
                    {
                        socket.Emit("gameOver", new { message = "LES SURVIVANTS GAGNENT !" });
                    }
                }
            }
        }

        if (partieEnCours && !enIntermission)
        {
            // Deplacement des joueurs
            foreach (var kvp in players)
            {
                string playerId = kvp.Key;
                GameObject playerObj = kvp.Value;
                float currentSpeed = speed; // On part de la vitesse de base

                if (playerObj != null && playerInputs.ContainsKey(playerId))
                {
                    if (dashEndTimes.ContainsKey(playerId) && Time.time < dashEndTimes[playerId])
                    {
                        currentSpeed = speed * dashSpeedMultiplier; // On booste la vitesse
                    }

                    Vector2 input = playerInputs[playerId];

                    if (input != Vector2.zero)
                    {
                        Vector3 newPos = playerObj.transform.position + (Vector3)(input * currentSpeed * Time.deltaTime);

                        float limiteGauche = -8.5f;
                        PlayerCollision collision = playerObj.GetComponent<PlayerCollision>();

                        if(collision != null && collision.isSafe)
                        {
                            limiteGauche = 7.0f;
                        }

                        newPos.x = Mathf.Clamp(newPos.x, limiteGauche, 8.5f); // gauche/droite
                        newPos.y = Mathf.Clamp(newPos.y, -4.5f, 4.5f); // haut/bas
            
                        playerObj.transform.position = newPos;
                    }
                }
            }
        }


    }

    private void SpawnPlayer(string id, string pseudo = "Anonyme", string hexColor = "#ffffff")
    {
        if (playerPrefab != null)
        {
            float randomY = UnityEngine.Random.Range(-4f, 4f);
            Vector3 spawnPos = new Vector3(-8f, randomY, 0f);
            GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            newPlayer.name = pseudo + "_" + id;
            SpriteRenderer renderer = newPlayer.GetComponent<SpriteRenderer>();

            Color newColor = Color.white;

            if (renderer != null)
            {
                if (ColorUtility.TryParseHtmlString(hexColor, out newColor))
                {
                    renderer.color = newColor;
                }
            }
            TextMeshPro textComponent = newPlayer.GetComponentInChildren<TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = pseudo;
            }

            PlayerCollision collisionScript = newPlayer.GetComponent<PlayerCollision>();
            if (collisionScript != null)
            {
                collisionScript.playerId = id;
            }
            players.Add(id, newPlayer);
            playerInputs.Add(id, Vector2.zero);
        }
    }

    private void RemovePlayer(string id)
    {
        if (players.ContainsKey(id))
        {
            Destroy(players[id]);
            players.Remove(id);
            playerInputs.Remove(id);
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void OnDestroy()
    {
        if (socket != null) socket.Disconnect();
    }

    // maj du compteur de survivants et de zombies
    public void UpdateCompteur()
    {
        if (!partieEnCours) return;

        int survivants = 0;
        int zombies = 0;

        foreach (var kvp in players)
        {
            if (kvp.Value != null)
            {
                if (kvp.Value.CompareTag("Enemy"))
                {
                    zombies++;
                }
                else
                {
                    survivants++;
                }
            }
        }

        if (compteurText != null)
        {
            compteurText.text = $"SURVIVANTS : {survivants} | ZOMBIES : {zombies}";
        }
    }

    public void CheckAllSurvivorsSafe()
    {
        if (!partieEnCours) 
            return;

        int totalSurvivors = 0;
        int safeSurvivors = 0;

        foreach (var kvp in players)
        {
            if (kvp.Value != null && !kvp.Value.CompareTag("Enemy"))
            {
                totalSurvivors++;

                PlayerCollision col = kvp.Value.GetComponent<PlayerCollision>();
                if (col != null && col.isSafe)
                {
                    safeSurvivors++;
                }
            }
        }

        if (totalSurvivors > 0 && totalSurvivors == safeSurvivors)
        {
            Debug.Log("Tous les survivants sont sauvés! time stop.");
            tempsRestant = 0;
        }
    }

    public void CheckZombiesWin()
    {
        if (!partieEnCours) return;

        int survivants = 0;

        //compteur de survivants(joueurs pas tagg�s "Enemy")
        foreach (var kvp in players)
        {
            if (kvp.Value != null && !kvp.Value.CompareTag("Enemy"))
            {
                survivants++;
            }
        }

        //si il y a des joueurs et aucun survivant, les zombies gagnent
        if (players.Count > 0 && survivants == 0)
        {
            partieEnCours = false;

            if (chronoText != null)
            {
                chronoText.text = "VICTOIRE DES ZOMBIES !";
            }

            //envoi du message de fin de partie au serveur node.js et aux telephones
            if (socket != null)
            {
                socket.Emit("gameOver", new { message = "LES ZOMBIES ONT GAGN� !" });
            }
        }
    }

    // Coroutine d'intermission entre les manches
    private System.Collections.IEnumerator LancerIntermission()
    {
        enIntermission = true;
        mancheCourante = 2;

        // TP de tous les joueurs sur la ligne de départ et transformation des survivants restants en zombies
        foreach (var kvp in players)
        {
            string pId = kvp.Key;
            GameObject p = kvp.Value;

            if (p != null)
            {
                PlayerCollision col = p.GetComponent<PlayerCollision>();
                float randomY = UnityEngine.Random.Range(-4f, 4f);

                if (p.CompareTag("Enemy"))
                {
                    p.transform.position = new Vector3(0f, randomY, 0f);
                }
                else if (col != null && !col.isSafe)
                {
                    p.tag = "Enemy";
                    p.GetComponent<SpriteRenderer>().color = new Color(0.31f, 0.41f, 0.13f);
                    p.transform.position = new Vector3(0f, randomY, 0f);
                    if (socket != null) socket.Emit("playerInfected", new { id = pId });
                }
                else if (col != null && col.isSafe)
                {
                    col.isSafe = false;
                    Color c = p.GetComponent<SpriteRenderer>().color;
                    c.a = 1f;
                    p.GetComponent<SpriteRenderer>().color = c;
                    p.transform.position = new Vector3(-8f, randomY, 0f);
                }
            }
        }

        // Compte à rebours pour la manche 2
        if (chronoText != null) chronoText.text = "MANCHE 2 DANS...";
        yield return new WaitForSeconds(2f);

        if (chronoText != null) chronoText.text = "5...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "4...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "3...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "2...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "1...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "GO !!!";
        yield return new WaitForSeconds(0.5f);

        tempsRestant = 90f;
        enIntermission = false;
    }
}