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
    [SerializeField] private AudioSource titleAudioSource;
    [SerializeField] private AudioClip titleMusic;

    private void Start()
    {
        Debug.Log("TitleScreenManager: Start called.");

        // 1. Redundant search for LobbyUI if reference is missing
        if (lobbyUI == null)
        {
            lobbyUI = FindObjectOfType<LobbyUI>();
            if (lobbyUI != null)
            {
                Debug.Log("TitleScreenManager: Auto-located LobbyUI in scene.");
            }
            else
            {
                Debug.LogError("TitleScreenManager: Failed to find LobbyUI! Transition will not work.");
            }
        }

        // 2. Ensure Title matches
        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
        }

        if (titleAudioSource == null)
        {
            titleAudioSource = titlePanel != null
                ? titlePanel.GetComponent<AudioSource>()
                : GetComponent<AudioSource>();
        }

        if (titleAudioSource != null && titleMusic != null)
        {
            titleAudioSource.clip = titleMusic;
            titleAudioSource.loop = true;

            if (!titleAudioSource.isPlaying)
            {
                titleAudioSource.Play();
            }
        }

        // 3. Setup Button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        else
        {
            Debug.LogError("TitleScreenManager: Play Button reference is MISSING!");
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

        if (titleAudioSource != null && titleAudioSource.isPlaying)
        {
            titleAudioSource.Stop();
        }

        // Signal Lobby to start
        lobbyUI.SetLobbyReady();

        Debug.Log("TitleScreenManager: Signal sent to LobbyUI; Title Screen dismissed.");
    }

    // Used by Editor script to link refs
    public void Setup(GameObject panel, LobbyUI lobby, Button button)
    {
        titlePanel = panel;
        lobbyUI = lobby;
        playButton = button;
    }
}
