using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// View component for the Outfit Skin Picker panel.
///
/// Inspector Setup
/// ---------------
///   panelRoot          — Root GameObject to show/hide (can be this GO or a child).
///   cardContainer      — Drag the CONTENT Transform here (the child of Viewport inside
///                        the Scroll View). Do NOT drag the Scroll View itself or the
///                        Viewport — cards must be children of Content to scroll correctly.
///   cardPrefab         — The SkinSlotCardView prefab to instantiate per outfit option.
///   equippedOutfitText — (optional) Label showing the currently equipped outfit name.
///   closeButton        — Button that fires OnClosed.
/// </summary>
public class SkinPickerPanelView : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Skin Cards")]
    [Tooltip("Drag the CONTENT Transform here (Scroll View → Viewport → Content). Do NOT drag the Scroll View or Viewport.")]
    [SerializeField] private Transform        cardContainer;
    [SerializeField] private SkinSlotCardView cardPrefab;

    [Header("Currently Equipped Label (optional)")]
    [SerializeField] private TextMeshProUGUI equippedOutfitText;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fires when the player clicks a skin card, passing its configId.</summary>
    public event Action<string> OnCardSelected;

    /// <summary>Fires when the player clicks the close button.</summary>
    public event Action OnClosed;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private readonly List<SkinSlotCardView> _cards = new List<SkinSlotCardView>();

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        closeButton?.onClick.AddListener(() => OnClosed?.Invoke());
        SetVisible(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Shows or hides the entire panel.</summary>
    public void SetVisible(bool visible)
    {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }

    /// <summary>
    /// Clears existing cards and spawns new ones from <paramref name="entries"/>.
    /// </summary>
    /// <param name="entries">Filtered list of entries for the current tab.</param>
    /// <param name="currentConfigId">ConfigId to mark as selected on open.</param>
    public void PopulateCards(
        IReadOnlyList<SkinCatalogManager.SkinEntry> entries,
        string currentConfigId)
    {
        // Destroy old cards
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (cardPrefab == null || cardContainer == null) return;

        // ── Default card (removes the outfit / shows base body) ───────────────
        Sprite bodyPreview = FindBodyPreview();
        SkinSlotCardView defaultCard = Instantiate(cardPrefab, cardContainer);
        defaultCard.Setup(string.Empty, bodyPreview, string.IsNullOrEmpty(currentConfigId));
        defaultCard.OnSelected += id => OnCardSelected?.Invoke(id);
        _cards.Add(defaultCard);

        // ── Outfit cards ─────────────────────────────────────────────────────
        foreach (var entry in entries)
        {
            var sprites = SkinCatalogManager.Instance?.GetSprites(entry.configId);
            Sprite preview = sprites != null && sprites.Length > 0 ? sprites[0] : null;

            SkinSlotCardView card = Instantiate(cardPrefab, cardContainer);
            card.Setup(entry.configId, preview, entry.configId == currentConfigId);
            card.OnSelected += id => OnCardSelected?.Invoke(id);
            _cards.Add(card);
        }
    }

    /// <summary>
    /// Updates which card shows the selected-highlight without rebuilding the grid.
    /// </summary>
    public void UpdateSelectedCard(string configId)
    {
        foreach (var card in _cards)
            card.SetSelected(card.ConfigId == configId);
    }

    /// <summary>
    /// Refreshes the "currently equipped" outfit label.
    /// Pass empty string or null to display "None".
    /// </summary>
    public void UpdateEquippedLabel(string outfit)
    {
        if (equippedOutfitText != null) equippedOutfitText.text = FormatLabel(outfit);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches the catalog for any entry whose configId contains "body" or "base"
    /// and returns its first frame as the Default card thumbnail.
    /// Returns null if no matching entry is found.
    /// </summary>
    private static Sprite FindBodyPreview()
    {
        var entries = SkinCatalogManager.Instance?.GetAllEntries();
        if (entries == null) return null;

        foreach (var e in entries)
        {
            string lower = e.configId.ToLowerInvariant();
            if (lower.Contains("body") || lower.Contains("base"))
            {
                var sprites = SkinCatalogManager.Instance.GetSprites(e.configId);
                if (sprites != null && sprites.Length > 0) return sprites[0];
            }
        }
        return null;
    }

    /// <summary>"farmer_default" → "Farmer Default", null/empty → "None"</summary>
    private static string FormatLabel(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return "None";
        return Regex.Replace(configId.Replace('_', ' '), @"\b(\w)", m => m.Value.ToUpper());
    }
}
