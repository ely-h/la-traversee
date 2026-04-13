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
    [SerializeField] private TMP_Text playersListText;
    [SerializeField] private TMP_Text joinUrlText;
    [SerializeField] private Button startButton;
    [SerializeField] private LobbyQrCodeDisplay qrCodeDisplay;

    private bool listenersRegistered;

    private void Start()
    {
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

        if (listenersRegistered)
        {
            yield break;
        }

        listenersRegistered = true;

        networkManager.socket.On("lobby_state", (response) => {
            try
            {
                LobbyStateData data = response.GetValue<LobbyStateData>();
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
            catch
            {
                // The host already receives the canonical lobby_state event.
            }
        });

        networkManager.socket.On("game_started", (response) => {
            networkManager.EnqueueLobbyAction(HandleGameStarted);
        });

        if (statusText != null)
        {
            statusText.text = "En attente de joueurs...";
        }
    }

    private void ApplyLobbyState(LobbyStateData data)
    {
        if (data == null)
        {
            return;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(data.state == "lobby");
        }

        if (statusText != null)
        {
            int count = data.players != null ? data.players.Count : 0;
            statusText.text = data.state == "lobby"
                ? $"Lobby: {count} joueur(s) connecte(s)"
                : "Chargement de l'arene...";
        }

        if (playersListText != null)
        {
            if (data.players == null || data.players.Count == 0)
            {
                playersListText.text = "Aucun joueur connecte";
            }
            else
            {
                List<string> lines = new List<string>();
                foreach (LobbyPlayerData player in data.players)
                {
                    lines.Add($"- {player.pseudo}");
                }

                playersListText.text = string.Join("\n", lines);
            }
        }

        if (qrCodeDisplay != null)
        {
            string url = qrCodeDisplay.RefreshQrCode(data.port);
            if (joinUrlText != null)
            {
                joinUrlText.text = url;
            }
        }
    }

    public void LaunchParty()
    {
        if (networkManager == null || networkManager.socket == null)
        {
            return;
        }

        networkManager.socket.Emit("start_game");
        if (statusText != null)
        {
            statusText.text = "Lancement de la partie...";
        }
        Debug.Log("LobbyUI: start_game envoye.");
    }

    private void HandleGameStarted()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
        
        if (networkManager != null)
        {
            networkManager.StartArenaPhase();
        }
    }
}
