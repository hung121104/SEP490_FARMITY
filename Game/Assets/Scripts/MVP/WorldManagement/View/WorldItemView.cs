using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// Component attached to WorldItem prefab to display individual world data
/// </summary>
public class WorldItemView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Runtime ID")]
    [SerializeField]
    private string id; // serialized so it's visible in inspector at runtime

    [Header("Scene")]
    [SerializeField]
    [Tooltip("Optional: name of the scene to load when this world is selected")]
    private string sceneToLoad;

    [Header("Rename")]
    [SerializeField]
    [Tooltip("Optional reference to UpdateWorld helper in world list scene")]
    private UpdateWorld updateWorldView;

    [Header("Delete")]
    [SerializeField]
    [Tooltip("Optional reference to DeleteWorld helper in world list scene")]
    private DeleteWorld deleteWorldView;

    private WorldModel worldData;

    /// <summary>
    /// Initialize the world item with data
    /// </summary>
    public void SetWorldData(WorldModel world)
    {
        worldData = world;
        // store the API _id on this GameObject for easy access later
        id = world?._id;
        UpdateDisplay();
    }

    /// <summary>
    /// The persistent ID from the API for this world item.
    /// Use this as an untrusted key; validate on server for sensitive ops.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Load the configured scene and pass the selected world's id to the next scene via WorldSelectionManager.
    /// Useful to call from a UI Button on click.
    /// </summary>
    public void LoadWorld()
    {
        LoadWorld(sceneToLoad);
    }

    /// <summary>
    /// Load a specific scene by name and pass the selected world's id.
    /// </summary>
    public void LoadWorld(string sceneName)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Debug.LogError("WorldItemView.LoadWorld: Id is empty - cannot load world.");
            return;
        }

        // ensure a session manager exists and store the id and name in-session only (no PlayerPrefs)
        var manager = WorldSelectionManager.EnsureExists();
        string displayName = worldData != null && !string.IsNullOrEmpty(worldData.worldName) 
            ? worldData.worldName 
            : "Unnamed World";
        manager.SetSelectedWorld(Id, displayName);
        manager.SetNewWorld(false);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("WorldItemView.LoadWorld: no scene name provided. Scene will not be loaded, but id is saved.");
            return;
        }

        // Properly handle Photon message queue when loading scenes
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.IsMessageQueueRunning = false;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// UI button hook from world-list item edit button.
    /// Opens shared rename popup and binds this item's world id/name.
    /// </summary>
    public void OnEditWorldNameButton()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Debug.LogWarning("[WorldItemView] Missing world id. Rename skipped.");
            return;
        }

        if (updateWorldView == null)
        {
            updateWorldView = UnityEngine.Object.FindFirstObjectByType<UpdateWorld>();
        }

        if (updateWorldView == null)
        {
            Debug.LogError("[WorldItemView] UpdateWorld helper not found in scene.");
            return;
        }

        string currentName = worldData != null ? worldData.worldName : string.Empty;
        updateWorldView.BeginRenameForWorld(Id, currentName);
    }

    /// <summary>
    /// UI button hook from world-list item delete button.
    /// Opens shared delete confirmation popup bound to this world id.
    /// </summary>
    public void OnDeleteWorldButton()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Debug.LogWarning("[WorldItemView] Missing world id. Delete skipped.");
            return;
        }

        if (deleteWorldView == null)
        {
            deleteWorldView = UnityEngine.Object.FindFirstObjectByType<DeleteWorld>();
        }

        if (deleteWorldView == null)
        {
            Debug.LogError("[WorldItemView] DeleteWorld helper not found in scene.");
            return;
        }

        string currentName = worldData != null ? worldData.worldName : "Unnamed World";
        deleteWorldView.BeginDeleteForWorld(Id, currentName);
    }

    private void UpdateDisplay()
    {
        if (worldData == null)
        {
            Debug.LogError("WorldItemView: worldData is null!");
            return;
        }

        // Debug log to check data
        Debug.Log($"Displaying world: _id={worldData._id}, worldName='{worldData.worldName}', day={worldData.day}, gold={worldData.gold}");

        // Set world name
        if (worldNameText != null)
        {
            string displayName = string.IsNullOrEmpty(worldData.worldName) ? "Unnamed World" : worldData.worldName;
            worldNameText.text = displayName;
            Debug.Log($"Set world name text to: {displayName}");
        }

        // Set time information
        if (timeText != null)
        {
            timeText.text = $"Day {worldData.day}, {worldData.hour:D2}:{worldData.minute:D2}";
        }

        // Set gold amount
        if (goldText != null)
        {
            goldText.text = $"Gold: {worldData.gold}";
        }
    }

    public WorldModel GetWorldData() => worldData;
}
