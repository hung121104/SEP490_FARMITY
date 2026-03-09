using System;
using System.Collections.Generic;

/// <summary>
/// Raw deserialization model matching the /player-data/world?_id=... API response.
/// Used only by WorldDataBootstrapper — never stored long-term.
///
/// NOTE: The `chunks` list requires Newtonsoft.Json for deserialization because
/// Unity's JsonUtility does not support Dictionary<string,T>.
/// WorldDataBootstrapper uses JsonConvert.DeserializeObject<WorldApiResponse>().
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

    /// <summary>Loaded chunk documents — one per saved 30x30 chunk.</summary>
    public List<ChunkResponseData> chunks = new List<ChunkResponseData>();

    // ─────────────────────────────────────── Nested types

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

// ─────────────────────────────────────────────────────────────────── Chunk DTOs

/// <summary>
/// One loaded chunk document from MongoDB.
/// `tiles` is a key–value map: key = local tile index as a string ("0"–"899"),
/// value = TileResponseData.
/// localIndex = localX + localY * 30,  where localX/Y are offsets within the chunk.
/// </summary>
[Serializable]
public class ChunkResponseData
{
    public int chunkX;
    public int chunkY;
    public int sectionId;

    /// <summary>
    /// Key = string representation of local tile index ("0"–"899").
    /// Requires Newtonsoft.Json for deserialization — JsonUtility cannot handle this.
    /// </summary>
    public Dictionary<string, TileResponseData> tiles
        = new Dictionary<string, TileResponseData>();
}

/// <summary>
/// Mirrors the TileData sub-document in chunk.schema.ts.
/// Only non-empty / non-default fields are guaranteed to be present.
/// </summary>
[Serializable]
public class TileResponseData
{
    /// <summary>"crop", "tilled", "empty", etc.</summary>
    public string type;

    /// <summary>Plant identifier string, e.g. "plant_corn". Null for tilled-only tiles.</summary>
    public string plantId;

    public int  cropStage;
    public float growthTimer;
    public int  pollenHarvestCount;
    public bool isWatered;
    public bool isFertilized;
    public bool isPollinated;
}
