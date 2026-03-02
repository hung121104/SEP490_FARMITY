using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-memory registry of all dropped items known to this client.
/// Thread-safe dictionary keyed by dropId.
/// No networking or visual logic — purely data management.
/// </summary>
public class DroppedItemService : IDroppedItemService
{
    // ── Storage ───────────────────────────────────────────────
    private readonly Dictionary<string, DroppedItemData> items = new Dictionary<string, DroppedItemData>();

    // ── Mutation ──────────────────────────────────────────────

    public void RegisterItem(DroppedItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.dropId))
        {
            Debug.LogWarning("[DroppedItemService] Cannot register item with null or empty dropId.");
            return;
        }

        if (items.ContainsKey(item.dropId))
        {
            Debug.LogWarning($"[DroppedItemService] Item '{item.dropId}' already registered, overwriting.");
        }

        items[item.dropId] = item;
    }

    public bool UnregisterItem(string dropId)
    {
        if (string.IsNullOrEmpty(dropId))
        {
            Debug.LogWarning("[DroppedItemService] Cannot unregister item with null or empty dropId.");
            return false;
        }

        return items.Remove(dropId);
    }

    public void Clear()
    {
        items.Clear();
    }

    // ── Query ─────────────────────────────────────────────────

    public DroppedItemData GetItem(string dropId)
    {
        if (string.IsNullOrEmpty(dropId)) return null;
        items.TryGetValue(dropId, out DroppedItemData item);
        return item;
    }

    public List<DroppedItemData> GetAllItems()
    {
        return new List<DroppedItemData>(items.Values);
    }

    public List<DroppedItemData> GetItemsInChunk(Vector2Int chunkPos)
    {
        var result = new List<DroppedItemData>();
        foreach (var item in items.Values)
        {
            if (item.chunkX == chunkPos.x && item.chunkY == chunkPos.y)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public bool HasItem(string dropId)
    {
        if (string.IsNullOrEmpty(dropId)) return false;
        return items.ContainsKey(dropId);
    }

    public int Count => items.Count;
}
