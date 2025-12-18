using SQLite;
using System.IO;
using UnityEngine;
using Photon.Pun;

public class SaveGameService : ISaveGameService
{
    private const string DatabaseName = "SaveGame.db";

    public void SavePlayerPosition(Transform playerTransform, string PlayerName)
    {
        string dbPath = Path.Combine(Application.persistentDataPath, DatabaseName);
        using (var db = new SQLiteConnection(dbPath))
        {
            db.CreateTable<SaveGameDataModel>();

            // Check if player already exists
            var existingData = db.Table<SaveGameDataModel>().FirstOrDefault(d => d.PlayerName == PlayerName);

            if (existingData != null)
            {
                // Update existing record
                existingData.PositionX = playerTransform.position.x;
                existingData.PositionY = playerTransform.position.y;
                existingData.PositionZ = playerTransform.position.z;
                db.Update(existingData);
                Debug.Log($"Updated position for {PlayerName} (Id: {existingData.Id})");
            }
            else
            {
                // Insert new record
                var data = new SaveGameDataModel
                {
                    PlayerName = PlayerName,
                    PositionX = playerTransform.position.x,
                    PositionY = playerTransform.position.y,
                    PositionZ = playerTransform.position.z
                };
                db.Insert(data);
                Debug.Log($"Inserted new position for {PlayerName}");
            }
        }
    }

    public SaveGameDataModel LoadPlayerPosition(string playerId)
    {
        string dbPath = Path.Combine(Application.persistentDataPath, DatabaseName);
        using (var db = new SQLiteConnection(dbPath))
        {
            return db.Table<SaveGameDataModel>().FirstOrDefault(d => d.PlayerName == playerId);
        }
    }
}
