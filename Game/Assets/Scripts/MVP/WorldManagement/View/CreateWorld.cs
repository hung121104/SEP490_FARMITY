using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

public class CreateWorld : MonoBehaviour
{
    private static readonly Regex WorldNameRegex = new Regex(@"^[A-Za-z0-9 ]+$");

    public string token;
    public string worldName = "Unnamed world";
    // Legacy InputField (UI) to read world name from; set in Inspector
    public InputField legacyWorldNameInput;
    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Scene")]
    [SerializeField]
    [Tooltip("Scene to load after world creation (e.g., LoadWorldScene)")]
    private string sceneToLoad = "LoadGameScene";

    private WorldPresenter presenter;

    private void Awake()
    {
        // Try to reuse existing MyWorldListView's presenter if available
        var listView = UnityEngine.Object.FindFirstObjectByType<MyWorldListView>();
        if (listView != null)
        {
            presenter = listView.GetPresenter();
        }

        // If no presenter found, create a local one (keeps behavior consistent)
        if (presenter == null)
        {
            IWorldService service = new WorldService();
            presenter = new WorldPresenter(service);
        }
    }

    // Call this from a UI button to create a world
    public async void OnCreateButton()
    {
        SetStatus(string.Empty);

        string worldNameToUse = worldName;
        if (legacyWorldNameInput != null && !string.IsNullOrEmpty(legacyWorldNameInput.text))
        {
            worldNameToUse = legacyWorldNameInput.text;
        }

        worldNameToUse = worldNameToUse != null ? worldNameToUse.Trim() : string.Empty;
        if (string.IsNullOrEmpty(worldNameToUse))
        {
            OnError("World name cannot be empty.");
            return;
        }

        if (!WorldNameRegex.IsMatch(worldNameToUse))
        {
            OnError("World name must contain only alphabet letters and spaces.");
            return;
        }

        var result = await presenter.CreateWorld(worldNameToUse);
        if (result != null)
        {
            OnSuccess(new WorldResponse {
                _id = result._id,
                worldName = result.worldName,
                ownerId = result.ownerId,
            });
        }
        else
        {
            string err = !string.IsNullOrEmpty(presenter.LastCreateWorldError)
                ? presenter.LastCreateWorldError
                : "Create failed (see log).";
            OnError(err);
        }
    }

    void OnSuccess(WorldResponse resp)
    {
        Debug.Log("Created world: " + resp.worldName + " id: " + resp._id);
        SetStatus($"World '{resp.worldName}' created.");

        // Load the newly created world using WorldSelectionManager
        LoadCreatedWorld(resp._id, resp.worldName);
    }
    
    private void LoadCreatedWorld(string worldId, string worldName)
    {
        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogError("CreateWorld.LoadCreatedWorld: worldId is empty - cannot load world.");
            return;
        }
        
        // Store the world id and name in WorldSelectionManager
        var manager = WorldSelectionManager.EnsureExists();
        string displayName = !string.IsNullOrEmpty(worldName) ? worldName : "Unnamed World";
        manager.SetSelectedWorld(worldId, displayName);
        // Flag this as a new world so LoadPlayerData keeps the player at the spawn point
        // instead of teleporting them to the default (0,0) position the server assigns.
        manager.SetNewWorld(true);
        
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("CreateWorld.LoadCreatedWorld: no scene name provided. Scene will not be loaded, but id is saved.");
            return;
        }
        
        // Properly handle Photon message queue when loading scenes
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.IsMessageQueueRunning = false;
        }
        SceneManager.LoadScene(sceneToLoad);
    }

    void OnError(string err)
    {
        Debug.LogError("Create world failed: " + err);
        SetStatus("Create world failed: " + err);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
