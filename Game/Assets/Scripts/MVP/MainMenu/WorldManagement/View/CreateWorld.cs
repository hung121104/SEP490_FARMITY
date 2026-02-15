using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CreateWorld : MonoBehaviour
{
    public string apiUrl = "http://localhost:3000/player-data/world";
    public string token;
    public string worldName = "World 7";
    // Legacy InputField (UI) to read world name from; set in Inspector
    public InputField legacyWorldNameInput;

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
    }

    void OnError(string err)
    {
        Debug.LogError("Create world failed: " + err);
    }
}
