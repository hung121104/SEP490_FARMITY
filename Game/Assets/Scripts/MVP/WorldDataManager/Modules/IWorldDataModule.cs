using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base interface for all world data modules (crops, inventory, structures, etc.)
/// </summary>
public interface IWorldDataModule
{
    /// <summary>
    /// Module name for debugging
    /// </summary>
    string ModuleName { get; }
    
    /// <summary>
    /// Initialize the module
    /// </summary>
    void Initialize(WorldDataManager manager);
    
    /// <summary>
    /// Clear all data in this module
    /// </summary>
    void ClearAll();
    
    /// <summary>
    /// Get memory usage estimate in MB
    /// </summary>
    float GetMemoryUsageMB();
    
    /// <summary>
    /// Get module-specific statistics
    /// </summary>
    Dictionary<string, object> GetStats();
}
