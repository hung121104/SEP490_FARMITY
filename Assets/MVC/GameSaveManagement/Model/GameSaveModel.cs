using System.IO;
using UnityEngine;

namespace GameSaveManagement.Model
{
    [System.Serializable]
    public class GameSaveModel
    {
        // Pure data container for saved player state.
        // Keep public fields so Unity's JsonUtility can serialize/deserialize when needed.
        public Vector3 Position;

        public GameSaveModel()
        {
            Position = Vector3.zero;
        }

        public GameSaveModel(Vector3 position)
        {
            Position = position;
        }
    }
}