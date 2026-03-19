using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Unified chunk data — stores BOTH crop tile data and structure tile data inside
/// one dictionary per chunk. Each entry (TileSlot) covers a single world position.
///
/// Mutual Exclusion Rules:
///   - A tile can have only one occupant kind: crop, structure, or resource.
///   - PlantCrop(), PlaceStructure(), and PlaceResource() enforce this automatically.
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
        public float  GrowthTimer;      // seconds accumulated toward next stage
        public byte   PollenHarvestCount; // pollen collections this flowering stage
        public bool   IsWatered;        // watered flag (affects growth speed)
        public float  WaterDecayTimer;  // in-game minutes accumulated toward water expiry
        public bool   IsFertilized;     // fertilizer applied (affects growth speed)
        public bool   IsPollinated;     // hybrid already applied, prevents double cross
    }

    // ── Structure sub-data ────────────────────────────────────────────────

    [Serializable]
    public struct StructureTileData
    {
        public string StructureId;  // structure identifier (e.g. "fence_wood")
        public int    PlacedDay;    // in-game day the structure was placed
        public int    CurrentHp;    // remaining hp for destruction logic
    }

    [Serializable]
    public struct ResourceTileData
    {
        public string ResourceId;  // resource identifier (e.g. "oak_tree")
        public int CurrentHp;      // remaining hp for harvesting logic
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

        // Resource slot
        public bool             HasResource;
        public ResourceTileData Resource;
    }

    // ── Storage ───────────────────────────────────────────────────────────

    /// <summary>Key = WorldX * 100000 + WorldY (via BaseChunkData.GetWorldKey)</summary>
    [NonSerialized]
    public Dictionary<long, TileSlot> tiles = new Dictionary<long, TileSlot>();

    /// <summary>
    /// World positions that were untilled (removed from tiles dict) since the last server save.
    /// WorldSaveManager sends these as type="empty" so the server clears the old tilled state.
    /// Cleared after each successful save.
    /// </summary>
    [NonSerialized]
    public HashSet<(int wx, int wy)> PendingUntilledPositions = new HashSet<(int, int)>();

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
        if (!slot.HasCrop && !slot.HasStructure && !slot.HasResource)
        {
            tiles.Remove(key);
            // Track this position so BuildPayload can send type="empty" to overwrite
            // the stale "tilled" record on the server.
            PendingUntilledPositions.Add((worldX, worldY));
        }
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
        slot.Crop    = new CropTileData { PlantId = plantId, IsWatered = slot.Crop.IsWatered, WaterDecayTimer = slot.Crop.WaterDecayTimer };
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

        if (!slot.IsTilled && !slot.HasStructure && !slot.HasResource)
        {
            tiles.Remove(key);
            PendingUntilledPositions.Add((worldX, worldY));
        }
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

    public bool UpdateGrowthTimer(int worldX, int worldY, float newTimer)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.GrowthTimer = newTimer;
        tiles[key]            = slot;
        IsDirty               = true;
        return true;
    }

    public bool AddGrowthTime(int worldX, int worldY, float deltaSeconds)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasCrop) return false;
        slot.Crop.GrowthTimer += deltaSeconds;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    // ── Watering ──────────────────────────────────────────────────────────

    public bool WaterTile(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.IsTilled) return false;
        slot.Crop.IsWatered       = true;
        slot.Crop.WaterDecayTimer = 0f;   // reset decay clock whenever freshly watered
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

    public bool AddWaterDecayTime(int worldX, int worldY, float deltaMinutes)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.Crop.IsWatered) return false;
        slot.Crop.WaterDecayTimer += deltaMinutes;
        tiles[key] = slot;
        IsDirty    = true;
        return true;
    }

    public bool SetWaterDecayTimer(int worldX, int worldY, float minutes)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot)) return false;
        slot.Crop.WaterDecayTimer = minutes;
        tiles[key] = slot;
        return true;
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
    /// Structures CANNOT be placed on tilled soil.
    /// </summary>
    public bool PlaceStructure(string structureId, int worldX, int worldY, int initialHp = 0)
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
            if (slot.HasResource)
            {
                Debug.LogWarning($"[UnifiedChunkData] Cannot place structure at ({worldX},{worldY}) — resource is there.");
                return false;
            }
            if (slot.IsTilled)
            {
                Debug.LogWarning($"[UnifiedChunkData] Cannot place structure at ({worldX},{worldY}) — tile is tilled.");
                return false;
            }
            slot.HasStructure = true;
            slot.Structure    = new StructureTileData { StructureId = structureId, CurrentHp = initialHp };
            tiles[key]        = slot;
        }
        else
        {
            tiles[key] = new TileSlot
            {
                WorldX       = worldX,
                WorldY       = worldY,
                HasStructure = true,
                Structure    = new StructureTileData { StructureId = structureId, CurrentHp = initialHp }
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
        if (!slot.IsTilled && !slot.HasCrop && !slot.HasResource)
        {
            tiles.Remove(key);
            PendingUntilledPositions.Add((worldX, worldY));
        }
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

    public bool UpdateStructureHp(int worldX, int worldY, int currentHp)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasStructure) return false;

        slot.Structure.CurrentHp = currentHp;
        tiles[key] = slot;
        IsDirty = true;
        return true;
    }

    public int GetStructureHp(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileSlot slot) && slot.HasStructure)
        {
            return slot.Structure.CurrentHp;
        }
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // RESOURCES
    // ══════════════════════════════════════════════════════════════════════

    public bool PlaceResource(string resourceId, int currentHp, int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);

        if (tiles.TryGetValue(key, out TileSlot slot))
        {
            if (slot.HasResource || slot.HasCrop || slot.HasStructure)
                return false;

            slot.HasResource = true;
            slot.Resource = new ResourceTileData
            {
                ResourceId = resourceId,
                CurrentHp = currentHp,
            };
            tiles[key] = slot;
        }
        else
        {
            tiles[key] = new TileSlot
            {
                WorldX = worldX,
                WorldY = worldY,
                HasResource = true,
                Resource = new ResourceTileData
                {
                    ResourceId = resourceId,
                    CurrentHp = currentHp,
                },
            };
        }

        IsDirty = true;
        return true;
    }

    public bool RemoveResource(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasResource) return false;

        slot.HasResource = false;
        slot.Resource = default;

        if (!slot.IsTilled && !slot.HasCrop && !slot.HasStructure)
        {
            tiles.Remove(key);
            PendingUntilledPositions.Add((worldX, worldY));
        }
        else
            tiles[key] = slot;

        IsDirty = true;
        return true;
    }

    public bool HasResource(int worldX, int worldY)
    {
        long key = GetKey(worldX, worldY);
        return tiles.TryGetValue(key, out TileSlot slot) && slot.HasResource;
    }

    public bool TryGetResource(int worldX, int worldY, out ResourceTileData resource)
    {
        long key = GetKey(worldX, worldY);
        if (tiles.TryGetValue(key, out TileSlot slot) && slot.HasResource)
        {
            resource = slot.Resource;
            return true;
        }
        resource = default;
        return false;
    }

    public int GetResourceCount()
    {
        int count = 0;
        foreach (var slot in tiles.Values)
            if (slot.HasResource) count++;
        return count;
    }

    public bool UpdateResourceHp(int worldX, int worldY, int currentHp)
    {
        long key = GetKey(worldX, worldY);
        if (!tiles.TryGetValue(key, out TileSlot slot) || !slot.HasResource) return false;

        slot.Resource.CurrentHp = currentHp;
        tiles[key] = slot;
        IsDirty = true;
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SERIALIZATION  (BaseChunkData abstract implementation)
    // Format:
    //   Header: ChunkX(4) ChunkY(4) SectionId(4) Count(2)
    //   Per slot: WorldX(4) WorldY(4) flags(1)
    //             [if HasCrop]       PlantIdLen(1) PlantId(N) CropStage(1) GrowthTimer(4)
    //                                PollenCount(1) IsWatered(1) IsFertilized(1) IsPollinated(1)
    //             [if HasStructure]  StructIdLen(1) StructId(N) PlacedDay(4)
    //             [if HasResource]   ResourceIdLen(1) ResourceId(N) CurrentHp(4)
    // flags byte: bit0=IsTilled, bit1=HasCrop, bit2=HasStructure, bit3=HasResource
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
            if (slot.HasResource)   flags |= 8;
            bytes.Add(flags);

            if (slot.HasCrop)
            {
                byte[] plantIdBytes = string.IsNullOrEmpty(slot.Crop.PlantId)
                    ? Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(slot.Crop.PlantId);
                bytes.Add((byte)plantIdBytes.Length);
                bytes.AddRange(plantIdBytes);
                bytes.Add(slot.Crop.CropStage);
                bytes.AddRange(BitConverter.GetBytes(slot.Crop.GrowthTimer));
                bytes.Add(slot.Crop.PollenHarvestCount);
                bytes.Add((byte)(slot.Crop.IsWatered    ? 1 : 0));
                bytes.AddRange(BitConverter.GetBytes(slot.Crop.WaterDecayTimer));
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

            if (slot.HasResource)
            {
                byte[] resourceIdBytes = string.IsNullOrEmpty(slot.Resource.ResourceId)
                    ? Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(slot.Resource.ResourceId);
                bytes.Add((byte)resourceIdBytes.Length);
                bytes.AddRange(resourceIdBytes);
                bytes.AddRange(BitConverter.GetBytes(slot.Resource.CurrentHp));
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
            slot.HasResource  = (flags & 8) != 0;

            if (slot.HasCrop)
            {
                int plantIdLen = data[offset++];
                slot.Crop.PlantId = plantIdLen > 0
                    ? System.Text.Encoding.UTF8.GetString(data, offset, plantIdLen)
                    : string.Empty;
                offset += plantIdLen;

                slot.Crop.CropStage          = data[offset++];
                slot.Crop.GrowthTimer        = BitConverter.ToSingle(data, offset); offset += 4;
                slot.Crop.PollenHarvestCount = offset < data.Length ? data[offset++] : (byte)0;
                slot.Crop.IsWatered          = offset < data.Length && data[offset++] == 1;
                slot.Crop.WaterDecayTimer    = offset + 4 <= data.Length ? BitConverter.ToSingle(data, offset) : 0f; offset += 4;
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

            if (slot.HasResource)
            {
                int resourceIdLen = offset < data.Length ? data[offset++] : 0;
                slot.Resource.ResourceId = resourceIdLen > 0
                    ? System.Text.Encoding.UTF8.GetString(data, offset, resourceIdLen)
                    : string.Empty;
                offset += resourceIdLen;
                slot.Resource.CurrentHp = offset + 4 <= data.Length
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
                // PlantIdLen(1) + PlantId(N) + CropStage(1) + GrowthTimer(4)
                // + PollenCount(1) + IsWatered(1) + WaterDecayTimer(4) + IsFertilized(1) + IsPollinated(1)
                size += 1 + plantIdLen + 1 + 4 + 1 + 1 + 4 + 1 + 1;
            }
            if (slot.HasStructure)
            {
                int structIdLen = string.IsNullOrEmpty(slot.Structure.StructureId)
                    ? 0 : System.Text.Encoding.UTF8.GetByteCount(slot.Structure.StructureId);
                size += 1 + structIdLen + 4;
            }
            if (slot.HasResource)
            {
                int resourceIdLen = string.IsNullOrEmpty(slot.Resource.ResourceId)
                    ? 0 : System.Text.Encoding.UTF8.GetByteCount(slot.Resource.ResourceId);
                size += 1 + resourceIdLen + 4;
            }
        }
        return size;
    }

    public override void Clear()
    {
        tiles.Clear();
        PendingUntilledPositions.Clear();
        IsLoaded = false;
        IsDirty  = false;
    }
}
