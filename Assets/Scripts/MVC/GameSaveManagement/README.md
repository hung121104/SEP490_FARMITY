# Game Save Management (MVC)

This module handles saving and loading the player's position in the game. It follows the MVC (Model-View-Controller) design pattern.

---

## 📂 File Structure

```
GameSaveManagement/
├── Controller/
│   └── GameSaveController.cs  # Handles communication between Model and View
├── Model/
│   └── GameSaveModel.cs       # Handles data saving and loading logic
├── View/
│   └── GameSaveView.cs        # Handles user feedback (e.g., messages)
└── README.md                  # Documentation
```

---

## 🛠️ How to Use

### 1️⃣ Add the GameSaveController to a GameObject

1. Create an empty GameObject in your Unity scene.
2. Rename it to `GameSaveManager`.
3. Attach the `GameSaveController` script to the `GameSaveManager` GameObject.

### 2️⃣ Save Player Position

Call the `SavePlayerPosition(Vector3 position)` method from the `GameSaveController` to save the player's position. For example:

```csharp
GameSaveController saveController = FindObjectOfType<GameSaveController>();
Vector3 playerPosition = player.transform.position;
saveController.SavePlayerPosition(playerPosition);
```

### 3️⃣ Load Player Position

Call the `LoadPlayerPosition()` method from the `GameSaveController` to load the player's position. For example:

```csharp
GameSaveController saveController = FindObjectOfType<GameSaveController>();
Vector3 loadedPosition = saveController.LoadPlayerPosition();
player.transform.position = loadedPosition;
```

### 4️⃣ Display Messages

Use the `GameSaveView` to display messages to the user:

```csharp
GameSaveView saveView = FindObjectOfType<GameSaveView>();
saveView.DisplaySaveMessage();
```

---

## 🔹 Notes

1. The player's position is saved to a JSON file named `player_save.json` in the root directory of the project.
2. If no save file exists, the default position returned is `(0, 0, 0)`.
3. This implementation does not include encryption for the save file.

---

## 📜 Example Usage

Here is an example of integrating the save and load functionality:

```csharp
using UnityEngine;
using GameSaveManagement.Controller;
using GameSaveManagement.View;

public class PlayerManager : MonoBehaviour
{
    private GameSaveController saveController;
    private GameSaveView saveView;

    private void Start()
    {
        saveController = FindObjectOfType<GameSaveController>();
        saveView = FindObjectOfType<GameSaveView>();
    }

    public void SaveGame()
    {
        Vector3 playerPosition = transform.position;
        saveController.SavePlayerPosition(playerPosition);
        saveView.DisplaySaveMessage();
    }

    public void LoadGame()
    {
        Vector3 loadedPosition = saveController.LoadPlayerPosition();
        transform.position = loadedPosition;
        saveView.DisplayLoadMessage(loadedPosition);
    }
}
```