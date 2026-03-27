using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to any layered SpriteRenderer in the Paper Doll hierarchy
/// (e.g. Hair Layer, Tool Layer, Hat Layer, Outfit Layer).
///
/// Every LateUpdate it:
///   1. Reads the current sprite name from the master (Body) renderer.
///   2. Parses the trailing integer index from the name (e.g. "Base_Plow_5" → 5).
///   3. Looks up its own configId's Sprite[] from SkinCatalogManager.
///   4. Applies the sprite at that index to its own SpriteRenderer —
///      keeping the layer perfectly in sync with the master animation.
///
/// Inspector Setup
/// ---------------
///   masterRenderer  — drag the SpriteRenderer that the Animator drives (Body).
///   ownRenderer     — the SpriteRenderer on THIS GameObject (auto-filled in Awake).
///   configId        — e.g. "blonde_hair", "gold_hoe". Set by EquipmentManager.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSpriteSwapper : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────────

    [Tooltip("The SpriteRenderer that the Animator drives (usually the Body layer).")]
    [SerializeField] private SpriteRenderer masterRenderer;

    [Tooltip("Config ID of the spritesheet this layer uses (e.g. 'gold_hoe').")]
    [SerializeField] private string configId;

    // ── Private State ─────────────────────────────────────────────────────────

    private SpriteRenderer _ownRenderer;

    /// Cached index of the last sprite we applied — avoids redundant lookups.
    private int _lastAppliedIndex = -1;

    /// Cached configId for which we last fetched sprites from the catalog.
    private string _cachedConfigId;
    private Sprite[] _cachedSprites;

    /// configIds that returned no sprites from the catalog — logged once each.
    private readonly HashSet<string> _warnedMissing = new HashSet<string>();

    /// The last frame index this swapper successfully applied.
    /// Other layer swappers on the same character read this as a fallback
    /// when they can't parse the master sprite name themselves.
    public int CurrentFrameIndex { get; private set; } = -1;

    // Reference to the DynamicSpriteSwapper on the masterRenderer's GameObject
    // (i.e. the body layer), cached at Start to avoid per-frame GetComponent.
    private DynamicSpriteSwapper _masterBodySwapper;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// The config ID of the spritesheet assigned to this layer.
    /// Setting this clears the sprite cache so the next LateUpdate
    /// picks up the new sheet immediately.
    /// </summary>
    public string ConfigId
    {
        get => configId;
        set
        {
            if (configId == value) return;
            configId = value;
            InvalidateCache();
        }
    }

    public void SetConfigId(string newConfigId)
    {
        ConfigId = newConfigId;
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _ownRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Cache the body layer swapper so sibling layers can read its frame index.
        // Skip self-reference (body layer's masterRenderer == its own renderer).
        if (masterRenderer != null)
        {
            var candidate = masterRenderer.GetComponent<DynamicSpriteSwapper>();
            if (candidate != null && candidate != this)
                _masterBodySwapper = candidate;
        }
    }

    private void LateUpdate()
    {
        // Guard: both renderers must be present
        if (_ownRenderer == null || masterRenderer == null) return;

        // Explicit clear when nothing is equipped (empty configId = intentional unequip)
        if (string.IsNullOrEmpty(configId))
        {
            if (_ownRenderer.sprite != null)
            {
                _ownRenderer.sprite = null;
                _lastAppliedIndex   = -1;
            }
            return;
        }

        if (!SkinCatalogManager.Instance?.IsReady ?? true) return;

        // 1 — Parse frame index from master sprite name
        Sprite masterSprite = masterRenderer.sprite;
        if (masterSprite == null) return;

        if (!TryParseFrameIndex(masterSprite.name, out int frameIndex))
        {
            // Sprite name has no trailing integer — fall back to the body layer's
            // last known frame index (written by its own swapper in LateUpdate).
            if (_masterBodySwapper != null && _masterBodySwapper.CurrentFrameIndex >= 0)
                frameIndex = _masterBodySwapper.CurrentFrameIndex;
            else
                return; // body frame not known yet — wait
        }

        // 2 — Avoid unnecessary work if nothing changed.
        // EXCEPTION: when masterRenderer == ownRenderer (body layer), the Animator
        // overwrites the sprite every Update, so we MUST re-apply the catalog sprite
        // each LateUpdate regardless of whether the frame index is the same.
        bool sameRenderer = ReferenceEquals(masterRenderer, _ownRenderer);
        if (!sameRenderer && frameIndex == _lastAppliedIndex && configId == _cachedConfigId) return;

        // 3 — Refresh sprite array cache when configId changes
        if (configId != _cachedConfigId)
        {
            _cachedSprites    = SkinCatalogManager.Instance.GetSprites(configId);
            _lastAppliedIndex = -1;

            if (_cachedSprites == null || _cachedSprites.Length == 0)
            {
                // configId is not in the server skin catalog.
                // Do NOT null the renderer — the body layer stays visible via the Animator
                // and overlay layers keep their last valid sprite.
                // Do NOT commit _cachedConfigId so we retry when configId changes.
                if (_warnedMissing.Add(configId))
                    Debug.LogWarning(
                        $"[DynamicSpriteSwapper] '{configId}' not found in runtime sprite catalogs " +
                        $"on '{gameObject.name}'. " +
                        "Add a matching entry in Combat Catalog or Skin Configs with a valid spritesheet URL.");
                return;
            }

            _cachedConfigId = configId; // commit only when real sprites exist
        }

        // 4 — Clamp so we never go out of bounds even if sheet sizes differ
        int clampedIndex = Mathf.Clamp(frameIndex, 0, _cachedSprites.Length - 1);

        if (clampedIndex != frameIndex)
        {
            Debug.LogWarning(
                $"[DynamicSpriteSwapper] '{configId}' has {_cachedSprites.Length} frames " +
                $"but master requested index {frameIndex}. Clamping to {clampedIndex}.");
        }

        _ownRenderer.sprite   = _cachedSprites[clampedIndex];
        _lastAppliedIndex     = frameIndex;
        CurrentFrameIndex     = frameIndex;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Parses the trailing integer from a sprite name.
    ///
    /// Supported formats:
    ///   "Base_Plow_5"   → 5
    ///   "Farmer_Idle_12"→ 12
    ///   "sheet_0"       → 0
    ///
    /// Returns false if no trailing integer is found (e.g. "StaticIcon").
    /// This is robust against sprite names that contain digits mid-string.
    /// </summary>
    private static bool TryParseFrameIndex(string spriteName, out int index)
    {
        index = 0;
        if (string.IsNullOrEmpty(spriteName)) return false;

        // Walk backwards from the end of the name; collect digit characters
        int end = spriteName.Length - 1;

        // Skip nothing — the name itself might end with a digit directly
        int digitEnd = end;

        while (digitEnd >= 0 && char.IsDigit(spriteName[digitEnd]))
            digitEnd--;

        // digitEnd now points to the last non-digit character.
        // The digits span [digitEnd+1 .. end].
        int digitStart = digitEnd + 1;
        int digitLength = end - digitStart + 1;

        if (digitLength == 0)
        {
            // No trailing digits
            return false;
        }

        // Guard against absurdly long digit strings (prevents int overflow)
        if (digitLength > 9)
        {
            Debug.LogWarning(
                $"[DynamicSpriteSwapper] Sprite name '{spriteName}' has a suspiciously " +
                "long trailing integer — skipping.");
            return false;
        }

        // The separator before the digits (if any) must be a non-digit,
        // which means the structure is correct.  Parse directly.
#if UNITY_2021_1_OR_NEWER
        return int.TryParse(spriteName.AsSpan(digitStart, digitLength), out index);
#else
        return int.TryParse(spriteName.Substring(digitStart, digitLength), out index);
#endif
    }

    private void InvalidateCache()
    {
        _cachedConfigId   = null;
        _cachedSprites    = null;
        _lastAppliedIndex = -1;
        _warnedMissing.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Prevent a null reference when ownRenderer is not yet set in editor
        if (_ownRenderer == null)
            _ownRenderer = GetComponent<SpriteRenderer>();
    }
#endif
}
