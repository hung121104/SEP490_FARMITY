using System.IO;
using UnityEngine;
using GameSaveManagement.Model;

namespace GameSaveManagement.Service
{
    public class GameSaveService : IGameSaveService
    {
        private readonly GameSaveModel _gameSaveModel;
        private static readonly string SaveFilePath = Application.persistentDataPath + "/save/player_save.json";

        public GameSaveService()
        {
            _gameSaveModel = new GameSaveModel();
        }

        // Persist the position and update the model
        public void SavePlayerPosition(Vector3 position)
        {
            _gameSaveModel.Position = position;

            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // DTO used for serialization to keep the model decoupled from storage format
            var dto = new PlayerData(position);
            string json = JsonUtility.ToJson(dto);
            File.WriteAllText(SaveFilePath, json);
#if UNITY_EDITOR
            Debug.Log("Saved player position to: " + SaveFilePath);
#endif
        }

        // Load from disk, update the model and return the position
        public Vector3 LoadPlayerPosition()
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                Vector3 pos = new Vector3(data.PositionX, data.PositionY, data.PositionZ);
                _gameSaveModel.Position = pos;
                return pos;
            }

            // No save file -> return whatever the model currently holds (default zero)
            return _gameSaveModel.Position;
        }

        [System.Serializable]
        private class PlayerData
        {
            public float PositionX;
            public float PositionY;
            public float PositionZ;

            public PlayerData(Vector3 position)
            {
                PositionX = position.x;
                PositionY = position.y;
                PositionZ = position.z;
            }
        }
    }
}