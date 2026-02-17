# World Data Manager - Modular Architecture

## 📁 File Structure

```
Assets/Scripts/MVP/WorldDataManager/
├── WorldDataManager.cs          # Core manager - coordinates all modules
├── Model/
│   ├── BaseChunkData.cs         # Base class for all chunk data types
│   ├── WorldSectionConfig.cs    # Section configuration class
│   ├── CropChunkData.cs         # Crop-specific chunk data
│   ├── InventoryChunkData.cs    # [TEMPLATE] Inventory chunk data
│   └── StructureChunkData.cs    # [TEMPLATE] Structure chunk data
├── Modules/
│   ├── IWorldDataModule.cs      # Base interface for all modules
│   ├── CropDataModule.cs        # Crop data management (ACTIVE)
│   ├── InventoryDataModule.cs   # [TEMPLATE] Inventory management
│   └── StructureDataModule.cs   # [TEMPLATE] Structure management
└── Editor/
    └── (editor scripts)
```

## 🎯 Key Features

### ✅ Separation of Concerns
- **WorldDataManager**: Core utilities (coordinate conversion, section lookup)
- **Data Modules**: Handle specific data types (crops, inventory, structures)
- **Chunk Data**: Store actual data efficiently

### ✅ Easy to Extend
Add new data types by:
1. Create a new `XXXChunkData` class extending `BaseChunkData`
2. Create a new `XXXDataModule` implementing `IWorldDataModule`
3. Initialize it in `WorldDataManager.InitializeModules()`

### ✅ Backward Compatible
All existing crop-related methods still work:
```csharp
WorldDataManager.Instance.PlantCropAtWorldPosition(pos, cropID);
WorldDataManager.Instance.TillTileAtWorldPosition(pos);
```

### ✅ Direct Module Access
For advanced usage:
```csharp
WorldDataManager.Instance.CropData.PlantCropAtWorldPosition(pos, cropID);
// Future:
// WorldDataManager.Instance.InventoryData.StoreItemAtWorldPosition(pos, itemID);
// WorldDataManager.Instance.StructureData.PlaceStructureAtWorldPosition(pos, structID);
```

## 🚀 Usage Examples

### Using Crop Module (Active)
```csharp
// Till and plant
Vector3 position = new Vector3(10, 20, 0);
WorldDataManager.Instance.TillTileAtWorldPosition(position);
WorldDataManager.Instance.PlantCropAtWorldPosition(position, cropTypeID: 1);

// Check and get crop data
if (WorldDataManager.Instance.HasCropAtWorldPosition(position))
{
    if (WorldDataManager.Instance.TryGetCropAtWorldPosition(position, out var cropData))
    {
        Debug.Log($"Crop stage: {cropData.CropStage}");
    }
}

// Update crop stage
WorldDataManager.Instance.UpdateCropStage(position, newStage: 2);
```

### Using Statistics
```csharp
// Get overall stats
WorldDataStats stats = WorldDataManager.Instance.GetStats();
Debug.Log($"Total crops: {stats.TotalCrops}");
Debug.Log($"Memory: {stats.MemoryUsageMB:F2} MB");

// Log detailed stats
WorldDataManager.Instance.LogStats();
```

## 📦 Adding New Modules

### Step 1: Create Chunk Data Class
```csharp
// Extend BaseChunkData
public class MyDataChunkData : BaseChunkData
{
    // Your data structures
    public Dictionary<long, MyData> myData = new Dictionary<long, MyData>();
    
    // Implement abstract methods
    public override byte[] ToBytes() { /* serialize */ }
    public override void FromBytes(byte[] data) { /* deserialize */ }
    public override int GetDataSizeBytes() { /* size */ }
    public override void Clear() { /* clear */ }
}
```

### Step 2: Create Module Class
```csharp
// Implement IWorldDataModule
public class MyDataModule : IWorldDataModule
{
    public string ModuleName => "My Data";
    private WorldDataManager manager;
    private Dictionary<int, Dictionary<Vector2Int, MyDataChunkData>> sections;
    
    public void Initialize(WorldDataManager manager) { /* init */ }
    public void ClearAll() { /* clear */ }
    public float GetMemoryUsageMB() { /* memory */ }
    public Dictionary<string, object> GetStats() { /* stats */ }
    
    // Your custom methods
    public bool DoSomethingAtWorldPosition(Vector3 worldPos) { /* logic */ }
}
```

### Step 3: Register in WorldDataManager
Edit `WorldDataManager.InitializeModules()`:
```csharp
private void InitializeModules()
{
    // Crop Module (existing)
    cropModule = new CropDataModule();
    cropModule.Initialize(this);
    modules[cropModule.ModuleName] = cropModule;
    
    // Add your module
    myDataModule = new MyDataModule();
    myDataModule.Initialize(this);
    modules[myDataModule.ModuleName] = myDataModule;
}
```

### Step 4: Add Public Access
Add property to WorldDataManager:
```csharp
private MyDataModule myDataModule;
public MyDataModule MyData => myDataModule;
```

## 🔮 Example Templates Included

### Inventory Module (Template)
- Store items, containers, dropped loot
- Location: `Modules/InventoryDataModule.cs`
- Data: `Model/InventoryChunkData.cs`

### Structure Module (Template)
- Player-made buildings, fences, decorations
- Location: `Modules/StructureDataModule.cs`
- Data: `Model/StructureChunkData.cs`

**To activate templates:**
1. Uncomment initialization in `WorldDataManager.InitializeModules()`
2. Add public property for module access
3. Customize the data structures as needed

## 🛠️ Core Utilities (Available to All Modules)

```csharp
// Coordinate conversion
Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
Vector3 worldPos = manager.ChunkToWorldPosition(chunkPos);

// Section lookup
int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
WorldSectionConfig config = manager.GetSectionConfig(sectionId);
bool inSection = manager.IsPositionInActiveSection(worldPos);
```

## 📊 Benefits

| Feature | Before | After |
|---------|--------|-------|
| **File Size** | 800+ lines | ~200 lines core + modular files |
| **Extensibility** | Hard to add features | Implement interface + register |
| **Maintainability** | All in one file | Separated by concern |
| **Testability** | Hard to test parts | Each module testable |
| **Memory** | Fixed for crops only | Per-module tracking |
| **Compatibility** | N/A | 100% backward compatible |

## 🎨 Architecture Diagram

```
WorldDataManager (Core)
├── Coordinate Utilities
├── Section Management
└── Module Coordinator
    ├── CropDataModule (ACTIVE)
    │   └── CropChunkData
    ├── InventoryDataModule (Template)
    │   └── InventoryChunkData
    └── StructureDataModule (Template)
        └── StructureChunkData
```

## 💡 Best Practices

1. **Keep modules independent** - Don't reference other modules directly
2. **Use WorldDataManager utilities** - For coordinate conversion, section lookup
3. **Implement IWorldDataModule** - For consistency and statistics
4. **Extend BaseChunkData** - Inherit common functionality
5. **Log statistics** - Use GetStats() for debugging and monitoring

## 🔄 Migration from Old Code

**No changes needed!** All existing code continues to work:
- `PlantCropAtWorldPosition()` ✅
- `TillTileAtWorldPosition()` ✅
- `GetChunk()` ✅
- `GetSection()` ✅
- `GetStats()` ✅ (enhanced with module breakdown)

## 📝 Notes

- **Templates are examples** - Customize for your specific needs
- **Serialization included** - `ToBytes()`/`FromBytes()` for network sync
- **Memory efficient** - Only stores data where it exists
- **Scalable** - Add as many modules as needed

---

**Need help?** Check the example templates in `Modules/` and `Model/` folders.
