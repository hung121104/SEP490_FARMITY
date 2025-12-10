using UnityEngine;

namespace GameSaveManagement.View
{
    public class GameSaveView : MonoBehaviour
    {
        public void DisplaySaveMessage()
        {
            Debug.Log("Game saved successfully!");
        }

        public void DisplayLoadMessage(Vector3 position)
        {
            Debug.Log($"Game loaded! Player position: {position}");
        }
    }
}