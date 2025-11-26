using System.IO;
using UnityEngine;

namespace GameSaveManagement.Model
{
    public class GameSaveModel
    {
        private const string SaveFilePath = "player_save.json";

        public void SavePosition(Vector3 position)
        {
            string json = JsonUtility.ToJson(new PlayerData(position));
            File.WriteAllText(SaveFilePath, json);
        }

        public Vector3 LoadPosition()
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                return new Vector3(data.PositionX, data.PositionY, data.PositionZ);
            }

            return Vector3.zero; // Default position if no save file exists
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