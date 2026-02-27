using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;

public class MoveData
{
    public float x { get; set; }
    public float y { get; set; }
}

public class NetworkManager : MonoBehaviour
{
    public SocketIOUnity socket;
    public Transform playerTransform;
    public float speed = 5f;

    private Vector2 currentMoveInput = Vector2.zero;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    void Start()
    {
        var uri = new Uri("http://localhost:3000");
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
                    // On inverse l'axe Y ici pour corriger la difference Web/Unity
                    currentMoveInput = new Vector2(data.x, -data.y);
                });
            }
            catch (Exception ex)
            {
                EnqueueMainThreadAction(() => {
                    Debug.LogError("Erreur reseau : " + ex.Message);
                });
            }
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

        // Deplacement du joueur
        if (playerTransform != null && currentMoveInput != Vector2.zero)
        {
            playerTransform.Translate(currentMoveInput * speed * Time.deltaTime, Space.World);
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