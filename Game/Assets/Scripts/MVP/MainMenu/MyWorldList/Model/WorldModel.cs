using System;

[Serializable]
public class WorldModel
{
    public string _id;
    public string worldName;  // Changed from 'name' to match API response
    public int day;
    public int month;
    public int year;
    public int hour;
    public int minute;
    public int gold;
    public string ownerId;

    // Constructor with default values
    public WorldModel()
    {
        day = 0;
        month = 0;
        year = 0;
        hour = 0;
        minute = 0;
        gold = 0;
    }
}

[Serializable]
public class WorldListResponse
{
    public WorldModel[] worlds;
}
