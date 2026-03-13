using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Handles client -> host resource hit requests and host-authoritative damage/loot flow.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class ResourceInteractionManager : MonoBehaviourPun
{
    [Header("Loot")]
    [Tooltip("Room-object prefab name under a Resources folder for PhotonNetwork.InstantiateRoomObject.")]
    [SerializeField] private string lootPrefabName = "LootPrefabName";

    [Header("FX")]
    [SerializeField] private string hitAnimatorTrigger = "Hit";

    public void RequestHitResource(int chunkX, int chunkY, int tileIndex, int damage, string toolId)
    {
        photonView.RPC(
            nameof(RPC_Host_ProcessHit),
            RpcTarget.MasterClient,
            chunkX,
            chunkY,
            tileIndex,
            damage,
            toolId ?? string.Empty);
    }

    [PunRPC]
    public void RPC_Host_ProcessHit(
        int chunkX,
        int chunkY,
        int tileIndex,
        int damage,
        string toolId,
        PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (damage <= 0) return;

        if (!TryGetChunk(chunkX, chunkY, out UnifiedChunkData chunk, out int sectionId))
            return;

        Vector2Int worldTile = TileIndexToWorldTile(chunkX, chunkY, tileIndex);

        if (!chunk.TryGetResource(worldTile.x, worldTile.y, out UnifiedChunkData.ResourceTileData resourceTile))
            return;

        ResourceConfigData config = ResourceCatalogManager.Instance?.GetResourceConfig(resourceTile.ResourceId);
        if (config == null)
            return;

        // Optional/mock tool validation.
        if (!string.IsNullOrEmpty(config.requiredToolId) &&
            !string.Equals(config.requiredToolId, toolId, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log(
                $"[ResourceInteraction] Reject hit from actor {info.Sender.ActorNumber}: " +
                $"tool '{toolId}' does not satisfy required '{config.requiredToolId}' for {resourceTile.ResourceId}.");
            return;
        }

        int newHp = Mathf.Max(0, resourceTile.CurrentHp - damage);

        if (newHp > 0)
        {
            chunk.UpdateResourceHp(worldTile.x, worldTile.y, newHp);
            chunk.IsDirty = true;
            WorldSaveManager.TryMarkChunkDirty(chunkX, chunkY, sectionId);

            photonView.RPC(
                nameof(RPC_Client_PlayHitEffect),
                RpcTarget.All,
                chunkX,
                chunkY,
                tileIndex);
            return;
        }

        // Destroyed
        chunk.RemoveResource(worldTile.x, worldTile.y);
        chunk.IsDirty = true;
        WorldSaveManager.TryMarkChunkDirty(chunkX, chunkY, sectionId);

        Vector3 worldPos = new Vector3(worldTile.x, worldTile.y, 0f);
        SpawnLootDrops(config, worldPos);

        photonView.RPC(
            nameof(RPC_Client_DestroyResource),
            RpcTarget.All,
            chunkX,
            chunkY,
            tileIndex);
    }

    [PunRPC]
    public void RPC_Client_PlayHitEffect(int chunkX, int chunkY, int tileIndex)
    {
        if (!TryFindResourceVisual(chunkX, chunkY, tileIndex, out GameObject visual))
            return;

        ParticleSystem ps = visual.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
            ps.Play();

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null && !string.IsNullOrEmpty(hitAnimatorTrigger))
            animator.SetTrigger(hitAnimatorTrigger);
    }

    [PunRPC]
    public void RPC_Client_DestroyResource(int chunkX, int chunkY, int tileIndex)
    {
        if (ResourceSpawnerManager.Instance != null)
        {
            ResourceSpawnerManager.Instance.RemoveResourceVisual(chunkX, chunkY, tileIndex);
            return;
        }

        if (TryFindResourceVisual(chunkX, chunkY, tileIndex, out GameObject visual))
            Destroy(visual);
    }

    private void SpawnLootDrops(ResourceConfigData config, Vector3 worldPos)
    {
        if (config.dropTable == null || config.dropTable.Length == 0)
            return;

        foreach (DropEntry drop in config.dropTable)
        {
            if (drop == null || string.IsNullOrEmpty(drop.itemId))
                continue;

            if (Random.value > drop.dropChance)
                continue;

            int minAmount = Mathf.Max(0, drop.minAmount);
            int maxAmount = Mathf.Max(minAmount, drop.maxAmount);
            int amount = Random.Range(minAmount, maxAmount + 1);

            for (int i = 0; i < amount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    Random.Range(-0.25f, 0.25f),
                    0f);

                PhotonNetwork.InstantiateRoomObject(
                    lootPrefabName,
                    worldPos + offset,
                    Quaternion.identity);
            }
        }
    }

    private static bool TryGetChunk(int chunkX, int chunkY, out UnifiedChunkData chunk, out int sectionId)
    {
        chunk = null;
        sectionId = -1;

        WorldDataManager manager = WorldDataManager.Instance;
        if (manager == null || !manager.IsInitialized)
            return false;

        Vector2Int chunkPos = new Vector2Int(chunkX, chunkY);

        foreach (WorldSectionConfig config in manager.sectionConfigs)
        {
            if (!config.IsActive || !config.ContainsChunk(chunkPos))
                continue;

            sectionId = config.SectionId;
            chunk = manager.GetChunk(sectionId, chunkPos);
            if (chunk != null)
                return true;
        }

        return false;
    }

    private static Vector2Int TileIndexToWorldTile(int chunkX, int chunkY, int tileIndex)
    {
        int chunkSize = Mathf.Max(1, WorldDataManager.Instance != null
            ? WorldDataManager.Instance.chunkSizeTiles
            : 30);

        int localX = tileIndex % chunkSize;
        int localY = tileIndex / chunkSize;

        int worldX = (chunkX * chunkSize) + localX;
        int worldY = (chunkY * chunkSize) + localY;

        return new Vector2Int(worldX, worldY);
    }

    private static bool TryFindResourceVisual(int chunkX, int chunkY, int tileIndex, out GameObject visual)
    {
        visual = null;

        if (ResourceSpawnerManager.Instance != null &&
            ResourceSpawnerManager.Instance.TryGetResourceVisual(chunkX, chunkY, tileIndex, out visual))
            return true;

        Vector2Int worldTile = TileIndexToWorldTile(chunkX, chunkY, tileIndex);
        int wx = worldTile.x;
        int wy = worldTile.y;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go == null || !go.name.StartsWith("Resource_"))
                continue;

            Vector3 pos = go.transform.position;
            if (Mathf.FloorToInt(pos.x) == wx && Mathf.FloorToInt(pos.y) == wy)
            {
                visual = go;
                return true;
            }
        }

        return false;
    }
}
