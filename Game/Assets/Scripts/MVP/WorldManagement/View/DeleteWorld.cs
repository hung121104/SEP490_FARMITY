using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Reusable confirmation popup for deleting a world from the world list.
/// </summary>
public class DeleteWorld : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject deleteConfirmPanelRoot;
    [SerializeField] private TextMeshProUGUI confirmMessageText;

    private string targetWorldId;
    private string targetWorldName;

    /// <summary>
    /// Bind popup to a specific world and show it.
    /// </summary>
    public void BeginDeleteForWorld(string worldId, string worldName)
    {
        targetWorldId = worldId;
        targetWorldName = string.IsNullOrEmpty(worldName) ? "Unnamed World" : worldName;

        if (confirmMessageText != null)
        {
            confirmMessageText.text = $"Delete world '{targetWorldName}'?";
        }

        if (deleteConfirmPanelRoot != null)
        {
            deleteConfirmPanelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// Confirm button hook in popup.
    /// </summary>
    public async void OnConfirmDeleteButtonClick()
    {
        if (string.IsNullOrEmpty(targetWorldId))
        {
            NotifyStatus("Delete failed: Missing world id.");
            return;
        }

        MyWorldListView listView = UnityEngine.Object.FindFirstObjectByType<MyWorldListView>();
        if (listView == null)
        {
            Debug.LogError("[DeleteWorld] MyWorldListView not found in scene.");
            return;
        }

        WorldPresenter presenter = listView.GetPresenter();
        if (presenter == null)
        {
            listView.UpdateStatus("Delete failed: Presenter not initialized.");
            return;
        }

        (bool success, string message) result = await presenter.DeleteWorld(targetWorldId);

        if (!result.success)
        {
            listView.UpdateStatus($"Delete failed: {result.message}");
            return;
        }

        listView.UpdateStatus($"World '{targetWorldName}' deleted.");
        listView.ReloadWorlds();

        ClearDeleteState();
        if (deleteConfirmPanelRoot != null)
        {
            deleteConfirmPanelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Cancel/close button hook in popup.
    /// </summary>
    public void OnCancelDeleteButtonClick()
    {
        ClearDeleteState();
        if (deleteConfirmPanelRoot != null)
        {
            deleteConfirmPanelRoot.SetActive(false);
        }
    }

    private void ClearDeleteState()
    {
        targetWorldId = null;
        targetWorldName = null;

        if (confirmMessageText != null)
        {
            confirmMessageText.text = string.Empty;
        }
    }

    private void NotifyStatus(string message)
    {
        MyWorldListView listView = UnityEngine.Object.FindFirstObjectByType<MyWorldListView>();
        if (listView != null)
        {
            listView.UpdateStatus(message);
        }
        else
        {
            Debug.LogWarning($"[DeleteWorld] {message}");
        }
    }
}
