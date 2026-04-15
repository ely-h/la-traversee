using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a dynamic scrollable player list inside the LobbyPanel.
/// Creates its own ScrollView hierarchy on first use if it doesn't already exist.
/// Same approach as WinnerListUI but for the lobby.
/// </summary>
public class LobbyPlayerListUI : MonoBehaviour
{
    [Tooltip("Optional: assign the Content transform manually. If null, auto-creates ScrollView.")]
    [SerializeField] private Transform contentParent;

    [Header("Entry Style")]
    [SerializeField] private float entryHeight = 40f;
    [SerializeField] private float fontSize = 26f;

    [Header("ScrollView Layout")]
    [SerializeField] private float scrollViewWidth = 600f;
    [SerializeField] private float scrollViewHeight = 400f;
    [SerializeField] private Vector2 scrollViewOffset = new Vector2(0f, -30f);

    private bool isInitialized;

    /// <summary>
    /// Refreshes the lobby player list with the given player names.
    /// </summary>
    public void RefreshPlayerList(List<string> playerNames)
    {
        EnsureInitialized();
        ClearEntries();

        if (playerNames == null || playerNames.Count == 0)
        {
            // Show a placeholder entry
            CreateEntry("En attente de joueurs...", new Color(1f, 1f, 1f, 0.5f));
            return;
        }

        foreach (string name in playerNames)
        {
            CreateEntry($"• {name}", Color.white);
        }

        // Force layout rebuild
        if (contentParent is RectTransform contentRT)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        }
    }

    private void CreateEntry(string text, Color color)
    {
        GameObject entry = new GameObject("LobbyEntry", typeof(RectTransform), typeof(TextMeshProUGUI));
        entry.transform.SetParent(contentParent, false);
        entry.transform.localScale = Vector3.one;

        RectTransform rt = entry.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, entryHeight);

        LayoutElement le = entry.AddComponent<LayoutElement>();
        le.preferredHeight = entryHeight;
        le.flexibleWidth = 1;

        TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(20f, 0f, 20f, 0f); // left/right padding
    }

    public void ClearEntries()
    {
        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized && contentParent != null) return;

        // Try to find existing ScrollView first
        Transform existingScrollView = transform.Find("LobbyPlayersScrollView");
        if (existingScrollView != null)
        {
            // Update its layout even if it exists
            UpdateScrollViewLayout(existingScrollView.GetComponent<RectTransform>());

            Transform viewport = existingScrollView.Find("Viewport");
            if (viewport != null)
            {
                contentParent = viewport.Find("Content");
                FixViewportMaskAlpha(viewport);
                
                // Ensure all components are present and linked
                SetupExistingScrollView(existingScrollView, viewport, contentParent);
            }
        }

        // If still null or missing, build it from scratch
        if (contentParent == null)
        {
            BuildScrollView();
        }

        isInitialized = true;
    }

    private void UpdateScrollViewLayout(RectTransform svRT)
    {
        if (svRT == null) return;
        svRT.anchorMin = new Vector2(0.5f, 0.5f);
        svRT.anchorMax = new Vector2(0.5f, 0.5f);
        svRT.sizeDelta = new Vector2(scrollViewWidth, scrollViewHeight);
        svRT.anchoredPosition = scrollViewOffset;
    }

    private void SetupExistingScrollView(Transform scrollView, Transform viewport, Transform content)
    {
        // 1. ScrollRect
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();

        // 2. Viewport Mask
        if (viewport.GetComponent<Mask>() == null) viewport.gameObject.AddComponent<Mask>();
        if (viewport.GetComponent<Image>() == null)
        {
            Image img = viewport.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 1);
        }

        // 3. Content Layout & Fitter
        if (content.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void BuildScrollView()
    {
        // === ScrollView root ===
        GameObject scrollViewGO = new GameObject("LobbyPlayersScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(CanvasRenderer), typeof(Image));
        scrollViewGO.transform.SetParent(transform, false);

        RectTransform svRT = scrollViewGO.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0.5f, 0.5f);
        svRT.anchorMax = new Vector2(0.5f, 0.5f);
        svRT.sizeDelta = new Vector2(scrollViewWidth, scrollViewHeight);
        svRT.anchoredPosition = scrollViewOffset;

        Image svBg = scrollViewGO.GetComponent<Image>();
        svBg.color = new Color(0f, 0f, 0f, 0.25f);
        svBg.raycastTarget = true;

        ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        // === Viewport ===
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollViewGO.transform, false);

        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(5f, 5f);
        vpRT.offsetMax = new Vector2(-5f, -5f);
        vpRT.pivot = new Vector2(0f, 1f);

        Image vpImage = viewportGO.GetComponent<Image>();
        // IMPORTANT: alpha MUST be > 0 for Mask stencil to work!
        vpImage.color = new Color(1f, 1f, 1f, 1f);
        vpImage.raycastTarget = false;

        Mask mask = viewportGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // === Content ===
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);

        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire ScrollRect references
        scrollRect.viewport = vpRT;
        scrollRect.content = contentRT;

        contentParent = contentRT;

        Debug.Log("LobbyPlayerListUI: ScrollView hierarchy built successfully.");
    }

    private void FixViewportMaskAlpha(Transform viewport)
    {
        if (viewport == null) return;
        Image vpImage = viewport.GetComponent<Image>();
        if (vpImage != null && vpImage.color.a < 0.01f)
        {
            Color c = vpImage.color;
            c.a = 1f;
            vpImage.color = c;
        }
    }
}
