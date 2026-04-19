using System.Collections;
using System.Collections.Generic;
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
///   2. The presenter waits for the skin catalog to be ready via the service,
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
    private ISkinPickerService   _service;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (view == null)
            view = GetComponent<SkinPickerPanelView>();

        _service = new SkinPickerService();
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
        // 1 — Wait for catalog to finish loading via the service.
        if (!_service.IsCatalogReady)
        {
            Debug.Log("[SkinPickerPresenter] Waiting for skin catalog...");
            yield return new WaitUntil(() => _service.IsCatalogReady);
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
        var outfits = _service.GetOutfitEntries();

        // Build preview data for each entry so the View never touches the catalog.
        var previews = new List<SkinPickerPanelView.CardData>(outfits.Count);
        foreach (var entry in outfits)
        {
            Sprite preview = _service.GetPreviewSprite(entry.configId);
            previews.Add(new SkinPickerPanelView.CardData(entry.configId, preview));
        }

        Sprite bodyPreview = _service.GetBodyPreviewSprite();

        view.PopulateCards(previews, bodyPreview, _currentOutfit ?? string.Empty);
    }
}
