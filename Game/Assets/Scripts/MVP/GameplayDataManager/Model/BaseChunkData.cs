using System;
using UnityEngine;

/// <summary>
/// Base class for all chunk data types
/// Provides common functionality for chunk-based data storage
/// </summary>
[Serializable]
public abstract class BaseChunkData
{
    public int ChunkX;
    public int ChunkY;
    public int SectionId;
    
    [NonSerialized]
    public bool IsLoaded = false;
    
    [NonSerialized]
    public bool IsDirty = false;
    
    [NonSerialized]
    public float LastSyncTime = 0f;
    
    /// <summary>
    /// Serialize chunk data to bytes for network/storage
    /// </summary>
    public abstract byte[] ToBytes();
    
    /// <summary>
    /// Deserialize chunk data from bytes
    /// </summary>
    public abstract void FromBytes(byte[] data);
    
    /// <summary>
    /// Get estimated data size in bytes
    /// </summary>
    public abstract int GetDataSizeBytes();
    
    /// <summary>
    /// Clear all data in this chunk
    /// </summary>
    public abstract void Clear();
    
    /// <summary>
    /// Generate a unique key from world coordinates
    /// Supports range of -10000 to +10000 for both X and Y
    /// </summary>
    protected long GetWorldKey(int worldX, int worldY)
    {
        return ((long)worldX * 100000L) + worldY;
    }
}
