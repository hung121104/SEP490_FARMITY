using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Handles data creation, registry management, and validation.
/// </summary>
public class DroppedItemService : IDroppedItemService
{
    // ── Storage ───────────────────────────────────────────────
    private readonly Dictionary<string, DroppedItemData> items = new Dictionary<string, DroppedItemData>();
    private readonly bool showDebugLogs;

    public DroppedItemService(bool showDebugLogs = true)
    {
        this.showDebugLogs = showDebugLogs;
    }

    // ── Data Creation ─────────────────────────────────────────

    public DroppedItemData CreateDroppedItemData(ItemModel item, Vector3 playerPosition, Vector2 dropOffset)
    {
        if (item == null)
        {
            Debug.LogWarning("[DroppedItemService] Cannot create data from null item.");
            return null;
        }

        float worldX = playerPosition.x + dropOffset.x;
        float worldY = playerPosition.y + dropOffset.y;

        // Build DroppedItemData from ItemModel using the factory method
        // quantity is taken directly from the ItemModel (drop the whole stack)
        DroppedItemData data = DroppedItemData.FromItemModel(item, worldX, worldY);

        // Fill chunk coordinates from WorldDataManager
        if (WorldDataManager.Instance != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(
                new Vector3(worldX, worldY, 0f));
            data.chunkX = chunkPos.x;
            data.chunkY = chunkPos.y;
        }

        // Fill room name from Photon
        if (PhotonNetwork.CurrentRoom != null)
        {
            data.roomName = PhotonNetwork.CurrentRoom.Name;
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemService] Created drop data: {data.itemName} x{data.quantity} at ({worldX:F1}, {worldY:F1})");

        return data;
    }

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
