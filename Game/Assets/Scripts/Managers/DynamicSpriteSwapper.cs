using System;
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

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _ownRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Guard: both renderers and a valid config ID must be present
        if (_ownRenderer == null || masterRenderer == null) return;
        if (string.IsNullOrEmpty(configId)) return;
        if (!SkinCatalogManager.Instance?.IsReady ?? true) return;

        // 1 — Parse frame index from master sprite name
        Sprite masterSprite = masterRenderer.sprite;
        if (masterSprite == null) return;

        if (!TryParseFrameIndex(masterSprite.name, out int frameIndex))
        {
            // Master sprite name doesn't contain a trailing integer — skip silently.
            return;
        }

        // 2 — Avoid unnecessary work if nothing changed
        if (frameIndex == _lastAppliedIndex && configId == _cachedConfigId) return;

        // 3 — Refresh sprite array cache when configId changes
        if (configId != _cachedConfigId)
        {
            _cachedSprites  = SkinCatalogManager.Instance.GetSprites(configId);
            _cachedConfigId = configId;
            _lastAppliedIndex = -1; // force re-apply with new sheet
        }

        if (_cachedSprites == null || _cachedSprites.Length == 0)
        {
            // Sheet hasn't loaded yet or configId is unknown — hide layer
            _ownRenderer.sprite = null;
            return;
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
