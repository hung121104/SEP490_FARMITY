using UnityEngine;

public class SaveGamePresenter
{
    private ISaveGameService saveService = new SaveGameService();

    public void SavePlayerPosition(Transform playerTransform, string playerId)
    {
        if (playerTransform != null)
        {
            saveService.SavePlayerPosition(playerTransform, playerId);
            Debug.Log($"Player position saved: X={playerTransform.position.x}, Y={playerTransform.position.y}");
        }
        else
        {
            Debug.LogError("Player transform not assigned. Please assign it in the Inspector.");
        }
    }
    public void LoadPlayerPosition(Transform playerTransform, string playerId)
    {
        var data = saveService.LoadPlayerPosition(playerId);
        if (data != null)
        {
            playerTransform.position = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
            Debug.Log($"Player position loaded: X={data.PositionX}, Y={data.PositionY}, Z={data.PositionZ}");
        }
        else
        {
            Debug.Log("No saved position found for the player.");
        }
    }
}
