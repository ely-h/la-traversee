using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LobbyPlayerData
{
    public string id { get; set; }
    public string pseudo { get; set; }
    public string color { get; set; }
}

[Serializable]
public class LobbyStateData
{
    public string state { get; set; }
    public List<LobbyPlayerData> players { get; set; }
    public int port { get; set; }
}

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private LobbyPlayerListUI lobbyPlayerListUI;
    [SerializeField] private TMP_Text joinUrlText;
    [SerializeField] private Button startButton;
    [SerializeField] private LobbyQrCodeDisplay qrCodeDisplay;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lobbyMusic;

    private bool listenersRegistered;
    private bool canShowLobby = false; // Prevents auto-show until Title Screen is dismissed
    private LobbyStateData lastLobbyState; // Caches state

    private void Start()
    {
        Debug.Log("LobbyUI: Initializing...");
        if (startButton != null)
        {
            startButton.onClick.AddListener(LaunchParty);
        }

        StartCoroutine(InitializeSocketBindings());
    }

    private IEnumerator InitializeSocketBindings()
    {
        while (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            yield return null;
        }

        while (networkManager.socket == null)
        {
            yield return null;
        }

        if (listenersRegistered) yield break;
        listenersRegistered = true;

        networkManager.socket.On("lobby_state", (response) => {
            try
            {
                LobbyStateData data = response.GetValue<LobbyStateData>();
                lastLobbyState = data;
                networkManager.EnqueueLobbyAction(() => ApplyLobbyState(data));
            }
            catch (Exception ex)
            {
                Debug.LogError("LobbyUI: erreur lobby_state " + ex.Message);
            }
        });

        networkManager.socket.On("player_joined", (response) => {
            try
            {
                LobbyStateData data = response.GetValue<LobbyStateData>();
                networkManager.EnqueueLobbyAction(() => ApplyLobbyState(data));
            }
            catch {}
        });

        networkManager.socket.On("game_started", (response) => {
            networkManager.EnqueueLobbyAction(HandleGameStarted);
        });

        networkManager.socket.On("game_restarted", (response) => {
            networkManager.EnqueueLobbyAction(HandleGameRestarted);
        });

        if (statusText != null) statusText.text = "En attente de joueurs...";
        Debug.Log("LobbyUI: Socket bindings initialized.");
    }

    private void ApplyLobbyState(LobbyStateData data)
    {
        if (data == null) return;

        bool isLobbyState = (data.state == "lobby");
        
        if (lobbyPanel != null)
        {
            // Only show if Title Screen is dismissed AND server is in lobby mode
            bool shouldBeActive = canShowLobby && isLobbyState;
            lobbyPanel.SetActive(shouldBeActive);
            Debug.Log($"LobbyUI: ApplyLobbyState - PanelActive={shouldBeActive} (canShow={canShowLobby}, state={data.state})");
        }

        // ... Existing UI Refresh ...
        if (statusText != null)
        {
            int count = data.players != null ? data.players.Count : 0;
            statusText.text = isLobbyState ? $"Lobby: {count} joueur(s) connecte(s)" : "Chargement...";
        }

        if (lobbyPlayerListUI != null && data.players != null)
        {
            List<string> names = new List<string>();
            foreach (var p in data.players) names.Add(p.pseudo);
            lobbyPlayerListUI.RefreshPlayerList(names);
        }

        if (qrCodeDisplay != null)
        {
            string url = qrCodeDisplay.RefreshQrCode(data.port);
            if (joinUrlText != null) joinUrlText.text = url;
        }
    }

    public void LaunchParty()
    {
        if (networkManager?.socket == null) return;
        networkManager.socket.Emit("start_game");
        if (statusText != null) statusText.text = "Lancement...";
    }

    private void HandleGameStarted()
    {
        Debug.Log("LobbyUI: Game Started received.");
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (audioSource != null) audioSource.Stop();
        networkManager?.StartArenaPhase();
    }

    private void HandleGameRestarted()
    {
        Debug.Log("LobbyUI: Game Restarted received.");
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (audioSource != null && lobbyMusic != null)
        {
            audioSource.clip = lobbyMusic;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Triggered by TitleScreenManager when 'PLAY' is clicked.
    /// </summary>
    public void SetLobbyReady()
    {
        Debug.Log("LobbyUI: SetLobbyReady() triggered from Title Screen.");
        canShowLobby = true;
        
        // Immediate show if we have a panel
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("LobbyUI: SetLobbyReady - Force-activating lobbyPanel.");
        }
        else
        {
            Debug.LogError("LobbyUI: SetLobbyReady - lobbyPanel reference is MISSING!");
        }

        // Music fallback
        if (audioSource != null && lobbyMusic != null && !audioSource.isPlaying)
        {
            audioSource.clip = lobbyMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Apply state if it arrived early
        if (lastLobbyState != null)
        {
            ApplyLobbyState(lastLobbyState);
        }
    }
}
