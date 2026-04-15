using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the transition from the Title Screen to the Lobby.
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private LobbyUI lobbyUI;

    [Header("Controls")]
    [SerializeField] private Button playButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip titleMusic;

    private void Start()
    {
        Debug.Log("TitleScreenManager: Start called.");

        // 1. Redundant search for LobbyUI if reference is missing
        if (lobbyUI == null)
        {
            lobbyUI = FindObjectOfType<LobbyUI>();
        }

        // 2. Play Music if assigned
        if (audioSource != null && titleMusic != null)
        {
            audioSource.clip = titleMusic;
            audioSource.loop = true;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        // 3. Ensure Title matches
        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
        }

        // 4. Setup Button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    public void OnPlayClicked()
    {
        Debug.Log("TitleScreenManager: Play button clicked!");

        if (lobbyUI == null)
        {
            Debug.LogError("TitleScreenManager: Cannot transition! LobbyUI is null.");
            return;
        }

        // Hide Title Panel
        if (titlePanel != null)
        {
            titlePanel.SetActive(false);
        }

        // Signal Lobby to start
        lobbyUI.SetLobbyReady();

        Debug.Log("TitleScreenManager: Signal sent to LobbyUI; Title Screen dismissed.");
    }

    // Used by Editor script to link refs
    public void Setup(GameObject panel, LobbyUI lobby, Button button, AudioSource audio, AudioClip music)
    {
        titlePanel = panel;
        lobbyUI = lobby;
        playButton = button;
        audioSource = audio;
        titleMusic = music;
    }
}
