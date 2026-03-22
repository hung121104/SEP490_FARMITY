using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Presenter (controller) for the Outfit Skin Picker UI.
///
/// Attach this MonoBehaviour to the same GameObject as (or a parent of)
/// <see cref="SkinPickerPanelView"/>.
///
/// How it works
/// ------------
///   1. Call <see cref="Open"/> from a button, hotkey handler, or menu.
///   2. The presenter waits for <see cref="SkinCatalogManager"/> to be ready,
///      then finds the local player's <see cref="PlayerAppearanceSync"/>.
///   3. It populates the view with all outfit skins from the catalog.
///   4. When the player clicks a card the outfit is instantly applied via
///      <see cref="PlayerAppearanceSync.SetOutfit"/>,
///      which syncs to all clients through Photon Custom Properties.
///
/// Inspector Setup
/// ---------------
///   view — drag the SkinPickerPanelView component into this field.
///          If left empty, the component is searched on the same GameObject.
/// </summary>
public class SkinPickerPresenter : MonoBehaviour
{
    [SerializeField] private SkinPickerPanelView view;

    // ── State ─────────────────────────────────────────────────────────────────

    private PlayerAppearanceSync _appearanceSync;
    private string               _currentOutfit;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (view == null)
            view = GetComponent<SkinPickerPanelView>();
    }

    private void Start()
    {
        if (view == null)
        {
            Debug.LogWarning("[SkinPickerPresenter] SkinPickerPanelView not found. Panel will not work.");
            return;
        }

        view.OnCardSelected += HandleCardSelected;
        view.OnClosed       += HandleClosed;
        Debug.Log("[SkinPickerPresenter] Subscribed to view events.");
    }

    private void OnDestroy()
    {
        if (view != null)
        {
            view.OnCardSelected -= HandleCardSelected;
            view.OnClosed       -= HandleClosed;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Opens the skin picker panel.</summary>
    public void Open()
    {
        if (view == null) return;
        view.SetVisible(true);
        StartCoroutine(OpenRoutine());
    }

    /// <summary>Closes the skin picker panel.</summary>
    public void Close()
    {
        if (view != null) view.SetVisible(false);
        _appearanceSync = null;
    }

    /// <summary>Toggles the panel open/closed.</summary>
    public void ToggleOpen()
    {
        if (view != null && view.IsVisible) Close();
        else Open();
    }

    // ── Private Flow ──────────────────────────────────────────────────────────

    private IEnumerator OpenRoutine()
    {
        // 1 — Wait for SkinCatalogManager to finish loading all sheets.
        if (SkinCatalogManager.Instance == null || !SkinCatalogManager.Instance.IsReady)
        {
            Debug.Log("[SkinPickerPresenter] Waiting for SkinCatalogManager...");
            yield return new WaitUntil(() =>
                SkinCatalogManager.Instance != null && SkinCatalogManager.Instance.IsReady);
        }

        // 2 — Find the local player's appearance sync with retry (accounts for late spawn).
        float elapsed = 0f;
        while (_appearanceSync == null && elapsed < 10f)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                var pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    _appearanceSync = go.GetComponent<PlayerAppearanceSync>();
                    if (_appearanceSync != null)
                    {
                        Debug.Log("[SkinPickerPresenter] Bound to local PlayerAppearanceSync.");
                        break;
                    }
                }
            }

            if (_appearanceSync == null)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        if (_appearanceSync == null)
        {
            Debug.LogWarning("[SkinPickerPresenter] Could not find local PlayerAppearanceSync after 10 s.");
        }
        else
        {
            // Seed current selection from the player's live properties.
            var (_, outfit, _, _) = _appearanceSync.GetCurrentAppearance();
            _currentOutfit = outfit;
            view.UpdateEquippedLabel(_currentOutfit);
        }

        // Populate outfit cards.
        PopulateOutfits();
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandleCardSelected(string configId)
    {
        if (_appearanceSync == null)
        {
            Debug.LogWarning("[SkinPickerPresenter] No appearance sync – cannot equip skin.");
            return;
        }

        _currentOutfit = configId;
        _appearanceSync.SetOutfit(configId);

        view.UpdateEquippedLabel(_currentOutfit);
        view.UpdateSelectedCard(configId);

        Debug.Log($"[SkinPickerPresenter] Equipped outfit: '{configId}'");
    }

    private void HandleClosed() => Close();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PopulateOutfits()
    {
        var allEntries = SkinCatalogManager.Instance?.GetAllEntries();
        if (allEntries == null || allEntries.Count == 0)
        {
            view.PopulateCards(new List<SkinCatalogManager.SkinEntry>(), string.Empty);
            return;
        }

        var outfits = allEntries.Where(e => e.category == SkinCategory.Outfit).ToList();
        view.PopulateCards(outfits, _currentOutfit ?? string.Empty);
    }
}
