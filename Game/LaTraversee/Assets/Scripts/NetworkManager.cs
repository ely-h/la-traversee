using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using TMPro;

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

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    public TMPro.TextMeshProUGUI chronoText; //time text UI
    public float tempsRestant = 60f; // 60 secondes
    public bool partieEnCours = true;

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

        if (partieEnCours)
        {
            tempsRestant -= Time.deltaTime;//enleve le temps ecoule a chaque frame

            if (chronoText != null)
            {
                chronoText.text = Mathf.CeilToInt(tempsRestant).ToString();
            }

            if (tempsRestant <= 0)
            {
                tempsRestant = 0;
                partieEnCours = false;
                chronoText.text = "VICTOIRE DES SURVIVANTS !";
            }
        }

        if (partieEnCours)
        {
            // Deplacement des joueurs
            foreach (var kvp in players)
            {
                string playerId = kvp.Key;
                GameObject playerObj = kvp.Value;

                if (playerObj != null && playerInputs.ContainsKey(playerId))
                {
                    Vector2 input = playerInputs[playerId];
                    if (input != Vector2.zero)
                    {
                        Vector3 newPos = playerObj.transform.position + (Vector3)(input * speed * Time.deltaTime);
                        newPos.x = Mathf.Clamp(newPos.x, -8.5f, 8.5f); //gauche/droite
                        newPos.y = Mathf.Clamp(newPos.y, -4.5f, 4.5f); //haut/bas
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
            GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
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
}