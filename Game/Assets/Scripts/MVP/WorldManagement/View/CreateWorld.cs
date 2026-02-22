using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class CreateWorld : MonoBehaviour
{
    public string token;
    public string worldName = "Unnamed world";
    // Legacy InputField (UI) to read world name from; set in Inspector
    public InputField legacyWorldNameInput;
    
    [Header("Scene")]
    [SerializeField]
    [Tooltip("Scene to load after world creation (e.g., LoadWorldScene)")]
    private string sceneToLoad = "LoadGameScene";

    private MyWorldListPresenter presenter;

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
            IMyWorldListService service = new MyWorldListService();
            presenter = new MyWorldListPresenter(service);
        }
    }

    // Call this from a UI button to create a world
    public async void OnCreateButton()
    {
        string worldNameToUse = worldName;
        if (legacyWorldNameInput != null && !string.IsNullOrEmpty(legacyWorldNameInput.text))
        {
            worldNameToUse = legacyWorldNameInput.text;
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
            OnError("Create failed (see log).");
        }
    }

    void OnSuccess(WorldResponse resp)
    {
        Debug.Log("Created world: " + resp.worldName + " id: " + resp._id);
        
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
    }
}
