using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class UpdateWorld : MonoBehaviour
{
    [SerializeField] private float autoSaveInterval = 5f;

    void Start()
    {
        InvokeRepeating(nameof(OnAutoSaveTick), autoSaveInterval, autoSaveInterval);
    }

    private void OnAutoSaveTick()
    {
        // Only the master client drives the save — it has PlayerDataManager + WorldDataManager populated
        if (!PhotonNetwork.IsMasterClient) return;

        StartCoroutine(SaveWorld());
    }

    private IEnumerator SaveWorld()
    {
        // Guard: managers must be ready
        if (WorldDataBootstrapper.Instance == null || !WorldDataBootstrapper.Instance.IsReady)
        {
            Debug.LogWarning("[UpdateWorld] Skipping save — world data not ready yet.");
            yield break;
        }

        string worldId = WorldSelectionManager.Instance?.SelectedWorldId;
        if (string.IsNullOrEmpty(worldId))
        {
            Debug.LogWarning("[UpdateWorld] Skipping save — no worldId.");
            yield break;
        }

        string jwt = SessionManager.Instance?.JwtToken;
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogWarning("[UpdateWorld] Skipping save — no JWT token.");
            yield break;
        }

        // Build character list from live PlayerEntity positions
        var characters = new List<WorldApi.UpdateWorldRequest.CharacterUpdate>();
        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv == null || pv.Owner == null) continue;

            if (!pv.Owner.CustomProperties.TryGetValue("accountId", out object rawId)) continue;
            string accountId = rawId as string;
            if (string.IsNullOrEmpty(accountId)) continue;
            var cached = PlayerDataManager.Instance?.players.Find(p => p.accountId == accountId);

            characters.Add(new WorldApi.UpdateWorldRequest.CharacterUpdate
            {
                accountId    = accountId,
                positionX    = go.transform.position.x,
                positionY    = go.transform.position.y,
            });
        }

        var wdm = WorldDataManager.Instance;
        var request = new WorldApi.UpdateWorldRequest
        {
            worldId    = worldId,
            day        = wdm?.Day,
            month      = wdm?.Month,
            year       = wdm?.Year,
            hour       = wdm?.Hour,
            minute     = wdm?.Minute,
            gold       = wdm?.Gold,
            characters = characters.Count > 0 ? characters : null
        };

        yield return StartCoroutine(WorldApi.UpdateWorld(jwt, request, (ok, json) =>
        {
            if (ok) Debug.Log("[UpdateWorld] Auto-save successful.");
            else    Debug.LogWarning($"[UpdateWorld] Auto-save failed: {json}");
        }));
    }
}
