using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Photon.Pun;
using Photon.Realtime;
using Newtonsoft.Json;

public class LoadPlayerData : MonoBehaviourPunCallbacks
{
    private const string TRACE = "[HPTRACE]";

    void Start()
    {
        StartCoroutine(WaitAndApplyAllPositions());
    }

    // Only the master needs to handle joining players — non-master clients self-load their own position.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("player join room");
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(WaitAndApplyPositionForPlayer(newPlayer));
    }

    private IEnumerator WaitAndApplyPositionForPlayer(Player newPlayer)
    {
        // Wait until WorldDataBootstrapper is ready
        yield return new WaitUntil(() =>
            WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady);

        // Wait for the joining client to broadcast their accountId via custom player properties.
        // SpawnPlayer.cs calls SetCustomProperties("accountId") on the joining client.
        float timeout = 10f;
        float elapsed = 0f;
        while (!newPlayer.CustomProperties.ContainsKey("accountId") && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (!newPlayer.CustomProperties.ContainsKey("accountId"))
        {
            Debug.LogWarning($"[LoadPlayerData] Timed out waiting for accountId property from player '{newPlayer.NickName}'.");
            yield break;
        }

        string playerId = newPlayer.CustomProperties["accountId"] as string;
        Debug.Log($"[LoadPlayerData] Joining player '{newPlayer.NickName}' has accountId='{playerId}'");

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[LoadPlayerData] accountId property is empty for joining player.");
            yield break;
        }

        // Wait until the player's GameObject is spawned in the scene
        PhotonView targetView = null;
        elapsed = 0f;
        while (targetView == null && elapsed < timeout)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == newPlayer.ActorNumber)
                {
                    targetView = pv;
                    break;
                }
            }
            if (targetView == null)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        if (targetView == null)
        {
            Debug.LogWarning($"[LoadPlayerData] Timed out waiting for PlayerEntity of '{newPlayer.NickName}'.");
            yield break;
        }

        if (!PlayerDataManager.Instance.players.Exists(p => p.accountId == playerId))
        {
            var known = string.Join(", ", PlayerDataManager.Instance.players.ConvertAll(p => p.accountId));
            Debug.LogWarning($"[LoadPlayerData] No data found for joining player '{playerId}'. Known accounts: [{known}]");
            yield break;
        }

        PlayerData data = PlayerDataManager.Instance.players.Find(p => p.accountId == playerId);
        Vector3 loadedPos = new Vector3(data.positionX, data.positionY, targetView.transform.position.z);

        targetView.RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
        Debug.Log($"[LoadPlayerData] Applied position for joining player '{playerId}': {loadedPos}");

        int restoredHealth = Mathf.Max(0, Mathf.RoundToInt(data.currentHealth));
        targetView.RPC("RPC_RestoreHealthFromMaster", targetView.Owner, restoredHealth);
        Debug.Log($"{TRACE} [LoadPlayerData] Sent health restore RPC to joining player '{playerId}' health={restoredHealth}");

        // Restore saved appearance via Custom Properties so all clients see it
        var appearance = targetView.GetComponent<PlayerAppearanceSync>();
        if (appearance != null)
        {
            targetView.RPC("RPC_RestoreAppearance", targetView.Owner,
                data.hairConfigId   ?? "",
                data.outfitConfigId ?? "",
                data.hatConfigId    ?? "",
                data.toolConfigId   ?? "");
        }
    }

    private IEnumerator WaitAndApplyAllPositions()
    {
        // Skip position restore entirely for freshly-created worlds — the server assigns
        // positionX=0 / positionY=0 as defaults which would teleport the player away from
        // the designed spawn point.  The flag is cleared here so re-entering later works.
        if (WorldSelectionManager.Instance != null && WorldSelectionManager.Instance.IsNewWorld)
        {
            Debug.Log("[LoadPlayerData] New world detected — skipping position restore, keeping spawn-point position.");
            WorldSelectionManager.Instance.SetNewWorld(false);
            yield break;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            // Non-master: self-load position directly from API — don't depend on master.
            yield return StartCoroutine(LoadOwnPositionFromServer());
            yield break;
        }

        // Master: wait for WorldDataBootstrapper, then apply positions for all current players.
        yield return new WaitUntil(() =>
            WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady);

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("PlayerEntity");

        foreach (var player in playerObjects)
        {
            PhotonView view = player.GetComponent<PhotonView>();

            if (view == null || view.Owner == null)
            {
                Debug.LogError($"[LoadPlayerData] No valid PhotonView or owner on {player.name}. Skipping.");
                continue;
            }

            string userId = view.Owner.UserId;

            if (!PlayerDataManager.Instance.players.Exists(p => p.accountId == userId))
            {
                Debug.LogWarning($"[LoadPlayerData] No data found for player {userId}. Skipping.");
                continue;
            }

            PlayerData data = PlayerDataManager.Instance.players.Find(p => p.accountId == userId);
            Vector3 loadedPos = new Vector3(data.positionX, data.positionY, player.transform.position.z);

            view.RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
            Debug.Log($"[LoadPlayerData] Synced position for {userId}: {loadedPos}");

            int restoredHealth = Mathf.Max(0, Mathf.RoundToInt(data.currentHealth));
            view.RPC("RPC_RestoreHealthFromMaster", view.Owner, restoredHealth);
            Debug.Log($"{TRACE} [LoadPlayerData] Sent health restore RPC to '{userId}' health={restoredHealth}");

            // Restore saved appearance for this player
            var appearanceSync = player.GetComponent<PlayerAppearanceSync>();
            if (appearanceSync != null)
            {
                view.RPC("RPC_RestoreAppearance", view.Owner,
                    data.hairConfigId   ?? "",
                    data.outfitConfigId ?? "",
                    data.hatConfigId    ?? "",
                    data.toolConfigId   ?? "");
            }
        }
    }

    // Each non-master client fetches its own saved position directly from the server.
    // This is independent of the master's PlayerDataManager, fixing the case where
    // the master's cached character list doesn't include the joining player.
    private IEnumerator LoadOwnPositionFromServer()
    {
        string worldId = WorldSelectionManager.Instance?.SelectedWorldId;
        string accountId = SessionManager.Instance?.UserId;
        string jwt = SessionManager.Instance?.JwtToken;

        if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(jwt))
        {
            Debug.LogWarning("[LoadPlayerData] Missing worldId, accountId, or JWT — cannot self-load position.");
            yield break;
        }

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/player-data/world?_id={worldId}";
        Debug.Log($"[LoadPlayerData] Non-master self-fetching position from: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + jwt);
            req.certificateHandler = new AcceptAllCertificatesHandler();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LoadPlayerData] Position fetch failed: {req.responseCode} {req.error}");
                yield break;
            }

            WorldApiResponse data;
            try { data = JsonConvert.DeserializeObject<WorldApiResponse>(req.downloadHandler.text); }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadPlayerData] Parse error: {ex.Message}");
                yield break;
            }

            if (data?.characters == null)
            {
                Debug.LogWarning("[LoadPlayerData] No characters array in response.");
                yield break;
            }

            WorldApiResponse.CharacterEntry myEntry = data.characters.Find(c => c.accountId == accountId);
            if (myEntry == null)
            {
                Debug.LogWarning($"[LoadPlayerData] No saved position for accountId '{accountId}' in world '{worldId}'.");
                yield break;
            }

            // Wait for the local player entity to be in the scene.
            GameObject localPlayer = null;
            float elapsed = 0f;
            while (localPlayer == null && elapsed < 10f)
            {
                foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
                {
                    PhotonView pv = go.GetComponent<PhotonView>();
                    if (pv != null && pv.IsMine) { localPlayer = go; break; }
                }
                if (localPlayer == null)
                {
                    yield return new WaitForSeconds(0.2f);
                    elapsed += 0.2f;
                }
            }

            if (localPlayer == null)
            {
                Debug.LogWarning("[LoadPlayerData] Timed out waiting for local PlayerEntity.");
                yield break;
            }

            Vector3 loadedPos = new Vector3(myEntry.positionX, myEntry.positionY, localPlayer.transform.position.z);
            localPlayer.GetComponent<PhotonView>().RPC("SetLoadedPosition", RpcTarget.All, loadedPos);
            Debug.Log($"[LoadPlayerData] Self-loaded position for '{accountId}': {loadedPos}");

            var healthPresenter = localPlayer.GetComponent<CombatManager.Presenter.PlayerHealthPresenter>();
            int restoredHealth = Mathf.Max(0, Mathf.RoundToInt(myEntry.currentHealth));
            if (healthPresenter != null && healthPresenter.GetService() != null)
            {
                healthPresenter.GetService().SetCurrentHealth(restoredHealth);
                Debug.Log($"{TRACE} [LoadPlayerData] Self-loaded health for '{accountId}' from world API: {restoredHealth}");
            }
            else
            {
                Debug.LogWarning($"{TRACE} [LoadPlayerData] Could not self-apply health for '{accountId}' (presenter/service missing). expectedHealth={restoredHealth}");
            }

            // Restore saved appearance — broadcast via Custom Properties
            var appearance = localPlayer.GetComponent<PlayerAppearanceSync>();
            if (appearance != null)
            {
                appearance.SetAll(
                    myEntry.hairConfigId   ?? "",
                    myEntry.outfitConfigId ?? "",
                    myEntry.hatConfigId    ?? "",
                    myEntry.toolConfigId   ?? "");
            }
        }
    }

    private class AcceptAllCertificatesHandler : UnityEngine.Networking.CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
