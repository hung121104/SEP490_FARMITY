using System.Collections;
using UnityEngine;

/// <summary>
/// Development test harness. Press <see cref="addItemKey"/> (default: T) to add items
/// from the live-service catalog. No ScriptableObjects referenced anywhere — items
/// are resolved by string ID from <see cref="ItemCatalogService"/>.
/// </summary>
public class TestInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryGameView inventorySystem;

    [Header("Data-Driven Test Items")]
    [Tooltip("Item IDs to add on key press. Must match IDs in the catalog JSON.")]
    [SerializeField] private string[] testItemIds = { "resource_gold", "resource_iron" };
    [SerializeField] private int     quantityPerItem = 2;
    [SerializeField] private Quality qualityOverride  = Quality.Normal;
    [SerializeField] private KeyCode addItemKey       = KeyCode.T;

    private void Start() => StartCoroutine(WaitForCatalogReady());

    private IEnumerator WaitForCatalogReady()
    {
        yield return new WaitUntil(() =>
            ItemCatalogService.Instance != null && ItemCatalogService.Instance.IsReady);
        Debug.Log("[TestInventory] Catalog ready — press T to add items.");
    }

    private void Update()
    {
        if (!Input.GetKeyDown(addItemKey)) return;

        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
        {
            Debug.LogWarning("[TestInventory] Catalog not ready yet.");
            return;
        }

        if (inventorySystem == null)
        {
            Debug.LogError("[TestInventory] InventoryGameView not assigned.");
            return;
        }

        foreach (var id in testItemIds)
        {
            bool added = inventorySystem.AddItem(id, quantityPerItem, qualityOverride);
            Debug.Log(added
                ? $"[TestInventory] Added '{id}' x{quantityPerItem}."
                : $"[TestInventory] Could not add '{id}' (catalog miss or inventory full).");
        }
    }
}
