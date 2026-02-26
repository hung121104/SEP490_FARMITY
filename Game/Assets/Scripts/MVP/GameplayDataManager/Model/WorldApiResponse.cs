using System;
using System.Collections.Generic;

/// <summary>
/// Raw deserialization model matching the /player-data/world?_id=... API response.
/// Used only by WorldDataBootstrapper — never stored long-term.
/// </summary>
[Serializable]
public class WorldApiResponse
{
    public string _id;
    public string worldName;
    public string ownerId;
    public int day;
    public int month;
    public int year;
    public int hour;
    public int minute;
    public int gold;
    public List<CharacterEntry> characters = new List<CharacterEntry>();

    [Serializable]
    public class CharacterEntry
    {
        public string _id;
        public string worldId;
        public string accountId;
        public float positionX;
        public float positionY;
        public int sectionIndex;
    }
}
