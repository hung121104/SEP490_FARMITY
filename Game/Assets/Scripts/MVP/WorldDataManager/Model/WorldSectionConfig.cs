using UnityEngine;

/// <summary>
/// Configuration for a world section with custom chunk ranges
/// </summary>
[System.Serializable]
public class WorldSectionConfig
{
    [Tooltip("Section ID (0-3)")]
    public int SectionId;
    
    [Tooltip("Section name")]
    public string SectionName = "Section";
    
    [Tooltip("Starting chunk X coordinate")]
    public int ChunkStartX;
    
    [Tooltip("Starting chunk Y coordinate")]
    public int ChunkStartY;
    
    [Tooltip("Number of chunks wide")]
    public int ChunksWidth;
    
    [Tooltip("Number of chunks tall")]
    public int ChunksHeight;
    
    [Tooltip("Is this section active?")]
    public bool IsActive = true;
    
    [Tooltip("Background color for visualization")]
    public Color DebugColor = Color.green;
    
    /// <summary>
    /// Check if a chunk position belongs to this section
    /// </summary>
    public bool ContainsChunk(Vector2Int chunkPos)
    {
        return chunkPos.x >= ChunkStartX && 
               chunkPos.x < ChunkStartX + ChunksWidth &&
               chunkPos.y >= ChunkStartY && 
               chunkPos.y < ChunkStartY + ChunksHeight;
    }
    
    /// <summary>
    /// Check if a world position belongs to this section
    /// </summary>
    public bool ContainsWorldPosition(Vector3 worldPos, int chunkSize)
    {
        int worldMinX = ChunkStartX * chunkSize;
        int worldMinY = ChunkStartY * chunkSize;
        int worldMaxX = (ChunkStartX + ChunksWidth) * chunkSize;
        int worldMaxY = (ChunkStartY + ChunksHeight) * chunkSize;
        
        return worldPos.x >= worldMinX && worldPos.x < worldMaxX &&
               worldPos.y >= worldMinY && worldPos.y < worldMaxY;
    }
    
    public int GetTotalChunks() => ChunksWidth * ChunksHeight;
    
    /// <summary>
    /// Get world space bounds (min and max positions in tiles)
    /// </summary>
    public (Vector2Int min, Vector2Int max) GetWorldBounds(int chunkSize)
    {
        Vector2Int min = new Vector2Int(
            ChunkStartX * chunkSize,
            ChunkStartY * chunkSize
        );
        Vector2Int max = new Vector2Int(
            (ChunkStartX + ChunksWidth) * chunkSize,
            (ChunkStartY + ChunksHeight) * chunkSize
        );
        return (min, max);
    }
}
