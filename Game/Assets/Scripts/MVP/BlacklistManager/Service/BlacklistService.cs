using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class BlacklistService : IBlacklistService
{
    private const string ENDPOINT = "/player-data/world/blacklist";

    public async Task<WorldBlacklistResponse> GetBlacklist(string worldId)
    {
        if (!HasValidSession() || string.IsNullOrEmpty(worldId))
            return null;

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}?_id={UnityWebRequest.EscapeURL(worldId)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.JwtToken);
            request.certificateHandler = new BypassCertificateHandler();

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[BlacklistService] GET failed ({request.responseCode}): {request.error} - {request.downloadHandler.text}");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<WorldBlacklistResponse>(request.downloadHandler.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[BlacklistService] Failed to parse GET response: " + ex.Message);
                return null;
            }
        }
    }

    public async Task<BlacklistMutateResponse> AddToBlacklist(string worldId, string playerId)
    {
        return await SendMutate("POST", worldId, playerId);
    }

    public async Task<BlacklistMutateResponse> RemoveFromBlacklist(string worldId, string playerId)
    {
        return await SendMutate("DELETE", worldId, playerId);
    }

    private async Task<BlacklistMutateResponse> SendMutate(string method, string worldId, string playerId)
    {
        if (!HasValidSession() || string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(playerId))
            return null;

        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}{ENDPOINT}";
        BlacklistMutateRequest payload = new BlacklistMutateRequest { _id = worldId, playerId = playerId };
        string json = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.JwtToken);
            request.certificateHandler = new BypassCertificateHandler();

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[BlacklistService] {method} failed ({request.responseCode}): {request.error} - {request.downloadHandler.text}");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<BlacklistMutateResponse>(request.downloadHandler.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BlacklistService] Failed to parse {method} response: " + ex.Message);
                return null;
            }
        }
    }

    private bool HasValidSession()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.IsAuthenticated())
        {
            Debug.LogError("[BlacklistService] Missing authentication session.");
            return false;
        }

        return true;
    }

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
