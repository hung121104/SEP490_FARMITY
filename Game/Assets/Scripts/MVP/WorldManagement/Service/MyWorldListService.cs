using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class MyWorldListService : IMyWorldListService
{
    private const string BASE_URL = AppConfig.ApiBaseUrl;

    [System.Serializable]
    private class ErrorPayload
    {
        public int statusCode;
        public string message;
    }

    public async Task<WorldModel[]> GetWorlds(string ownerId = null)
    {
        // Ensure user is authenticated
        if (!SessionManager.Instance.IsAuthenticated())
        {
            Debug.LogError("User not authenticated. Cannot retrieve worlds.");
            return null;
        }

        // Build URL with optional query parameter
        string url = $"{BASE_URL}/player-data/worlds";
        if (!string.IsNullOrEmpty(ownerId))
        {
            url += $"?ownerId={ownerId}";
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Add Authorization header with Bearer token
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.JwtToken);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Raw JSON Response: {jsonResponse}");

                // Parse the response - expecting an array of world documents
                WorldListResponse response = JsonUtility.FromJson<WorldListResponse>("{\"worlds\":" + jsonResponse + "}");
                
                // Debug: Check parsed data
                if (response != null && response.worlds != null)
                {
                    Debug.Log($"Parsed {response.worlds.Length} worlds");
                    for (int i = 0; i < response.worlds.Length && i < 3; i++)
                    {
                        var world = response.worlds[i];
                        Debug.Log($"World {i}: _id={world._id}, worldName={world.worldName}, day={world.day}, gold={world.gold}");
                    }
                }
                
                return response.worlds;
            }
            else
            {
                if (request.responseCode == 401)
                    SessionManager.Instance?.HandleJwtExpired("Get worlds unauthorized (401)");

                Debug.LogError($"Error retrieving worlds: {request.error}");
                return null;
            }
        }
    }

    public async Task<WorldResponse> CreateWorld(string worldName)
    {
        if (!SessionManager.Instance.IsAuthenticated())
        {
            throw new System.Exception("User not authenticated.");
        }

        string url = $"{BASE_URL}/player-data/world";
        string body = "{\"worldName\": \"" + worldName + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.JwtToken);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    var resp = JsonUtility.FromJson<WorldResponse>(jsonResponse);
                    Debug.Log($"Created world: {resp.worldName} id={resp._id}");
                    return resp;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed to parse create-world response: " + ex.Message);
                    throw new System.Exception("Failed to parse create-world response.");
                }
            }
            else
            {
                if (request.responseCode == 401)
                    SessionManager.Instance?.HandleJwtExpired("Create world unauthorized (401)");

                string raw = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string message = ExtractErrorMessage(raw, request.error);
                Debug.LogError($"Error creating world: {request.error} (code: {request.responseCode}) - {raw}");
                throw new System.Exception(message);
            }
        }
    }

    private static string ExtractErrorMessage(string rawResponse, string fallback)
    {
        if (!string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                ErrorPayload payload = JsonUtility.FromJson<ErrorPayload>(rawResponse);
                if (payload != null && !string.IsNullOrEmpty(payload.message))
                {
                    return payload.message;
                }
            }
            catch
            {
                // Ignore parse errors and use fallback text below.
            }

            return rawResponse;
        }

        return string.IsNullOrEmpty(fallback) ? "Unknown create world error." : fallback;
    }

    public async Task<(bool success, string message)> DeleteWorld(string worldId)
    {
        if (!SessionManager.Instance.IsAuthenticated())
        {
            Debug.LogError("User not authenticated. Cannot delete world.");
            return (false, "User not authenticated.");
        }

        if (string.IsNullOrEmpty(worldId))
        {
            return (false, "Missing world id.");
        }

        string url = $"{BASE_URL}/player-data/world?_id={UnityWebRequest.EscapeURL(worldId)}";

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.JwtToken);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return (true, "World deleted successfully.");
            }

            string response = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.responseCode == 401)
                SessionManager.Instance?.HandleJwtExpired("Delete world unauthorized (401)");

            string error = !string.IsNullOrEmpty(response) ? response : request.error;
            Debug.LogError($"Error deleting world: {request.error} (code: {request.responseCode}) - {response}");
            return (false, error);
        }
    }
}
