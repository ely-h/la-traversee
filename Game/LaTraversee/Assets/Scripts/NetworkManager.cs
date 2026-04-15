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
    [SerializeField] private float survivorSpeed = 2.25f;
    [SerializeField] private float infectedSpeed = 2.35f;

    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector2> playerInputs = new Dictionary<string, Vector2>();
    private Dictionary<string, float> dashEndTimes = new Dictionary<string, float>();
    private Dictionary<string, float> dashCooldownEndTimes = new Dictionary<string, float>();

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    
    public bool canMove = false; // MODIFIED: Added canMove boolean

    public TMPro.TextMeshProUGUI chronoText; //time text UI
    public float tempsRestant = 90f;
    public bool partieEnCours = true;
    public int mancheCourante = 1;
    public bool enIntermission = false;

    public float dashSpeedMultiplier = 3f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 5f; 

    [Header("End Screen UI")]
    public GameObject gameOverPanel;
    public TMPro.TextMeshProUGUI gameOverTeamText;
    public WinnerListUI winnerListUI;
    public UnityEngine.UI.Button playAgainButton;

    public TMPro.TextMeshProUGUI compteurText;
    private Process localServerProcess;
    public bool startInLobby = true;
    private Coroutine arenaStartCoroutine;

    // audio
    public AudioSource audioSource;
    public AudioClip tickSound;
    public AudioClip intermissionMusic;
    public AudioClip zombiesWinSound;
    public AudioClip survivorsWinSound;
    public AudioClip gameMusic;
    private bool countdownSoundPlayed = false;

    void Start()
    {
        Debug.Log("Le script se lance bien !");
        StartLocalServer();
        partieEnCours = !startInLobby;
        tempsRestant = 90f;

        var uri = new Uri("http://localhost:4242");
        socket = new SocketIOUnity(uri);
        socket.JsonSerializer = new NewtonsoftJsonSerializer();
        
        if (startInLobby)
        {
            if (chronoText != null) chronoText.text = "EN ATTENTE";
            if (compteurText != null) compteurText.text = "Lobby";
        }
        else
        {
            PlayGameMusic();
        }

        // Wire Play Again button
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        // Ecoute connexion
        socket.OnConnected += (sender, e) => {
            EnqueueMainThreadAction(() => {
                Debug.Log("Unity connecte au serveur Node.js");
                socket.Emit("registerHost");
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

        // MODIFIED: REMOVED socket.On("game_started") to prevent overlap with LobbyUI

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
        // Standard Unity path for both Editor and Standalone builds
        string path = Path.Combine(Application.streamingAssetsPath, "Server");
        if (Directory.Exists(path))
        {
            return path;
        }

        Debug.LogError("AutoServerStart: Directory not found at: " + path);
        return null;
    }

    private void ShutdownLocalServer()
    {
        if (localServerProcess == null) return;

        Debug.Log("AutoServerStart: Stopping local server.");

        try
        {
            if (!localServerProcess.HasExited)
            {
                // 1. Try clean shutdown via stdin
                localServerProcess.StandardInput.WriteLine("shutdown");
                localServerProcess.StandardInput.Flush();

                // 2. Wait for exit, then kill if stubborn
                if (!localServerProcess.WaitForExit(2000))
                {
                    Debug.LogWarning("AutoServerStart: Server did not stop cleanly, killing process.");
                    localServerProcess.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("AutoServerStart: Shutdown error: " + ex.Message);
            // Forced kill if everything else fails
            try { localServerProcess?.Kill(); } catch { }
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
            CheckVictoryConditions(); // Verify if disconnect or infection emptied a team

            if (partieEnCours && tempsRestant <= 0)
            {
                if (mancheCourante == 1)
                {
                    StartCoroutine(LancerIntermission());
                }
                else
                {
                    TriggerGameOver(false);
                }
            }
        }

    }

    void FixedUpdate()
    {
        // MODIFIED: Added !canMove
        if (!partieEnCours || enIntermission || !canMove)
        {
            return;
        }

        foreach (var kvp in players)
        {
            string playerId = kvp.Key;
            GameObject playerObj = kvp.Value;
            if (playerObj == null || !playerInputs.ContainsKey(playerId))
            {
                continue;
            }

            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                continue;
            }

            float baseSpeed = playerObj.CompareTag("Enemy") ? infectedSpeed : survivorSpeed;
            float currentSpeed = baseSpeed;
            if (dashEndTimes.ContainsKey(playerId) && Time.time < dashEndTimes[playerId])
            {
                currentSpeed = baseSpeed * dashSpeedMultiplier;
            }

            Vector2 input = playerInputs[playerId];
            Vector2 newPos = rb.position + (input * currentSpeed * Time.fixedDeltaTime);

            PlayerSpriteController sprCtrl = playerObj.GetComponent<PlayerSpriteController>();
            if (sprCtrl != null) sprCtrl.UpdateDirection(input);

            float limiteGauche = -17.0f;
            PlayerCollision collision = playerObj.GetComponent<PlayerCollision>();
            if (collision != null && collision.isSafe)
            {
                limiteGauche = 15.0f;
            }

            newPos.x = Mathf.Clamp(newPos.x, limiteGauche, 17.0f);
            newPos.y = Mathf.Clamp(newPos.y, -9.5f, 5.0f);

            rb.MovePosition(newPos);
        }
    }

    private void SpawnPlayer(string id, string pseudo = "Anonyme", string hexColor = "#ffffff")
    {
        if (playerPrefab != null)
        {
            float randomY = UnityEngine.Random.Range(-9f, 4f);
            Vector3 spawnPos = new Vector3(-16f, randomY, 0f);
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
                
                // Nouveauté : Apply the same color to the TextMeshPro outline dynamically
                if (ColorUtility.TryParseHtmlString(hexColor, out newColor))
                {
                    textComponent.outlineColor = newColor;
                    textComponent.outlineWidth = 0.2f; // Ensure width is non-zero so we can see the Outline
                }
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

    public void EnqueueLobbyAction(Action action)
    {
        EnqueueMainThreadAction(action);
    }

    public void BeginGameFromLobby()
    {
        if (partieEnCours)
        {
            return;
        }
        partieEnCours = true;
        enIntermission = false;
        canMove = true; // MODIFIED: Set canMove = true
        mancheCourante = 1;
        tempsRestant = 90f;
        countdownSoundPlayed = false;
        if (chronoText != null) chronoText.text = Mathf.CeilToInt(tempsRestant).ToString();
        PlayGameMusic();
        Debug.Log("NetworkManager: partie lancee depuis le lobby.");

        // Sélection aléatoire des premiers zombies
        InitializeZombies();
    }

    public void InitializeZombies()
    {
        if (players.Count == 0) return;

        // 1 zombie pour 2-9 joueurs, 2 pour 10-19, etc.
        int zombieCount = 1 + (players.Count / 10);
        if (zombieCount > players.Count) zombieCount = players.Count;

        List<string> playerIds = new List<string>(players.Keys);

        // Mélange aléatoire type Fisher-Yates
        for (int i = 0; i < playerIds.Count; i++)
        {
            string temp = playerIds[i];
            int randomIndex = UnityEngine.Random.Range(i, playerIds.Count);
            playerIds[i] = playerIds[randomIndex];
            playerIds[randomIndex] = temp;
        }

        // Infection des 'zombieCount' premiers joueurs
        for (int i = 0; i < zombieCount; i++)
        {
            string zId = playerIds[i];
            GameObject zObj = players[zId];

            PlayerCollision col = zObj.GetComponent<PlayerCollision>();
            if (col != null)
            {
                col.Infect();
                SetPlayerPosition(zObj, Vector2.zero); // TP au centre
            }
        }
    }

    public void StartArenaPhase()
    {
        if (arenaStartCoroutine != null || partieEnCours)
        {
            return;
        }

        arenaStartCoroutine = StartCoroutine(ArenaStartCountdown());
    }

    private System.Collections.IEnumerator ArenaStartCountdown()
    {
        partieEnCours = false;
        enIntermission = false;
        canMove = false; // Add safe measure
        mancheCourante = 1;
        countdownSoundPlayed = false;

        if (compteurText != null)
        {
            compteurText.text = "Preparez-vous";
        }

        audioSource?.PlayOneShot(tickSound);

        for (int remaining = 5; remaining >= 1; remaining--)
        {
            if (chronoText != null)
            {
                chronoText.text = remaining.ToString();
            }

            yield return new WaitForSeconds(1f);
        }

        if (chronoText != null)
        {
            chronoText.text = "GO !!!";
        }

        yield return new WaitForSeconds(0.5f);

        arenaStartCoroutine = null;
        BeginGameFromLobby();
    }


    private void PlayGameMusic()
    {
        if (audioSource != null && gameMusic != null)
        {
            audioSource.clip = gameMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void SetPlayerPosition(GameObject player, Vector2 position)
    {
        if (player == null)
        {
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
            return;
        }

        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
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

    public void CheckVictoryConditions()
    {
        if (!partieEnCours || enIntermission) return;

        if (players.Count == 0) return;

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

        if (survivants == 0 && zombies > 0)
        {
            TriggerGameOver(true);
        }
        else if (zombies == 0 && survivants > 0)
        {
            TriggerGameOver(false);
        }
    }

    public void CheckZombiesWin()
    {
        CheckVictoryConditions();
    }

    public void TriggerGameOver(bool zombiesWon)
    {
        partieEnCours = false;
        canMove = false;
        tempsRestant = 0;

        if (audioSource != null) audioSource.Stop();

        string winningTeamMsg = zombiesWon ? "Les Infectés ont gagné !" : "Les Survivants ont gagné !";
        string teamId = zombiesWon ? "Infectés" : "Survivants";

        if (audioSource != null) 
        {
            audioSource.PlayOneShot(zombiesWon ? zombiesWinSound : survivorsWinSound);
        }

        if (chronoText != null) 
        {
            chronoText.text = zombiesWon ? "VICTOIRE DES INFECTÉS !" : "VICTOIRE DES SURVIVANTS !";
        }

        List<string> winnerPseudos = new List<string>();
        foreach (var kvp in players) 
        {
            if (kvp.Value != null) 
            {
                bool isZombie = kvp.Value.CompareTag("Enemy");
                if (zombiesWon == isZombie) 
                {
                    var tmpro = kvp.Value.GetComponentInChildren<TextMeshPro>();
                    if (tmpro != null) winnerPseudos.Add(tmpro.text);
                }
            }
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverTeamText != null) gameOverTeamText.text = winningTeamMsg;
        if (winnerListUI != null)
        {
            winnerListUI.PopulateWinners(winnerPseudos);
        }

        if (socket != null) 
        {
            socket.Emit("gameOver", new { 
                message = winningTeamMsg, 
                winningTeam = teamId, 
                winners = winnerPseudos.ToArray() 
            });
        }
    }

    /// <summary>
    /// Called by the Play Again button on the GameOver panel.
    /// Resets the full Unity game state and tells the server to go back to lobby.
    /// </summary>
    public void PlayAgain()
    {
        Debug.Log("PlayAgain: Resetting game state for a new round.");

        // 1. Stop any running coroutines (intermission, countdown)
        StopAllCoroutines();
        arenaStartCoroutine = null;

        // 2. Destroy all player GameObjects
        foreach (var kvp in players)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        players.Clear();
        playerInputs.Clear();
        dashEndTimes.Clear();
        dashCooldownEndTimes.Clear();

        // 3. Reset game state variables
        partieEnCours = false;
        enIntermission = false;
        canMove = false;
        mancheCourante = 1;
        tempsRestant = 90f;
        countdownSoundPlayed = false;

        // 4. Reset UI
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (chronoText != null) chronoText.text = "EN ATTENTE";
        if (compteurText != null) compteurText.text = "Lobby";
        if (audioSource != null) audioSource.Stop();

        // 5. Tell the server to go back to lobby
        if (socket != null)
        {
            socket.Emit("restart_game");
            socket.Emit("request_rejoin_all");
        }

        Debug.Log("PlayAgain: Reset complete. Waiting for players in lobby.");
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
                float randomY = UnityEngine.Random.Range(-9f, 4f);

                if (p.CompareTag("Enemy"))
                {
                    SetPlayerPosition(p, new Vector2(0f, randomY));
                }
                else if (col != null && !col.isSafe)
                {
                    p.tag = "Enemy";
                    PlayerSpriteController sprCtrl = p.GetComponent<PlayerSpriteController>();
                    if (sprCtrl != null) sprCtrl.SetState(PlayerState.Infected);
                    SetPlayerPosition(p, new Vector2(0f, randomY));
                    if (socket != null) socket.Emit("playerInfected", new { id = pId });
                }
                else if (col != null && col.isSafe)
                {
                    col.isSafe = false;
                    PlayerSpriteController sprCtrl = p.GetComponent<PlayerSpriteController>();
                    if (sprCtrl != null) sprCtrl.SetState(PlayerState.Survivor);
                    Color c = p.GetComponent<SpriteRenderer>().color;
                    c.a = 1f;
                    p.GetComponent<SpriteRenderer>().color = c;
                    SetPlayerPosition(p, new Vector2(-16f, randomY));

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

        // Arrêt de la musique de jeu et lancement de l'intermission
        if (audioSource != null && intermissionMusic != null){
            audioSource.Stop();
            audioSource.clip = intermissionMusic;
            audioSource.loop = false;
            audioSource.Play();
        }

        // Compte à rebours pour la manche 2
        if (chronoText != null) chronoText.text = "MANCHE 2 DANS...";
        yield return new WaitForSeconds(2f);

        if (chronoText != null) chronoText.text = "10...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "9...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "8...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "7...";
        yield return new WaitForSeconds(1f);

        if (chronoText != null) chronoText.text = "6...";
        yield return new WaitForSeconds(1f);

        audioSource?.PlayOneShot(tickSound);

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

        // Relance la musique pour la manche 2
        if (audioSource != null && gameMusic != null){
            audioSource.clip = gameMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        tempsRestant = 90f;
        enIntermission = false;
    }
}
