using UnityEngine;

/// <summary>
/// Session-style manager that survives scene loads and holds the currently-selected world id.
/// Create it once (it will self-initialize if needed) and read `SelectedWorldId` from other scenes.
/// This manager intentionally does NOT persist to disk; it's for runtime session state only.
/// </summary>
public class WorldSelectionManager : MonoBehaviour
{
    public static WorldSelectionManager Instance { get; private set; }
    
    [Header("Runtime Session")]
    [SerializeField]
    [Tooltip("Runtime selected world id (session-only). Visible in Inspector during Play.")]
    private string selectedWorldId;

    [SerializeField]
    [Tooltip("Runtime selected world name (session-only). Visible in Inspector during Play.")]
    private string worldName;

    public string SelectedWorldId => selectedWorldId;
    public string WorldName => worldName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Ensure an instance exists in the scene. If none exists, one will be created.
    /// </summary>
    public static WorldSelectionManager EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("WorldSelectionManager");
        return go.AddComponent<WorldSelectionManager>();
    }

    public void SetSelectedWorldId(string id)
    {
        selectedWorldId = id;
    }

    public void SetWorldName(string name)
    {
        worldName = name;
    }

    public void SetSelectedWorld(string id, string name)
    {
        selectedWorldId = id;
        worldName = name;
    }

    public void ClearSelectedWorldId()
    {
        selectedWorldId = null;
        worldName = null;
    }
}
