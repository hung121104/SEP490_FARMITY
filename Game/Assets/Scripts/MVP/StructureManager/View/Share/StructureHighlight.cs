using UnityEngine;

/// <summary>
/// Singleton that manages a single Highlight GameObject.
/// Usage: Place the Prefab at Resources/StructureHighlight (auto-instantiated),
/// OR drag it into any scene's manager as a pre-placed instance.
/// Supports DontDestroyOnLoad so it persists across scene loads.
/// </summary>
public class StructureHighlight : MonoBehaviour
{
    // ── Singleton ───────────────────────────────────────────────────────

    private static StructureHighlight _instance;

    [Header("Auto-Load Configuration")]
    [Tooltip("The path inside 'Resources' folder where the prefab is located.")]
    [SerializeField] private string prefabResourcePath = "StructureHighlight";

    public static StructureHighlight Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StructureHighlight>();

                if (_instance == null)
                {
                    // If we don't have an instance, we use a default hardcoded path or look for a fallback
                    string path = "StructureHighlight";
                    GameObject prefab = Resources.Load<GameObject>(path);
                    
                    if (prefab != null)
                    {
                        GameObject obj = Instantiate(prefab);
                        obj.name = "StructureHighlight [Auto]";
                        _instance = obj.GetComponent<StructureHighlight>();
                        // Sync the path variable
                        _instance.prefabResourcePath = path;
                        DontDestroyOnLoad(obj);

                        if (_instance.showDebugLogs)
                            Debug.Log($"[StructureHighlight] Auto-instantiated from Resources/{path}");
                    }
                    else
                    {
                        Debug.LogWarning($"[StructureHighlight] Prefab not found at Resources/{path}. Check your folder structure!");
                    }
                }
            }
            return _instance;
        }
    }

    // ── Settings ────────────────────────────────────────────────────────

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("Settings")]
    [Tooltip("Default offset applied on top of the custom offset passed by each structure.")]
    public Vector3 defaultOffset = new Vector3(0, 1.5f, 0);

    [Header("Visual Object")]
    [Tooltip("The visual child GameObject (sprite, particle, etc.) to show/hide. " +
             "If left empty, the entire StructureHighlight GameObject will be toggled instead.")]
    [SerializeField] private GameObject visualRoot;

    private Transform _currentTarget;

    // ── Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton guard
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback: if no visualRoot assigned, use the entire GameObject
        if (visualRoot == null) visualRoot = gameObject;

        visualRoot.SetActive(false);
    }

    private void Update()
    {
        // Smoothly follow the current target
        if (_currentTarget != null && visualRoot.activeSelf)
        {
            transform.position = _currentTarget.position + defaultOffset;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Show the highlight on a target. The final position = target.position + customOffset.
    /// </summary>
    public void Show(Transform target, Vector3 customOffset)
    {
        _currentTarget = target;
        transform.position = target.position + customOffset;
        visualRoot.SetActive(true);

        if (showDebugLogs) Debug.Log($"[StructureHighlight] Showing on {target.name}");
    }

    /// <summary>
    /// Hide the highlight. Guard: only hides if the requesting target is still the current one.
    /// This prevents a table from accidentally hiding the highlight that another table just claimed.
    /// </summary>
    public void Hide(Transform requestingTarget)
    {
        if (_currentTarget == requestingTarget)
        {
            visualRoot.SetActive(false);
            _currentTarget = null;
            
            if (showDebugLogs) Debug.Log($"[StructureHighlight] Hiding from {requestingTarget.name}");
        }
    }
}
