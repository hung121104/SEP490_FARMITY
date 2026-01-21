using UnityEngine;
using System.Threading.Tasks;

public interface ISaveGameService
{
    void SavePlayerPosition(Transform playerTransform, string playerName);
    Task<SaveGameDataModel> LoadPlayerPosition(string playerName);
}
