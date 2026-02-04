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
    public struct CompactCrop
    {
        public ushort CropTypeID;  // 2 bytes
        public byte CropStage;     // 1 byte
        public int WorldX;         // 4 bytes - ABSOLUTE WORLD X
        public int WorldY;         // 4 bytes - ABSOLUTE WORLD Y
        
        // Total: 11 bytes per crop (slightly larger but MUCH simpler)
    }
    
    // Dictionary key is world position: WorldX * 100000 + WorldY
    [NonSerialized]
    public Dictionary<long, CompactCrop> plantedCrops = new Dictionary<long, CompactCrop>();
    
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
    /// Plant crop at ABSOLUTE world position - super simple!
    /// </summary>
    public bool PlantCrop(ushort cropTypeID, int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        
        if (plantedCrops.ContainsKey(key))
        {
            Debug.LogWarning($"Crop already exists at world pos ({worldX}, {worldY})");
            return false;
        }
        
        plantedCrops[key] = new CompactCrop
        {
            CropTypeID = cropTypeID,
            CropStage = 0,
            WorldX = worldX,
            WorldY = worldY
        };
        
        IsDirty = true;
        return true;
    }
    
    public bool RemoveCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (plantedCrops.Remove(key))
        {
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool UpdateCropStage(int worldX, int worldY, byte newStage)
    {
        long key = GetKey(worldX, worldY);
        if (plantedCrops.TryGetValue(key, out CompactCrop crop))
        {
            crop.CropStage = newStage;      
            plantedCrops[key] = crop;
            IsDirty = true;
            return true;
        }
        return false;
    }
    
    public bool HasCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return plantedCrops.ContainsKey(key);
    }
    
    public bool TryGetCrop(int worldX, int worldY, out CompactCrop crop)
    {
        long key = GetKey(worldX, worldY);
        return plantedCrops.TryGetValue(key, out crop);
    }
    
    public List<CompactCrop> GetAllCrops()
    {
        return new List<CompactCrop>(plantedCrops.Values);
    }
    
    /// <summary>
    /// Serialize with absolute world positions - easy to debug!
    /// Format: [ChunkX(4)] [ChunkY(4)] [SectionId(4)] [Count(2)] [Crop1(11)] [Crop2(11)] ...
    /// </summary>
    public byte[] ToBytes()
    {
        int count = plantedCrops.Count;
        byte[] data = new byte[14 + (count * 11)]; // 14 header + 11 bytes per crop
        
        BitConverter.GetBytes(ChunkX).CopyTo(data, 0);
        BitConverter.GetBytes(ChunkY).CopyTo(data, 4);
        BitConverter.GetBytes(SectionId).CopyTo(data, 8);
        BitConverter.GetBytes((ushort)count).CopyTo(data, 12);
        
        int offset = 14;
        foreach (var crop in plantedCrops.Values)
        {
            BitConverter.GetBytes(crop.CropTypeID).CopyTo(data, offset);     // 2 bytes
            data[offset + 2] = crop.CropStage;                                // 1 byte
            BitConverter.GetBytes(crop.WorldX).CopyTo(data, offset + 3);     // 4 bytes
            BitConverter.GetBytes(crop.WorldY).CopyTo(data, offset + 7);     // 4 bytes
            offset += 11;
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
        
        plantedCrops.Clear();
        
        int offset = 14;
        for (int i = 0; i < count && offset + 11 <= data.Length; i++)
        {
            CompactCrop crop = new CompactCrop
            {
                CropTypeID = BitConverter.ToUInt16(data, offset),
                CropStage = data[offset + 2],
                WorldX = BitConverter.ToInt32(data, offset + 3),
                WorldY = BitConverter.ToInt32(data, offset + 7)
            };
            
            long key = GetKey(crop.WorldX, crop.WorldY);
            plantedCrops[key] = crop;
            offset += 11;
        }
        
        IsLoaded = true;
        IsDirty = false;
    }
    
    public int GetDataSizeBytes() => 14 + (plantedCrops.Count * 11);
    public int GetCropCount() => plantedCrops.Count;
    
    public void Clear()
    {
        plantedCrops.Clear();
        IsLoaded = false;
        IsDirty = false;
    }
}