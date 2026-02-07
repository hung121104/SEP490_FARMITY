using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class CropChunkData
{
    public int ChunkX;
    public int ChunkY;
    public int SectionId;
    
    [Serializable]
    public struct TileData
    {
        public bool IsTilled;      // 1 byte - whether tile is tilled
        public bool HasCrop;       // 1 byte - whether tile has a crop
        public ushort CropTypeID;  // 2 bytes
        public byte CropStage;     // 1 byte
        public int WorldX;         // 4 bytes - ABSOLUTE WORLD X
        public int WorldY;         // 4 bytes - ABSOLUTE WORLD Y
        
        // Total: 13 bytes per tile (unified structure)
    }
    
    // Dictionary key is world position: WorldX * 100000 + WorldY
    // Single dictionary for both tilled tiles and crops - more memory efficient!
    [NonSerialized]
    public Dictionary<long, TileData> tiles = new Dictionary<long, TileData>();
    
    [NonSerialized]
    public bool IsLoaded = false;
    
    [NonSerialized]
    public bool IsDirty = false;
    
    [NonSerialized]
    public float LastSyncTime = 0f;
    
    private long GetKey(int worldX, int worldY)
    {
        // Combine X and Y into single key: supports -10000 to +10000 range
        return ((long)worldX * 100000L) + worldY;
    }
    
    /// <summary>
    /// Plant crop at ABSOLUTE world position - can only plant on tilled tiles!
    /// </summary>
    public bool PlantCrop(ushort cropTypeID, int worldX, int worldY)
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
            tile.CropTypeID = cropTypeID;
            tile.CropStage = 0;
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
            tile.CropTypeID = 0;
            tile.CropStage = 0;
            
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
    /// Serialize with absolute world positions - easy to debug!
    /// Format: [ChunkX(4)] [ChunkY(4)] [SectionId(4)] [Count(2)] [Tile1(13)] [Tile2(13)] ...
    /// </summary>
    public byte[] ToBytes()
    {
        int count = tiles.Count;
        byte[] data = new byte[14 + (count * 13)]; // 14 header + 13 bytes per tile
        
        BitConverter.GetBytes(ChunkX).CopyTo(data, 0);
        BitConverter.GetBytes(ChunkY).CopyTo(data, 4);
        BitConverter.GetBytes(SectionId).CopyTo(data, 8);
        BitConverter.GetBytes((ushort)count).CopyTo(data, 12);
        
        int offset = 14;
        foreach (var tile in tiles.Values)
        {
            data[offset] = (byte)(tile.IsTilled ? 1 : 0);                    // 1 byte
            data[offset + 1] = (byte)(tile.HasCrop ? 1 : 0);                 // 1 byte
            BitConverter.GetBytes(tile.CropTypeID).CopyTo(data, offset + 2); // 2 bytes
            data[offset + 4] = tile.CropStage;                                // 1 byte
            BitConverter.GetBytes(tile.WorldX).CopyTo(data, offset + 5);     // 4 bytes
            BitConverter.GetBytes(tile.WorldY).CopyTo(data, offset + 9);     // 4 bytes
            offset += 13;
        }
        
        return data;
    }
    
    public void FromBytes(byte[] data)
    {
        if (data == null || data.Length < 14)
        {
            Debug.LogError("Invalid chunk data: too short");
            return;
        }
        
        ChunkX = BitConverter.ToInt32(data, 0);
        ChunkY = BitConverter.ToInt32(data, 4);
        SectionId = BitConverter.ToInt32(data, 8);
        int count = BitConverter.ToUInt16(data, 12);
        
        tiles.Clear();
        
        int offset = 14;
        for (int i = 0; i < count && offset + 13 <= data.Length; i++)
        {
            TileData tile = new TileData
            {
                IsTilled = data[offset] == 1,
                HasCrop = data[offset + 1] == 1,
                CropTypeID = BitConverter.ToUInt16(data, offset + 2),
                CropStage = data[offset + 4],
                WorldX = BitConverter.ToInt32(data, offset + 5),
                WorldY = BitConverter.ToInt32(data, offset + 9)
            };
            
            long key = GetKey(tile.WorldX, tile.WorldY);
            tiles[key] = tile;
            offset += 13;
        }
        
        IsLoaded = true;
        IsDirty = false;
    }
    
    public int GetDataSizeBytes() => 14 + (tiles.Count * 13);
    
    public int GetCropCount()
    {
        int count = 0;
        foreach (var tile in tiles.Values)
        {
            if (tile.HasCrop) count++;
        }
        return count;
    }
    
    public void Clear()
    {
        tiles.Clear();
        IsLoaded = false;
        IsDirty = false;
    }
}
