using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MapManager that can either instantiate map prefabs or load map scenes additively.
/// - Set `useSceneMode` to true to load maps by scene name (additive).
/// - Assign scene names in `mapSceneNames` (they must be in Build Settings) when using scene mode.
/// - When using prefab mode, assign prefabs in `mapPrefabs`.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Mode")]
    [Tooltip("If true, maps will be loaded as scenes (additive). If false, prefabs will be instantiated.")]
    [SerializeField]
    private bool useSceneMode = true;

    [Header("Map Prefabs (Prefab Mode)")]
    [Tooltip("List of map prefabs that can be loaded at runtime. Only used when Use Scene Mode is false.")]
    [SerializeField]
    private GameObject[] mapPrefabs = new GameObject[0];

    [Tooltip("Optional parent transform to keep spawned maps organized in the hierarchy (prefab mode only).")]
    [SerializeField]
    private Transform mapParent;

    [Header("Map Scenes (Scene Mode)")]
    [Tooltip("List of scene names that can be loaded. Scenes must be added to Build Settings.")]
    [SerializeField]
    private string[] mapSceneNames = new string[0];

    [Header("Startup")]
    [Tooltip("Automatically load a map on Start if true.")]
    [SerializeField]
    private bool loadOnStart = true;

    [Tooltip("Index of the map to load on Start (only used if Load On Start is true).")]
    [SerializeField]
    private int startMapIndex = 0;

    private GameObject currentMapInstance;
    private string currentMapSceneName;
    public int CurrentMapIndex { get; private set; } = -1;

    void Awake()
    {
        // simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (!loadOnStart)
            return;

        if (useSceneMode)
        {
            if (mapSceneNames != null && mapSceneNames.Length > 0)
            {
                var idx = Mathf.Clamp(startMapIndex, 0, mapSceneNames.Length - 1);
                LoadMap(idx);
            }
        }
        else
        {
            if (mapPrefabs != null && mapPrefabs.Length > 0)
            {
                var idx = Mathf.Clamp(startMapIndex, 0, mapPrefabs.Length - 1);
                LoadMap(idx);
            }
        }
    }

    /// <summary>
    /// Load a map by index. Behavior depends on mode (scene or prefab).
    /// </summary>
    public void LoadMap(int index)
    {
        if (useSceneMode)
        {
            if (mapSceneNames == null || mapSceneNames.Length == 0)
            {
                Debug.LogWarning("MapManager: no map scenes assigned.");
                return;
            }

            if (index < 0 || index >= mapSceneNames.Length)
            {
                Debug.LogWarning($"MapManager: invalid map index {index}.");
                return;
            }

            var sceneName = mapSceneNames[index];
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"MapManager: scene name at index {index} is empty.");
                return;
            }

            UnloadCurrentMap();

            // Load the scene additively and set it active once loaded
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogWarning($"MapManager: failed to start loading scene '{sceneName}'. Is it added to Build Settings?");
                return;
            }

            op.completed += (asyncOp) =>
            {
                var loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadedScene);
                    currentMapSceneName = sceneName;
                    CurrentMapIndex = index;
                }
                else
                {
                    Debug.LogWarning($"MapManager: loaded scene '{sceneName}' is not valid after load.");
                }
            };
        }
        else
        {
            if (mapPrefabs == null || mapPrefabs.Length == 0)
            {
                Debug.LogWarning("MapManager: no map prefabs assigned.");
                return;
            }

            if (index < 0 || index >= mapPrefabs.Length)
            {
                Debug.LogWarning($"MapManager: invalid map index {index}.");
                return;
            }

            UnloadCurrentMap();

            var prefab = mapPrefabs[index];
            if (prefab == null)
            {
                Debug.LogWarning($"MapManager: prefab at index {index} is null.");
                return;
            }

            currentMapInstance = Instantiate(prefab, mapParent);
            currentMapInstance.transform.localPosition = Vector3.zero;
            currentMapInstance.transform.localRotation = Quaternion.identity;
            CurrentMapIndex = index;
        }
    }

    /// <summary>
    /// Load a map by name. Will try scenes first if in scene mode, otherwise matches prefab names.
    /// </summary>
    public void LoadMap(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (useSceneMode)
        {
            if (mapSceneNames == null)
                return;

            for (int i = 0; i < mapSceneNames.Length; i++)
            {
                var sceneName = mapSceneNames[i];
                if (!string.IsNullOrEmpty(sceneName) && sceneName == name)
                {
                    LoadMap(i);
                    return;
                }
            }

            Debug.LogWarning($"MapManager: no map scene with name '{name}' found.");
        }
        else
        {
            if (mapPrefabs == null)
                return;

            for (int i = 0; i < mapPrefabs.Length; i++)
            {
                var prefab = mapPrefabs[i];
                if (prefab != null && prefab.name == name)
                {
                    LoadMap(i);
                    return;
                }
            }

            Debug.LogWarning($"MapManager: no map prefab with name '{name}' found.");
        }
    }

    /// <summary>
    /// Unload currently loaded map instance or scene if any.
    /// </summary>
    public void UnloadCurrentMap()
    {
        if (useSceneMode)
        {
            if (!string.IsNullOrEmpty(currentMapSceneName))
            {
                // Unload asynchronously
                var op = SceneManager.UnloadSceneAsync(currentMapSceneName);
                if (op == null)
                {
                    Debug.LogWarning($"MapManager: failed to unload scene '{currentMapSceneName}'.");
                }

                currentMapSceneName = null;
                CurrentMapIndex = -1;
            }
        }
        else
        {
            if (currentMapInstance != null)
            {
                Destroy(currentMapInstance);
                currentMapInstance = null;
                CurrentMapIndex = -1;
            }
        }
    }

    /// <summary>
    /// Reloads the currently selected map (if any).
    /// </summary>
    public void ReloadCurrentMap()
    {
        if (CurrentMapIndex >= 0)
            LoadMap(CurrentMapIndex);
    }

    /// <summary>
    /// Returns the prefab at the provided index or null (prefab mode only).
    /// </summary>
    public GameObject GetMapPrefab(int index)
    {
        if (mapPrefabs == null || index < 0 || index >= mapPrefabs.Length)
            return null;
        return mapPrefabs[index];
    }

    /// <summary>
    /// Returns the scene name at the provided index or null (scene mode only).
    /// </summary>
    public string GetMapSceneName(int index)
    {
        if (mapSceneNames == null || index < 0 || index >= mapSceneNames.Length)
            return null;
        return mapSceneNames[index];
    }

    /// <summary>
    /// Helper for editor or runtime to know whether a map is loaded.
    /// </summary>
    public bool HasMapLoaded() => (useSceneMode ? !string.IsNullOrEmpty(currentMapSceneName) : currentMapInstance != null);
}
