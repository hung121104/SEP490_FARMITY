using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class CropChunkData : BaseChunkData
{
    
    [Serializable]
    public struct TileData
    {
        public bool IsTilled;           // 1 byte - whether tile is tilled
        public bool HasCrop;            // 1 byte - whether tile has a crop
        public string PlantId;          // variable - plant identifier string (from PlantDataSO.PlantId)
        public byte CropStage;          // 1 byte
        public int TotalAge;            // 4 bytes - days since planting
        public int WorldX;              // 4 bytes - ABSOLUTE WORLD X
        public int WorldY;              // 4 bytes - ABSOLUTE WORLD Y
        public byte PollenHarvestCount; // 1 byte - pollen collections this flowering stage (resets on stage change)
    }
    
    // Dictionary key is world position: WorldX * 100000 + WorldY
    // Single dictionary for both tilled tiles and crops - more memory efficient!
    [NonSerialized]
    public Dictionary<long, TileData> tiles = new Dictionary<long, TileData>();
    
    private long GetKey(int worldX, int worldY)
    {
        // Use base class method for consistency
        return GetWorldKey(worldX, worldY);
    }
    
    /// <summary>
    /// Plant crop at ABSOLUTE world position - can only plant on tilled tiles!
    /// </summary>
    public bool PlantCrop(string plantId, int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        
        if (tiles.TryGetValue(key, out TileData tile))
        {
            if (!tile.IsTilled)
            {
                Debug.LogWarning($"Cannot plant crop at ({worldX}, {worldY}) - tile is not tilled!");
                return false;
            }
            
            if (tile.HasCrop)
            {
                Debug.LogWarning($"Crop already exists at world pos ({worldX}, {worldY})");
                return false;
            }
            
            // Update existing tilled tile to add crop
            tile.HasCrop = true;
            tile.PlantId = plantId;
            tile.CropStage = 0;
            tile.TotalAge = 0;  // Start at day 0
            tile.PollenHarvestCount = 0;
            tiles[key] = tile;
        }
        else
        {
            // Tile doesn't exist - must till first!
            Debug.LogWarning($"Cannot plant crop at ({worldX}, {worldY}) - tile must be tilled first!");
            return false;
        }
        
        IsDirty = true;
        return true;
    }
    
    public bool RemoveCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.HasCrop = false;
            tile.PlantId = string.Empty;
            tile.CropStage = 0;
            tile.TotalAge = 0;
            tile.PollenHarvestCount = 0;
            
            // If tile is no longer tilled either, remove it completely
            if (!tile.IsTilled)
            {
                tiles.Remove(key);
            }
            else
            {
                tiles[key] = tile;
            }
            
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool UpdateCropStage(int worldX, int worldY, byte newStage)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.CropStage = newStage;
            tile.PollenHarvestCount = 0; // Reset pollen counter whenever stage changes
            tiles[key] = tile;
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Increments the pollen harvest count for this tile by 1.
    /// Returns false if the tile does not exist or has no crop.
    /// </summary>
    public bool IncrementPollenHarvestCount(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.PollenHarvestCount++;
            tiles[key] = tile;
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>Resets the pollen harvest count for this tile to 0.</summary>
    public bool ResetPollenHarvestCount(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.PollenHarvestCount = 0;
            tiles[key] = tile;
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool UpdateCropAge(int worldX, int worldY, int newAge)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.TotalAge = newAge;
            tiles[key] = tile;
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool IncrementCropAge(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.HasCrop)
        {
            tile.TotalAge++;
            tiles[key] = tile;
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool HasCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileData tile) && tile.HasCrop;
    }
    
    public bool TryGetCrop(int worldX, int worldY, out TileData crop)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out crop) && crop.HasCrop)
        {
            return true;
        }
        crop = default;
        return false;
    }
    
    public List<TileData> GetAllCrops()
    {
        List<TileData> crops = new List<TileData>();
        foreach (var tile in tiles.Values)
        {
            if (tile.HasCrop)
            {
                crops.Add(tile);
            }
        }
        return crops;
    }

    /// <summary>
    /// Get all tiles (tilled and/or with crops) stored in this chunk
    /// </summary>
    public List<TileData> GetAllTiles()
    {
        List<TileData> list = new List<TileData>();
        foreach (var tile in tiles.Values)
        {
            list.Add(tile);
        }
        return list;
    }
    
    /// <summary>
    /// Mark a tile as tilled at ABSOLUTE world position
    /// </summary>
    public bool TillTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        
        if (tiles.TryGetValue(key, out TileData tile))
        {
            if (tile.IsTilled)
            {
                Debug.LogWarning($"Tile already tilled at world pos ({worldX}, {worldY})");
                return false;
            }
            
            tile.IsTilled = true;
            tiles[key] = tile;
        }
        else
        {
            // Create new tilled tile without crop
            tiles[key] = new TileData
            {
                IsTilled = true,
                HasCrop = false,
                WorldX = worldX,
                WorldY = worldY
            };
        }
        
        IsDirty = true;
        return true;
    }
    
    /// <summary>
    /// Remove tilled status from a tile
    /// </summary>
    public bool UntillTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileData tile) && tile.IsTilled)
        {
            tile.IsTilled = false;
            
            // If tile has no crop either, remove it completely
            if (!tile.HasCrop)
            {
                tiles.Remove(key);
            }
            else
            {
                tiles[key] = tile;
            }
            
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if a tile is tilled
    /// </summary>
    public bool IsTilled(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileData tile) && tile.IsTilled;
    }
    
    /// <summary>
    /// Get all tilled tile positions
    /// </summary>
    public List<Vector2Int> GetAllTilledPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var tile in tiles.Values)
        {
            if (tile.IsTilled)
            {
                positions.Add(new Vector2Int(tile.WorldX, tile.WorldY));
            }
        }
        return positions;
    }
    
    public int GetTilledCount()
    {
        int count = 0;
        foreach (var tile in tiles.Values)
        {
            if (tile.IsTilled) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Serialize with absolute world positions.
    /// Format: [ChunkX(4)] [ChunkY(4)] [SectionId(4)] [Count(2)] [Tile...]
    /// Per tile: IsTilled(1) + HasCrop(1) + PlantIdLen(1) + PlantId(N) + CropStage(1) + TotalAge(4) + WorldX(4) + WorldY(4)
    /// </summary>
    public override byte[] ToBytes()
    {
        var bytes = new System.Collections.Generic.List<byte>();
        bytes.AddRange(BitConverter.GetBytes(ChunkX));           // 4
        bytes.AddRange(BitConverter.GetBytes(ChunkY));           // 4
        bytes.AddRange(BitConverter.GetBytes(SectionId));        // 4
        bytes.AddRange(BitConverter.GetBytes((ushort)tiles.Count)); // 2

        foreach (var tile in tiles.Values)
        {
            bytes.Add((byte)(tile.IsTilled ? 1 : 0));            // 1
            bytes.Add((byte)(tile.HasCrop ? 1 : 0));             // 1
            byte[] plantIdBytes = string.IsNullOrEmpty(tile.PlantId)
                ? System.Array.Empty<byte>()
                : System.Text.Encoding.UTF8.GetBytes(tile.PlantId);
            bytes.Add((byte)plantIdBytes.Length);                // 1  (max 255)
            bytes.AddRange(plantIdBytes);                        // N
            bytes.Add(tile.CropStage);                                   // 1
            bytes.AddRange(BitConverter.GetBytes(tile.TotalAge));         // 4
            bytes.AddRange(BitConverter.GetBytes(tile.WorldX));           // 4
            bytes.AddRange(BitConverter.GetBytes(tile.WorldY));           // 4
            bytes.Add(tile.PollenHarvestCount);                           // 1 (new)
        }

        return bytes.ToArray();
    }
    
    public override void FromBytes(byte[] data)
    {
        if (data == null || data.Length < 14)
        {
            Debug.LogError("Invalid chunk data: too short");
            return;
        }

        ChunkX    = BitConverter.ToInt32(data, 0);
        ChunkY    = BitConverter.ToInt32(data, 4);
        SectionId = BitConverter.ToInt32(data, 8);
        int count = BitConverter.ToUInt16(data, 12);

        tiles.Clear();

        int offset = 14;
        for (int i = 0; i < count && offset < data.Length; i++)
        {
            TileData tile = new TileData
            {
                IsTilled = data[offset]     == 1,
                HasCrop  = data[offset + 1] == 1
            };
            offset += 2;

            int plantIdLen = data[offset++];
            tile.PlantId = plantIdLen > 0
                ? System.Text.Encoding.UTF8.GetString(data, offset, plantIdLen)
                : string.Empty;
            offset += plantIdLen;

            tile.CropStage = data[offset++];
            tile.TotalAge  = BitConverter.ToInt32(data, offset); offset += 4;
            tile.WorldX    = BitConverter.ToInt32(data, offset); offset += 4;
            tile.WorldY    = BitConverter.ToInt32(data, offset); offset += 4;
            // PollenHarvestCount added in v2 — graceful fallback for old save data
            tile.PollenHarvestCount = offset < data.Length ? data[offset++] : (byte)0;

            tiles[GetKey(tile.WorldX, tile.WorldY)] = tile;
        }

        IsLoaded = true;
        IsDirty  = false;
    }
    
    public override int GetDataSizeBytes()
    {
        int size = 14; // header
        foreach (var tile in tiles.Values)
        {
            int plantIdLen = string.IsNullOrEmpty(tile.PlantId)
                ? 0 : System.Text.Encoding.UTF8.GetByteCount(tile.PlantId);
            size += 2 + 1 + plantIdLen + 1 + 4 + 4 + 4 + 1; // IsTilled+HasCrop + PlantIdLen + PlantId + CropStage + TotalAge + WorldX + WorldY + PollenHarvestCount
        }
        return size;
    }
    
    public int GetCropCount()
    {
        int count = 0;
        foreach (var tile in tiles.Values)
        {
            if (tile.HasCrop) count++;
        }
        return count;
    }
    
    public override void Clear()
    {
        tiles.Clear();
        IsLoaded = false;
        IsDirty = false;
    }
}
