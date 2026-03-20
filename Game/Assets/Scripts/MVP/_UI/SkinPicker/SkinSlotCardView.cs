using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single card in the skin-picker scroll list.
///
/// Inspector Setup
/// ---------------
///   previewImage     — Image that shows the first frame of the spritesheet.
///   nameText         — TextMeshProUGUI that displays a human-readable skin name.
///   selectedHighlight — Any GameObject used as a "selected" border/overlay.
///
/// The card requires a Button component on the same GameObject.
/// The Button's Normal/Highlighted/Pressed colours can be set freely in the Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class SkinSlotCardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image              previewImage;
    [SerializeField] private TextMeshProUGUI    nameText;
    [SerializeField] private GameObject         selectedHighlight;

    /// <summary>The configId this card represents (e.g. "blonde_hair").</summary>
    public string ConfigId { get; private set; }

    private Button _button;

    /// <summary>Fired when the player clicks this card, passing the configId.</summary>
    public event Action<string> OnSelected;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => OnSelected?.Invoke(ConfigId));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the card's visuals.
    /// </summary>
    /// <param name="configId">The skin config identifier.</param>
    /// <param name="preview">First frame sprite used as the card thumbnail. May be null.</param>
    /// <param name="isSelected">Whether to show the selected highlight.</param>
    public void Setup(string configId, Sprite preview, bool isSelected)
    {
        ConfigId = configId;

        if (previewImage != null)
        {
            previewImage.sprite  = preview;
            previewImage.enabled = preview != null;
        }

        if (nameText != null)
            nameText.text = FormatConfigId(configId);

        SetSelected(isSelected);
    }

    /// <summary>Toggles the selected-highlight overlay.</summary>
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>"blonde_hair" → "Blonde Hair", empty/null → "Default"</summary>
    private static string FormatConfigId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Default";
        return Regex.Replace(id.Replace('_', ' '), @"\b(\w)", m => m.Value.ToUpper());
    }
}
