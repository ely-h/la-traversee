using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the scrollable list of winning players in the GameOverPanel.
/// Creates text entries dynamically — no prefab needed.
/// </summary>
public class WinnerListUI : MonoBehaviour
{
    [Tooltip("The Content transform inside the ScrollView (has VerticalLayoutGroup)")]
    [SerializeField] private Transform contentParent;

    [Header("Entry Style")]
    [SerializeField] private float entryHeight = 45f;
    [SerializeField] private float fontSize = 30f;

    /// <summary>
    /// Clears any existing entries and populates the scroll list with winner names.
    /// </summary>
    public void PopulateWinners(List<string> winnerPseudos)
    {
        // Auto-find contentParent if reference is missing
        if (contentParent == null)
        {
            Debug.LogWarning("WinnerListUI: contentParent was null! Attempting auto-find...");
            Transform scrollView = transform.Find("WinnersScrollView");
            if (scrollView != null)
            {
                Transform viewport = scrollView.Find("Viewport");
                if (viewport != null)
                {
                    contentParent = viewport.Find("Content");
                }
            }

            if (contentParent == null)
            {
                Debug.LogError("WinnerListUI: Could NOT find Content! Check hierarchy.");
                return;
            }
        }

        // Fix Mask alpha bug: Mask requires non-zero alpha on its Image to write
        // to the stencil buffer. The Viewport Image was created with alpha=0 which
        // causes the Mask to hide everything. Setting alpha=1 fixes it
        // (showMaskGraphic=false keeps it visually invisible).
        FixViewportMaskAlpha();

        ClearEntries();

        if (winnerPseudos == null)
        {
            Debug.LogError("WinnerListUI: winnerPseudos list is null!");
            return;
        }

        Debug.Log($"WinnerListUI: Populating {winnerPseudos.Count} winner(s).");

        foreach (string pseudo in winnerPseudos)
        {
            GameObject entry = new GameObject("WinnerEntry_" + pseudo, typeof(RectTransform), typeof(TextMeshProUGUI));
            entry.transform.SetParent(contentParent, false);
            entry.transform.localScale = Vector3.one;

            RectTransform rt = entry.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, entryHeight);

            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = entryHeight;
            le.flexibleWidth = 1;

            TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();
            tmp.text = pseudo;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
        }

        // Force layout rebuild
        if (contentParent is RectTransform contentRT)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        }

        // === DIAGNOSTIC: Log full hierarchy dimensions ===
        DiagnoseScrollView();
    }

    private void DiagnoseScrollView()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== WinnerListUI DIAGNOSTIC ===");

        // Walk up: Content -> Viewport -> ScrollView -> GameOverPanel
        Transform current = contentParent;
        while (current != null && current != transform.parent)
        {
            RectTransform rt = current.GetComponent<RectTransform>();
            if (rt != null)
            {
                sb.AppendLine($"  {current.name}: rect={rt.rect} sizeDelta={rt.sizeDelta} active={current.gameObject.activeInHierarchy}");
            }
            current = current.parent;
        }

        // Check mask
        Transform scrollView = transform.Find("WinnersScrollView");
        if (scrollView != null)
        {
            Transform viewport = scrollView.Find("Viewport");
            if (viewport != null)
            {
                var mask = viewport.GetComponent<Mask>();
                var img = viewport.GetComponent<Image>();
                sb.AppendLine($"  Viewport Mask: {(mask != null ? "exists, showGraphic=" + mask.showMaskGraphic : "MISSING")}");
                sb.AppendLine($"  Viewport Image: {(img != null ? "color=" + img.color : "MISSING")}");
            }
        }

        // Check content children
        sb.AppendLine($"  Content has {contentParent.childCount} children");
        for (int i = 0; i < Mathf.Min(contentParent.childCount, 3); i++)
        {
            var child = contentParent.GetChild(i);
            var crt = child.GetComponent<RectTransform>();
            sb.AppendLine($"    [{i}] {child.name}: rect={crt.rect} localPos={crt.localPosition}");
        }

        Debug.Log(sb.ToString());
    }

    public void ClearEntries()
    {
        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void FixViewportMaskAlpha()
    {
        // contentParent is Content, its parent is Viewport
        if (contentParent == null || contentParent.parent == null) return;

        Transform viewport = contentParent.parent;
        Image vpImage = viewport.GetComponent<Image>();
        if (vpImage != null && vpImage.color.a < 0.01f)
        {
            Color c = vpImage.color;
            c.a = 1f;
            vpImage.color = c;
            Debug.Log("WinnerListUI: Fixed Viewport Image alpha (was 0, now 1).");
        }
    }
}
