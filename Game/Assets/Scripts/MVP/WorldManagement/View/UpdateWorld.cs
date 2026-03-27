using System.Collections;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// MVP View helper used only for updating world names.
/// It sends worldId + worldName to PUT /player-data/world and does not perform auto-save.
/// </summary>
public class UpdateWorld : MonoBehaviour
{
    [Header("Reusable Rename Panel")]
    [SerializeField] private TMP_InputField worldNameInput;
    [SerializeField] private InputField legacyWorldNameInput;
    [SerializeField] private GameObject renamePanelRoot;

    private string targetWorldId;

    /// <summary>
    /// Bind the popup to a specific world before showing it.
    /// </summary>
    public void BeginRenameForWorld(string worldId, string currentWorldName)
    {
        targetWorldId = worldId;
        SetInputText(currentWorldName ?? string.Empty);

        if (renamePanelRoot != null)
        {
            renamePanelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// UI button hook on the reusable popup confirm button.
    /// </summary>
    public void OnUpdateButtonOnClick()
    {
        string proposedName = ReadInputText();
        if (string.IsNullOrEmpty(proposedName))
        {
            NotifyStatus("Rename failed: World name is empty.");
            return;
        }

        UpdateWorldNameById(targetWorldId, proposedName, (ok, response) =>
        {
            if (!ok)
            {
                NotifyStatus($"Rename failed: {response}");
                return;
            }

            NotifyStatus($"World renamed to '{proposedName}'.");
            RefreshWorldList();
            ClearRenameState();
            if (renamePanelRoot != null)
            {
                renamePanelRoot.SetActive(false);
            }
        });
    }

    /// <summary>
    /// Optional close/cancel hook for popup close button.
    /// </summary>
    public void OnCancelRenameButtonClick()
    {
        ClearRenameState();
        if (renamePanelRoot != null)
        {
            renamePanelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Programmatic entry point for world-list flow where worldId is known per item.
    /// </summary>
    public void UpdateWorldNameById(string worldId, string newWorldName, Action<bool, string> onComplete = null)
    {
        string sanitized = string.IsNullOrEmpty(newWorldName) ? null : newWorldName.Trim();
        if (string.IsNullOrEmpty(sanitized))
        {
            Debug.LogWarning("[UpdateWorld] World name is empty. Update cancelled.");
            onComplete?.Invoke(false, "World name is empty.");
            return;
        }

        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogWarning("[UpdateWorld] Missing worldId. Rename skipped.");
            onComplete?.Invoke(false, "Missing worldId.");
            return;
        }

        StartCoroutine(UpdateWorldNameRoutine(worldId, sanitized, onComplete));
    }

    private string ReadInputText()
    {
        if (worldNameInput != null && !string.IsNullOrEmpty(worldNameInput.text))
        {
            return worldNameInput.text.Trim();
        }
        if (legacyWorldNameInput != null && !string.IsNullOrEmpty(legacyWorldNameInput.text))
        {
            return legacyWorldNameInput.text.Trim();
        }
        return null;
    }

    private void SetInputText(string value)
    {
        if (worldNameInput != null)
        {
            worldNameInput.text = value;
        }
        if (legacyWorldNameInput != null)
        {
            legacyWorldNameInput.text = value;
        }
    }

    private void ClearRenameState()
    {
        targetWorldId = null;
        SetInputText(string.Empty);
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
            Debug.Log($"[UpdateWorld] {message}");
        }
    }

    private void RefreshWorldList()
    {
        MyWorldListView listView = UnityEngine.Object.FindFirstObjectByType<MyWorldListView>();
        if (listView != null)
        {
            listView.ReloadWorlds();
        }
    }

    private IEnumerator UpdateWorldNameRoutine(string worldId, string newWorldName, Action<bool, string> onComplete)
    {
        string jwt = SessionManager.Instance?.JwtToken;
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogWarning("[UpdateWorld] Missing JWT token. Rename skipped.");
            onComplete?.Invoke(false, "Missing JWT token.");
            yield break;
        }

        var request = new WorldApi.UpdateWorldRequest
        {
            worldId = worldId,
            worldName = newWorldName,
        };

        yield return StartCoroutine(WorldApi.UpdateWorld(jwt, request, (ok, json) =>
        {
            if (!ok)
            {
                Debug.LogWarning($"[UpdateWorld] Rename failed: {json}");
                onComplete?.Invoke(false, json);
                return;
            }

            Debug.Log($"[UpdateWorld] World renamed to '{newWorldName}'.");
            onComplete?.Invoke(true, json);
        }));
    }
}
