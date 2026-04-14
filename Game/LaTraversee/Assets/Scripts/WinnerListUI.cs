using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the scrollable list of winning players in the GameOverPanel.
/// Clears previous entries and instantiates a TextMeshProUGUI prefab for each winner.
/// </summary>
public class WinnerListUI : MonoBehaviour
{
    [Tooltip("The Content transform inside the ScrollView (has VerticalLayoutGroup)")]
    [SerializeField] private Transform contentParent;

    [Tooltip("Prefab with a TextMeshProUGUI used for each winner entry")]
    [SerializeField] private GameObject winnerEntryPrefab;

    /// <summary>
    /// Clears any existing entries and populates the scroll list with winner names.
    /// </summary>
    public void PopulateWinners(List<string> winnerPseudos)
    {
        // Clear previous entries to avoid duplicates on replay
        ClearEntries();

        if (winnerPseudos == null || winnerEntryPrefab == null || contentParent == null)
        {
            return;
        }

        foreach (string pseudo in winnerPseudos)
        {
            GameObject entry = Instantiate(winnerEntryPrefab, contentParent);
            TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = pseudo;
            }
        }
    }

    /// <summary>
    /// Destroys all child entries from the content container.
    /// </summary>
    public void ClearEntries()
    {
        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}
