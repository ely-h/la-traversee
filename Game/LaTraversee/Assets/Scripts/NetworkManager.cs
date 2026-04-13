using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using TMPro;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

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
    private Dictionary<string, float> dashCooldownEndTimes = new Dictionary<string, float>();

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    public TMPro.TextMeshProUGUI chronoText; //time text UI
    public float tempsRestant = 90f;
    public bool partieEnCours = true;
    public int mancheCourante = 1;
    public bool enIntermission = false;

    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 5f; 

    public TMPro.TextMeshProUGUI compteurText;
    private Process localServerProcess;

    // audio
    public AudioSource audioSource;
    public AudioClip tickSound;
    public AudioClip intermissionMusic;
    private bool countdownSoundPlayed = false;


    void Start()
    {
        Debug.Log("Le script se lance bien !");
        StartLocalServer();

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
                    Vector2 input = new Vector2(data.x, -data.y);
                    if (input.magnitude < 0.1f) input = Vector2.zero;
                    // Le - car le sprite allait dans le sens inverse de l'input
                    playerInputs[data.id] = input;
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
                    // Vérifie si le cooldown est terminé
                    bool canDash = !dashCooldownEndTimes.ContainsKey(data.id) 
                               || Time.time >= dashCooldownEndTimes[data.id];

                    if (canDash) {
                        dashEndTimes[data.id] = Time.time + dashDuration;
                        dashCooldownEndTimes[data.id] = Time.time + dashCooldown;
                        Debug.Log("Dash activé pour : " + data.id);

                        // Optionnel : notifie le téléphone du cooldown
                        socket.Emit("dashCooldown", new { id = data.id, cooldown = dashCooldown });
                    } else {
                        float remaining = dashCooldownEndTimes[data.id] - Time.time;
                        Debug.Log($"Dash refusé pour {data.id}, cooldown restant : {remaining:F1}s");
                    }
                });
            }
        } catch (Exception ex) { Debug.LogError("Erreur Dash : " + ex.Message); }
    });
        // Lancement de la connexion
        socket.Connect();
    }

    private void StartLocalServer()
    {
        if (localServerProcess != null && !localServerProcess.HasExited)
        {
            Debug.Log("AutoServerStart: le serveur local est deja actif.");
            return;
        }

        string serverDirectory = ResolveServerDirectory();
        if (string.IsNullOrEmpty(serverDirectory))
        {
            Debug.LogError("AutoServerStart: dossier Server introuvable.");
            return;
        }

        string serverScript = Path.Combine(serverDirectory, "server.js");
        if (!File.Exists(serverScript))
        {
            Debug.LogError("AutoServerStart: server.js introuvable dans " + serverDirectory);
            return;
        }

        try
        {
            localServerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "\"" + serverScript + "\"",
                    WorkingDirectory = serverDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                },
                EnableRaisingEvents = true
            };

            localServerProcess.OutputDataReceived += (sender, args) => {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Debug.Log("[Server] " + args.Data);
                }
            };

            localServerProcess.ErrorDataReceived += (sender, args) => {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Debug.LogError("[Server] " + args.Data);
                }
            };

            localServerProcess.Exited += (sender, args) => {
                Debug.Log("AutoServerStart: le serveur local s'est arrete.");
            };

            localServerProcess.Start();
            localServerProcess.BeginOutputReadLine();
            localServerProcess.BeginErrorReadLine();
            Debug.Log("AutoServerStart: serveur local demarre automatiquement.");
        }
        catch (Exception ex)
        {
            Debug.LogError("AutoServerStart: echec du demarrage du serveur local: " + ex.Message);
        }
    }

    private string ResolveServerDirectory()
    {
        string[] candidates = new string[]
        {
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Server")),
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Server")),
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Server"))
        };

        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void ShutdownLocalServer()
    {
        if (localServerProcess == null || localServerProcess.HasExited)
        {
            return;
        }

        Debug.Log("AutoServerStart: arret propre du serveur local.");

        try
        {
            localServerProcess.StandardInput.WriteLine("shutdown");
            localServerProcess.StandardInput.Flush();

            if (!localServerProcess.WaitForExit(3000))
            {
                localServerProcess.Kill();
                localServerProcess.WaitForExit(1000);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("AutoServerStart: echec de l'arret du serveur local: " + ex.Message);
        }
        finally
        {
            localServerProcess.Dispose();
            localServerProcess = null;
        }
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

            // Son de décompte pour les 5 dernières secondes
            if (tempsRestant <= 5f && !countdownSoundPlayed)
            {
                countdownSoundPlayed = true;
                if (audioSource != null && tickSound != null)
                    audioSource.PlayOneShot(tickSound);
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
                        socket.Emit("gameOver", new { message = "LES SURVIVANTS ONT GAGNÉ !" });
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
            dashCooldownEndTimes.Remove(id); // Nettoyage
            dashEndTimes.Remove(id);         // Nettoyage
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("AutoServerStart: fermeture de l'application, extinction du serveur.");

        if (socket != null)
        {
            socket.Disconnect();
        }

        ShutdownLocalServer();
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
                socket.Emit("gameOver", new { message = "LES ZOMBIES ONT GAGNÉ !" });
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

                    if (socket != null) 
                    {
                        socket.Emit("playerReset", new { id = pId });
                    }
                }
            }
        }
        CheckZombiesWin();

        if (!partieEnCours) {
            enIntermission = false;
            yield break;
        }

        // Reset du flag pour la manche 2
        countdownSoundPlayed = false;

        // Lancement de la musique d'intermission
        if (audioSource != null && intermissionMusic != null){
            audioSource.clip = intermissionMusic;
            audioSource.loop = false;
            audioSource.Play();
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

        // Arrêt de la musique
        if (audioSource != null) audioSource.Stop();

        tempsRestant = 90f;
        enIntermission = false;
    }
}
