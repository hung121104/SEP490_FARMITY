using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Main view component that manages the world list display
/// Attach this to a GameObject in your scene
/// </summary>
public class MyWorldListView : MonoBehaviour
{
    [Header("Prefab & Container")]
    [SerializeField] private GameObject worldItemPrefab;
    [SerializeField] private Transform worldListContainer;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;

    private MyWorldListPresenter presenter;
    private List<WorldItemView> worldItemInstances = new List<WorldItemView>();

    private void Awake()
    {
        // Hide world item prefab if it's active in the scene
        if (worldItemPrefab != null && worldItemPrefab.activeInHierarchy)
        {
            worldItemPrefab.SetActive(false);
        }

        // Initialize presenter with service
        IMyWorldListService service = new MyWorldListService();
        presenter = new MyWorldListPresenter(service);
        presenter.SetView(this);
    }

    private async void Start()
    {
        // Load worlds when scene starts
        await LoadWorlds();
    }

    private async System.Threading.Tasks.Task LoadWorlds()
    {
        ShowLoading(true);
        UpdateStatus("Loading worlds...");

        // Request worlds from presenter
        await presenter.LoadWorlds();
    }

    /// <summary>
    /// Display the list of worlds (called by presenter)
    /// </summary>
    public void DisplayWorlds(WorldModel[] worlds)
    {
        // Clear existing items
        ClearWorldList();

        if (worlds == null || worlds.Length == 0)
        {
            UpdateStatus("No worlds found.");
            ShowLoading(false);
            return;
        }

        // Instantiate world items
        foreach (var world in worlds)
        {
            CreateWorldItem(world);
        }

        UpdateStatus($"Loaded {worlds.Length} world(s)");
        ShowLoading(false);
    }

    private void CreateWorldItem(WorldModel world)
    {
        if (worldItemPrefab == null || worldListContainer == null)
        {
            Debug.LogError("WorldItemPrefab or WorldListContainer not assigned!");
            return;
        }

        // Instantiate the prefab
        GameObject itemObj = Instantiate(worldItemPrefab, worldListContainer);
        itemObj.SetActive(true);

        // Get the WorldItemView component and set data
        WorldItemView itemView = itemObj.GetComponent<WorldItemView>();
        if (itemView != null)
        {
            itemView.SetWorldData(world);
            worldItemInstances.Add(itemView);
        }
        else
        {
            Debug.LogError("WorldItemPrefab is missing WorldItemView component!");
        }
    }

    private void ClearWorldList()
    {
        // Destroy all existing world items
        foreach (var item in worldItemInstances)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        worldItemInstances.Clear();
    }

    public void ShowError(string message)
    {
        UpdateStatus($"Error: {message}");
        ShowLoading(false);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Debug.Log($"[MyWorldListView] {message}");
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }

    private void OnDestroy()
    {
        ClearWorldList();
    }

    // Expose presenter for other views (e.g., CreateWorld) to call presenter actions
    public MyWorldListPresenter GetPresenter() => presenter;

    /// <summary>
    /// Add a single world to the displayed list (called by presenter)
    /// </summary>
    public void AddWorld(WorldModel world)
    {
        if (world == null) return;
        CreateWorldItem(world);
        UpdateStatus($"World '{world.worldName}' created.");
    }

    #region Public API for Testing

    [ContextMenu("Reload Worlds")]
    public async void ReloadWorlds()
    {
        await LoadWorlds();
    }

    #endregion
}
