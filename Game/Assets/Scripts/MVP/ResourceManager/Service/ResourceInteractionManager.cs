using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

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
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.2f;

    public void OnEnable()
    {
        ChunkDataSyncManager.OnResourceHpUpdated += HandleResourceHpUpdated;
    }

    public void OnDisable()
    {
        ChunkDataSyncManager.OnResourceHpUpdated -= HandleResourceHpUpdated;
    }

    private void HandleResourceHpUpdated(int worldX, int worldY, int newHp)
    {
        // Play the hit effect when we receive a health update (means it was hit but not destroyed)
        if (WorldDataManager.Instance == null) return;
        
        int chunkX = Mathf.FloorToInt(worldX / (float)WorldDataManager.Instance.chunkSizeTiles);
        int chunkY = Mathf.FloorToInt(worldY / (float)WorldDataManager.Instance.chunkSizeTiles);
        
        int localX = worldX - (chunkX * WorldDataManager.Instance.chunkSizeTiles);
        int localY = worldY - (chunkY * WorldDataManager.Instance.chunkSizeTiles);
        int tileIndex = localY * WorldDataManager.Instance.chunkSizeTiles + localX;
        
        PlayHitEffectLocally(chunkX, chunkY, tileIndex);
    }

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

        ToolData hitTool = ItemCatalogService.Instance?.GetItemData<ToolData>(toolId);
        if (hitTool == null)
        {
            Debug.Log($"[ResourceInteraction] Reject hit from actor {info.Sender.ActorNumber}: tool '{toolId}' not found or not a ToolData.");
            return;
        }

        if (hitTool.toolType != config.requiredToolType || hitTool.toolPower < config.minToolPower)
        {
            Debug.Log(
                $"[ResourceInteraction] Reject hit from actor {info.Sender.ActorNumber}: " +
                $"tool '{toolId}' (Type: {hitTool.toolType}, Power: {hitTool.toolPower}) does not satisfy required {config.requiredToolType} with minimum power {config.minToolPower}.");
            return;
        }

        // Calculate Damage: tool power + (tool power - minimum tool power)
        int calculatedDamage = hitTool.toolPower + (hitTool.toolPower - config.minToolPower);
        if (calculatedDamage <= 0) calculatedDamage = 1; // Fallback safeguard

        int newHp = Mathf.Max(0, resourceTile.CurrentHp - calculatedDamage);

        if (newHp > 0)
        {
            chunk.UpdateResourceHp(worldTile.x, worldTile.y, newHp);
            chunk.IsDirty = true;
            WorldSaveManager.TryMarkChunkDirty(chunkX, chunkY, sectionId);

            ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
            if (syncManager != null)
            {
                syncManager.BroadcastResourceHpUpdated(Mathf.FloorToInt(worldTile.x), Mathf.FloorToInt(worldTile.y), newHp);
            }
            return;
        }

        // Destroyed
        chunk.RemoveResource(worldTile.x, worldTile.y);
        chunk.IsDirty = true;
        WorldSaveManager.TryMarkChunkDirty(chunkX, chunkY, sectionId);

        Vector3 worldPos = new Vector3(worldTile.x, worldTile.y, 0f);
        SpawnLootDrops(config, worldPos);

        ChunkDataSyncManager syncManagerEnd = FindAnyObjectByType<ChunkDataSyncManager>();
        if (syncManagerEnd != null)
        {
            syncManagerEnd.BroadcastResourceRemoved(Mathf.FloorToInt(worldTile.x), Mathf.FloorToInt(worldTile.y));
        }
        DestroyResourceLocally(chunkX, chunkY, tileIndex);
    }

    public void PlayHitEffectLocally(int chunkX, int chunkY, int tileIndex)
    {
        if (!TryFindResourceVisual(chunkX, chunkY, tileIndex, out GameObject visual))
            return;

        ParticleSystem ps = visual.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
            ps.Play();

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null && !string.IsNullOrEmpty(hitAnimatorTrigger))
            animator.SetTrigger(hitAnimatorTrigger);

        StartCoroutine(ShakeRoutine(visual.transform));
    }

    private IEnumerator ShakeRoutine(Transform target)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            if (target == null) yield break;

            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            target.position = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null) target.position = originalPos;
    }

    public void DestroyResourceLocally(int chunkX, int chunkY, int tileIndex)
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
        if (config.dropTable == null || config.dropTable.Count == 0)
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
