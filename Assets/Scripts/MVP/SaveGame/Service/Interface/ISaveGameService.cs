using UnityEngine;

public interface ISaveGameService
{
    void SavePlayerPosition(Transform playerTransform, string playerName);
    SaveGameDataModel LoadPlayerPosition(string playerName);
}
