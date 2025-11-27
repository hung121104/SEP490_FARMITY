using UnityEngine;

namespace GameSaveManagement.Service
{
    public interface IGameSaveService
    {
        void SavePlayerPosition(Vector3 position);
        Vector3 LoadPlayerPosition();
    }
}