using UnityEngine;

// Interfaces
public interface IToolSystem
{
    bool UseTool(ItemDataSO tool, Vector3 targetPosition);
}

public interface IFarmingSystem
{
    bool PlantSeed(ItemDataSO seed, Vector3 targetPosition);
}

public interface IConsumableSystem
{
    bool Consume(ItemDataSO consumable);
}

public interface IWeaponSystem
{
    bool UseWeapon(ItemDataSO weapon, Vector3 targetPosition);
}

// Mock implementations (replace with your real systems when available)
public class ToolSystem : MonoBehaviour, IToolSystem
{
    public bool UseTool(ItemDataSO tool, Vector3 targetPosition)
    {
        Debug.Log($"🔨 Using tool {tool.itemName} at {targetPosition}");
        return true; // Mock success
    }
}

public class FarmingSystem : MonoBehaviour, IFarmingSystem
{
    public bool PlantSeed(ItemDataSO seed, Vector3 targetPosition)
    {
        Debug.Log($"🌱 Planting seed {seed.itemName} at {targetPosition}");
        return true; // Mock success
    }
}

public class ConsumableSystem : MonoBehaviour, IConsumableSystem
{
    public bool Consume(ItemDataSO consumable)
    {
        Debug.Log($"🍎 Consuming {consumable.itemName}");
        return true; // Mock success
    }
}

public class WeaponSystem : MonoBehaviour, IWeaponSystem
{
    public bool UseWeapon(ItemDataSO weapon, Vector3 targetPosition)
    {
        Debug.Log($"⚔️ Using weapon {weapon.itemName} at {targetPosition}");
        return true; // Mock success
    }
}
