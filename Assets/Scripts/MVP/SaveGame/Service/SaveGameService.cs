using SQLite;
using System.IO;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

[Serializable]
public class SavePositionRequest
{
    public string worldId;
    public string playerID;
    public float positionX;
    public float positionY;
    public int chunkIndex;
}

[Serializable]
public class LoadPositionResponse
{
    public float positionX;
    public float positionY;
    public int chunkIndex;
}

public class SaveGameService : ISaveGameService
{

    public async void SavePlayerPosition(Transform playerTransform, string PlayerName)
    {
        var requestData = new SavePositionRequest
        {
            worldId = "world123",
            playerID = PlayerName,
            positionX = playerTransform.position.x,
            positionY = playerTransform.position.y,
            chunkIndex = 5
        };

        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest("https://localhost:3000/character/save-position", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Position saved successfully to API");
            }
            else
            {
                Debug.LogError("Error saving position to API: " + request.error);
            }
        }
    }

    public async Task<SaveGameDataModel> LoadPlayerPosition(string playerId)
    {
        // Note: The accountId is hardcoded here for demonstration purposes.
//============================
        string url = $"https://localhost:3000/character/position?worldId=world123&accountId=696dca345c2e69905095f827";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                LoadPositionResponse response = JsonUtility.FromJson<LoadPositionResponse>(jsonResponse);

                var data = new SaveGameDataModel
                {
                    PlayerName = playerId,
                    PositionX = response.positionX,
                    PositionY = response.positionY,
                    PositionZ = 0f // Assuming Z is not provided, set to 0
                };

                Debug.Log($"Loaded position from API for {playerId}: X={data.PositionX}, Y={data.PositionY}");
                return data;
            }
            else
            {
                Debug.LogError("Error loading position from API: " + request.error);
                return null;
            }
        }
    }
}
