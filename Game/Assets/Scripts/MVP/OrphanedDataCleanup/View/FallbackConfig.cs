using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour holding shared fallback configuration.
/// Place on the same persistent GameObject as CatalogServices.
/// If no sprite is assigned, a 16x16 magenta square is generated at runtime.
/// </summary>
public class FallbackConfig : MonoBehaviour
{
    public static FallbackConfig Instance { get; private set; }

    [Header("Fallback Placeholder")]
    [Tooltip("Sprite used for all orphaned/deleted data visuals. If null, a magenta square is auto-generated.")]
    [SerializeField] private Sprite placeholderSprite;

    /// <summary>Returns the configured placeholder sprite, or auto-generates one if not assigned.</summary>
    public Sprite PlaceholderSprite => placeholderSprite != null
        ? placeholderSprite
        : FallbackDataFactory.CreatePlaceholderSprite();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }
}
