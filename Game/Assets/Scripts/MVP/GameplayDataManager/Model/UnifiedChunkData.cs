using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Unified chunk data — stores BOTH crop tile data and structure tile data inside
/// one dictionary per chunk. Each entry (TileSlot) covers a single world position.
///
/// Mutual Exclusion Rules:
///   - A tile with HasCrop = true cannot have HasStructure = true and vice versa.
///   - PlantCrop() and PlaceStructure() both enforce this automatically.
///   - IsTilled is a ground-state flag shared by both (a tile must be tilled before planting,
///     but structures can be placed on any non-occupied tile regardless of tilled state).
/// </summary>
[Serializable]
public class UnifiedChunkData : BaseChunkData
{
    // ── Crop sub-data ─────────────────────────────────────────────────────

    [Serializable]
    public struct CropTileData
    {
        public string PlantId;          // plant identifier (from PlantDataSO.PlantId)
        public byte   CropStage;        // current growth stage index
        public int    TotalAge;         // days since planting
        public byte   PollenHarvestCount; // pollen collections this flowering stage
        public bool   IsWatered;        // watered this day
        public bool   IsFertilized;     // fertilizer applied
        public bool   IsPollinated;     // hybrid already applied, prevents double cross
    }

    // ── Structure sub-data ────────────────────────────────────────────────

    [Serializable]
    public struct StructureTileData
    {
        public string StructureId;  // structure identifier (e.g. "fence_wood")
        public int    PlacedDay;    // in-game day the structure was placed
    }

    // ── Unified tile slot — one per world position ────────────────────────

    [Serializable]
    public struct TileSlot
    {
        public int  WorldX;
        public int  WorldY;

        // Ground state
        public bool IsTilled;

        // Crop slot
        public bool         HasCrop;
        public CropTileData Crop;

        // Structure slot
        public bool              HasStructure;
        public StructureTileData Structure;
    }

    // ── Storage ───────────────────────────────────────────────────────────

    /// <summary>Key = WorldX * 100000 + WorldY (via BaseChunkData.GetWorldKey)</summary>
    [NonSerialized]
    public Dictionary<long, TileSlot> tiles = new Dictionary<long, TileSlot>();

    private long GetKey(int worldX, int worldY) => GetWorldKey(worldX, worldY);

    // ══════════════════════════════════════════════════════════════════════
    // TILLING
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Mark a tile as tilled. Creates the slot if it doesn't exist.</summary>
    public bool TillTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileSlot slot))
        {
            if (slot.IsTilled)
            {
                Debug.LogWarning($"[UnifiedChunkData] Tile already tilled at ({worldX},{worldY})");
                return false;
            }
            slot.IsTilled = true;
            tiles[key] = slot;
        }
        else
        {
            tiles[key] = new TileSlot { WorldX = worldX, WorldY = worldY, IsTilled = true };
        }
        IsDirty = true;
        return true;
    }

    public bool UntillTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.IsTilled) return false;
        slot.IsTilled = false;
        if (!slot.HasCrop && !slot.HasStructure)
            tiles.Remove(key);
        else
            tiles[key] = slot;
        IsDirty = true;
        return true;
    }

    public bool IsTilled(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.IsTilled;
    }

    public List<Vector2Int> GetAllTilledPositions()
    {
        var positions = new List<Vector2Int>();
        foreach (var slot in tiles.Values)
            if (slot.IsTilled) positions.Add(new Vector2Int(slot.WorldX, slot.WorldY));
        return positions;
    }

    public int GetTilledCount()
    {
        int count = 0;
        foreach (var slot in tiles.Values)
            if (slot.IsTilled) count++;
        return count;
    }

    // ══════════════════════════════════════════════════════════════════════
    // CROPS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Plant a crop. Tile must be tilled and must NOT already have a crop or structure.
    /// </summary>
    public bool PlantCrop(string plantId, int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);

        if (!tiles.TryGetValue(key, out TileSlot slot))
        {
            Debug.LogWarning($"[UnifiedChunkData] Cannot plant at ({worldX},{worldY}) — tile must be tilled first.");
            return false;
        }
        if (!slot.IsTilled)
        {
            Debug.LogWarning($"[UnifiedChunkData] Cannot plant at ({worldX},{worldY}) — tile is not tilled.");
            return false;
        }
        if (slot.HasCrop)
        {
            Debug.LogWarning($"[UnifiedChunkData] Crop already exists at ({worldX},{worldY}).");
            return false;
        }
        if (slot.HasStructure)
        {
            Debug.LogWarning($"[UnifiedChunkData] Cannot plant at ({worldX},{worldY}) — structure is already there.");
            return false;
        }

        slot.HasCrop = true;
        slot.Crop    = new CropTileData { PlantId = plantId };
        tiles[key]   = slot;
        IsDirty      = true;
        return true;
    }

    public bool RemoveCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;

        slot.HasCrop = false;
        slot.Crop    = default;

        if (!slot.IsTilled && !slot.HasStructure)
            tiles.Remove(key);
        else
            tiles[key] = slot;

        IsDirty = true;
        return true;
    }

    public bool HasCrop(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.HasCrop;
    }

    public bool TryGetCrop(int worldX, int worldY, out CropTileData crop)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileSlot slot) && slot.HasCrop)
        {
            crop = slot.Crop;
            return true;
        }
        crop = default;
        return false;
    }

    /// <summary>Returns TileSlots that have a crop, carrying WorldX/WorldY for caller convenience.</summary>
    public List<TileSlot> GetAllCrops()
    {
        var list = new List<TileSlot>();
        foreach (var slot in tiles.Values)
            if (slot.HasCrop) list.Add(slot);
        return list;
    }

    /// <summary>Returns all TileSlots (tilled, with crop, or with structure).</summary>
    public List<TileSlot> GetAllTiles()
    {
        return new List<TileSlot>(tiles.Values);
    }

    public int GetCropCount()
    {
        int count = 0;
        foreach (var slot in tiles.Values)
            if (slot.HasCrop) count++;
        return count;
    }

    // ── Crop mutation helpers ─────────────────────────────────────────────

    public bool UpdateCropStage(int worldX, int worldY, byte newStage)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.CropStage          = newStage;
        slot.Crop.PollenHarvestCount = 0; // reset on stage change
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool UpdateCropAge(int worldX, int worldY, int newAge)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.TotalAge = newAge;
        tiles[key]         = slot;
        IsDirty            = true;
        return true;
    }

    public bool IncrementCropAge(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.TotalAge++;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    // ── Watering ──────────────────────────────────────────────────────────

    public bool WaterTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.IsTilled) return false;
        slot.Crop.IsWatered = true;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool UnwaterTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot)) return false;
        slot.Crop.IsWatered = false;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool IsWateredAt(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.Crop.IsWatered;
    }

    // ── Fertilizer ────────────────────────────────────────────────────────

    public bool FertilizeTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.IsTilled) return false;
        slot.Crop.IsFertilized = true;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool UnfertilizeTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot)) return false;
        slot.Crop.IsFertilized = false;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool IsFertilizedAt(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.Crop.IsFertilized;
    }

    // ── Pollination ───────────────────────────────────────────────────────

    public bool SetPollinated(int worldX, int worldY, bool value)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot)) return false;
        slot.Crop.IsPollinated = value;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool IsPollinatedAt(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.Crop.IsPollinated;
    }

    public bool IncrementPollenHarvestCount(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.PollenHarvestCount++;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool ResetPollenHarvestCount(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.PollenHarvestCount = 0;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    /// <summary>
    /// Morphs an existing crop's PlantId to a hybrid (crossbreeding).
    /// Resets CropStage to startStage and marks IsPollinated.
    /// </summary>
    public bool SetCropPlantId(int worldX, int worldY, string newPlantId, byte startStage)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.PlantId      = newPlantId;
        slot.Crop.CropStage    = startStage;
        slot.Crop.IsPollinated = true;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // STRUCTURES
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Place a structure at this world position.
    /// Fails if a crop is already present (mutual exclusion).
    /// Structures do NOT require a tilled tile.
    /// </summary>
    public bool PlaceStructure(string structureId, int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);

        if (tiles.TryGetValue(key, out TileSlot slot))
        {
            if (slot.HasStructure)
            {
                Debug.LogWarning($"[UnifiedChunkData] Structure already exists at ({worldX},{worldY}).");
                return false;
            }
            if (slot.HasCrop)
            {
                Debug.LogWarning($"[UnifiedChunkData] Cannot place structure at ({worldX},{worldY}) — crop is there.");
                return false;
            }
            slot.HasStructure = true;
            slot.Structure    = new StructureTileData { StructureId = structureId };
            tiles[key]        = slot;
        }
        else
        {
            tiles[key] = new TileSlot
            {
                WorldX       = worldX,
                WorldY       = worldY,
                HasStructure = true,
                Structure    = new StructureTileData { StructureId = structureId }
            };
        }

        IsDirty = true;
        return true;
    }

    public bool RemoveStructure(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasStructure) return false;
        slot.HasStructure = false;
        slot.Structure    = default;
        if (!slot.IsTilled && !slot.HasCrop)
            tiles.Remove(key);
        else
            tiles[key] = slot;
        IsDirty = true;
        return true;
    }

    public bool HasStructure(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.HasStructure;
    }

    public bool TryGetStructure(int worldX, int worldY, out StructureTileData structure)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileSlot slot) && slot.HasStructure)
        {
            structure = slot.Structure;
            return true;
        }
        structure = default;
        return false;
    }

    public List<TileSlot> GetAllStructures()
    {
        var list = new List<TileSlot>();
        foreach (var slot in tiles.Values)
            if (slot.HasStructure) list.Add(slot);
        return list;
    }

    public int GetStructureCount()
    {
        int count = 0;
        foreach (var slot in tiles.Values)
            if (slot.HasStructure) count++;
        return count;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SERIALIZATION  (BaseChunkData abstract implementation)
    // Format:
    //   Header: ChunkX(4) ChunkY(4) SectionId(4) Count(2)
    //   Per slot: WorldX(4) WorldY(4) flags(1)
    //             [if HasCrop]       PlantIdLen(1) PlantId(N) CropStage(1) TotalAge(4)
    //                                PollenCount(1) IsWatered(1) IsFertilized(1) IsPollinated(1)
    //             [if HasStructure]  StructIdLen(1) StructId(N) PlacedDay(4)
    // flags byte: bit0=IsTilled, bit1=HasCrop, bit2=HasStructure
    // ══════════════════════════════════════════════════════════════════════

    public override byte[] ToBytes()
    {
        var bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(ChunkX));
        bytes.AddRange(BitConverter.GetBytes(ChunkY));
        bytes.AddRange(BitConverter.GetBytes(SectionId));
        bytes.AddRange(BitConverter.GetBytes((ushort)tiles.Count));

        foreach (var slot in tiles.Values)
        {
            bytes.AddRange(BitConverter.GetBytes(slot.WorldX));
            bytes.AddRange(BitConverter.GetBytes(slot.WorldY));

            byte flags = 0;
            if (slot.IsTilled)      flags |= 1;
            if (slot.HasCrop)       flags |= 2;
            if (slot.HasStructure)  flags |= 4;
            bytes.Add(flags);

            if (slot.HasCrop)
            {
                byte[] plantIdBytes = string.IsNullOrEmpty(slot.Crop.PlantId)
                    ? Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(slot.Crop.PlantId);
                bytes.Add((byte)plantIdBytes.Length);
                bytes.AddRange(plantIdBytes);
                bytes.Add(slot.Crop.CropStage);
                bytes.AddRange(BitConverter.GetBytes(slot.Crop.TotalAge));
                bytes.Add(slot.Crop.PollenHarvestCount);
                bytes.Add((byte)(slot.Crop.IsWatered    ? 1 : 0));
                bytes.Add((byte)(slot.Crop.IsFertilized ? 1 : 0));
                bytes.Add((byte)(slot.Crop.IsPollinated ? 1 : 0));
            }

            if (slot.HasStructure)
            {
                byte[] structIdBytes = string.IsNullOrEmpty(slot.Structure.StructureId)
                    ? Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(slot.Structure.StructureId);
                bytes.Add((byte)structIdBytes.Length);
                bytes.AddRange(structIdBytes);
                bytes.AddRange(BitConverter.GetBytes(slot.Structure.PlacedDay));
            }
        }

        return bytes.ToArray();
    }

    public override void FromBytes(byte[] data)
    {
        if (data == null || data.Length < 14)
        {
            Debug.LogError("[UnifiedChunkData] Invalid data: too short.");
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
            TileSlot slot = new TileSlot();
            slot.WorldX = BitConverter.ToInt32(data, offset); offset += 4;
            slot.WorldY = BitConverter.ToInt32(data, offset); offset += 4;

            byte flags = data[offset++];
            slot.IsTilled     = (flags & 1) != 0;
            slot.HasCrop      = (flags & 2) != 0;
            slot.HasStructure = (flags & 4) != 0;

            if (slot.HasCrop)
            {
                int plantIdLen = data[offset++];
                slot.Crop.PlantId = plantIdLen > 0
                    ? System.Text.Encoding.UTF8.GetString(data, offset, plantIdLen)
                    : string.Empty;
                offset += plantIdLen;

                slot.Crop.CropStage          = data[offset++];
                slot.Crop.TotalAge           = BitConverter.ToInt32(data, offset); offset += 4;
                slot.Crop.PollenHarvestCount = offset < data.Length ? data[offset++] : (byte)0;
                slot.Crop.IsWatered          = offset < data.Length && data[offset++] == 1;
                slot.Crop.IsFertilized       = offset < data.Length && data[offset++] == 1;
                slot.Crop.IsPollinated       = offset < data.Length && data[offset++] == 1;
            }

            if (slot.HasStructure)
            {
                int structIdLen = offset < data.Length ? data[offset++] : 0;
                slot.Structure.StructureId = structIdLen > 0
                    ? System.Text.Encoding.UTF8.GetString(data, offset, structIdLen)
                    : string.Empty;
                offset += structIdLen;
                slot.Structure.PlacedDay = offset + 4 <= data.Length
                    ? BitConverter.ToInt32(data, offset)
                    : 0;
                offset += 4;
            }

            tiles[GetKey(slot.WorldX, slot.WorldY)] = slot;
        }

        IsLoaded = true;
        IsDirty  = false;
    }

    public override int GetDataSizeBytes()
    {
        int size = 14; // header
        foreach (var slot in tiles.Values)
        {
            size += 4 + 4 + 1; // WorldX + WorldY + flags
            if (slot.HasCrop)
            {
                int plantIdLen = string.IsNullOrEmpty(slot.Crop.PlantId)
                    ? 0 : System.Text.Encoding.UTF8.GetByteCount(slot.Crop.PlantId);
                size += 1 + plantIdLen + 1 + 4 + 1 + 1 + 1 + 1;
            }
            if (slot.HasStructure)
            {
                int structIdLen = string.IsNullOrEmpty(slot.Structure.StructureId)
                    ? 0 : System.Text.Encoding.UTF8.GetByteCount(slot.Structure.StructureId);
                size += 1 + structIdLen + 4;
            }
        }
        return size;
    }

    public override void Clear()
    {
        tiles.Clear();
        IsLoaded = false;
        IsDirty  = false;
    }
}
