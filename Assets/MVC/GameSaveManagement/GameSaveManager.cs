using UnityEngine;
using GameSaveManagement.Controller;
using GameSaveManagement.View;

public class GameSaveManager : MonoBehaviour
{
    private GameSaveController saveController;
    private GameSaveView saveView;

    public Transform playerTransform; // Reference to the player's Transform

    private void Awake()
    {
        // Initialize the GameSaveController and GameSaveView
        saveController = GetComponent<GameSaveController>();
        saveView = GetComponent<GameSaveView>();
    }

    public void SaveGame()
    {
        if (playerTransform != null)
        {
            // Save the player's position
            saveController.SavePlayerPosition(playerTransform.position);

            // Display a save message
            if (saveView != null)
            {
                saveView.DisplaySaveMessage();
            }
        }
        else
        {
            Debug.LogError("Player Transform is not assigned in GameSaveManager!");
        }
    }

    public void LoadGame()
    {
        if (playerTransform != null)
        {
            // Load the player's position
            Vector3 loadedPosition = saveController.LoadPlayerPosition();
            playerTransform.position = loadedPosition;

            // Display a load message
            if (saveView != null)
            {
                saveView.DisplayLoadMessage(loadedPosition);
            }
        }
        else
        {
            Debug.LogError("Player Transform is not assigned in GameSaveManager!");
        }
    }
}