using SQLite;
using UnityEngine;

public class SaveGameDataModel
{
    [PrimaryKey,AutoIncrement]
    public int Id { get; set; }
    [Unique]
    public string PlayerName { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
}
