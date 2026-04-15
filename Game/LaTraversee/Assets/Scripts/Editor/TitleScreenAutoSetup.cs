#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor utility to automatically construct the Title Screen UI hierarchy and link references.
/// Usage: In Unity, go to 'Tools > Create Title Screen'.
/// </summary>
public class TitleScreenAutoSetup : EditorWindow
{
    [MenuItem("Tools/Create Title Screen")]
    public static void CreateTitleScreen()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Error: No Canvas found in the current scene! Please create a Canvas first.");
            return;
        }

        LobbyUI lobby = Object.FindObjectOfType<LobbyUI>();
        if (lobby == null)
        {
            Debug.LogWarning("Warning: LobbyUI not found. You will need to assign it manually in the TitleScreenManager component.");
        }

        // --- 1. Create TitlePanel ---
        GameObject titlePanel = new GameObject("TitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titlePanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = titlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.SetAsLastSibling(); // Ensure it's on top of others

        Image panelImage = titlePanel.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f); // Dark background

        // --- 2. Create TitleText ---
        GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleTextObj.transform.SetParent(titlePanel.transform, false);
        
        TextMeshProUGUI titleText = titleTextObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "LA TRAVERSÉE";
        titleText.fontSize = 90;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        
        RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(1000, 200);

        // --- 3. Create PlayButton ---
        GameObject playBtnObj = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        playBtnObj.transform.SetParent(titlePanel.transform, false);
        
        RectTransform btnRect = playBtnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -100);
        btnRect.sizeDelta = new Vector2(300, 100);

        Image btnImage = playBtnObj.GetComponent<Image>();
        btnImage.color = new Color(0.12f, 0.45f, 0.85f); // Nice blue

        Button btn = playBtnObj.GetComponent<Button>();

        // --- 4. Create Button Text ---
        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(playBtnObj.transform, false);
        
        TextMeshProUGUI btnText = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnText.text = "PLAY";
        btnText.fontSize = 40;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        // --- 5. Setup TitleScreenManager ---
        TitleScreenManager manager = canvas.gameObject.GetComponent<TitleScreenManager>();
        if (manager == null) manager = canvas.gameObject.AddComponent<TitleScreenManager>();
        
        // We use SerializedObject to link private references from the editor script
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
        so.FindProperty("playButton").objectReferenceValue = btn;
        if (lobby != null) so.FindProperty("lobbyUI").objectReferenceValue = lobby;
        so.ApplyModifiedProperties();

        Debug.Log("Success: Title Screen created and references linked! Select the Canvas to see the setup.");
        Selection.activeGameObject = titlePanel;
    }
}
#endif
