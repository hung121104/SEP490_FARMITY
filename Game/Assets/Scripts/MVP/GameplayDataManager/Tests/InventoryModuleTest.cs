using UnityEngine;

/// <summary>
/// Attach to any GameObject in a test scene to verify InventoryDataModule works.
/// Remove after validation.
/// </summary>
public class InventoryModuleTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private bool showStats = true;

    private void Start()
    {
        if (runOnStart) RunTests();
    }

    [ContextMenu("Run Tests")]
    public void RunTests()
    {
        var wdm = WorldDataManager.Instance;
        int passed = 0, failed = 0;

        if (verboseLogs) Debug.Log("========== InventoryDataModule Tests ==========");

        // ── Test 1: Register characters ──────────────────────────────────
        wdm.RegisterCharacterInventory("player_001", maxSlots: 36);
        wdm.RegisterCharacterInventory("player_002", maxSlots: 36);

        Assert("T01 Register player_001",  wdm.HasCharacterInventory("player_001"), ref passed, ref failed);
        Assert("T02 Register player_002",  wdm.HasCharacterInventory("player_002"), ref passed, ref failed);
        Assert("T03 Unknown char = false", !wdm.HasCharacterInventory("ghost_999"),  ref passed, ref failed);

        // ── Test 2: Set slots ─────────────────────────────────────────────
        wdm.SetInventorySlot("player_001", 0, itemId: 10, quantity: 5);
        wdm.SetInventorySlot("player_001", 1, itemId: 20, quantity: 1);
        wdm.SetInventorySlot("player_002", 0, itemId: 30, quantity: 99);

        Assert("T04 player_001 slot0 has item",
            wdm.TryGetInventorySlot("player_001", 0, out var s04) && s04.ItemId == 10 && s04.Quantity == 5,
            ref passed, ref failed);

        Assert("T05 player_002 slot0 qty=99",
            wdm.TryGetInventorySlot("player_002", 0, out var s05) && s05.Quantity == 99,
            ref passed, ref failed);

        // ── Test 3: HasItemInSlot ─────────────────────────────────────────
        Assert("T06 HasItem slot0 = true",  wdm.HasInventoryItem("player_001", 0), ref passed, ref failed);
        Assert("T07 HasItem slot5 = false", !wdm.HasInventoryItem("player_001", 5), ref passed, ref failed);

        // ── Test 4: AddQuantity ───────────────────────────────────────────
        wdm.AddInventoryQuantity("player_001", 0, 10, 3); // 5 + 3 = 8
        Assert("T08 Add qty → 8",
            wdm.TryGetInventorySlot("player_001", 0, out var s08) && s08.Quantity == 8,
            ref passed, ref failed);

        // ── Test 5: RemoveQuantity ────────────────────────────────────────
        wdm.RemoveInventoryQuantity("player_001", 0, 2); // 8 - 2 = 6
        Assert("T09 Remove qty → 6",
            wdm.TryGetInventorySlot("player_001", 0, out var s09) && s09.Quantity == 6,
            ref passed, ref failed);

        // ── Test 6: RemoveQuantity clears slot when qty = 0 ───────────────
        wdm.SetInventorySlot("player_001", 2, itemId: 50, quantity: 3);
        wdm.RemoveInventoryQuantity("player_001", 2, 3); // 3 - 3 = 0 → slot cleared
        Assert("T10 Remove all → slot empty",
            !wdm.HasInventoryItem("player_001", 2),
            ref passed, ref failed);

        // ── Test 7: ClearSlot ─────────────────────────────────────────────
        wdm.SetInventorySlot("player_001", 3, itemId: 99, quantity: 10);
        wdm.ClearInventorySlot("player_001", 3);
        Assert("T11 ClearSlot → empty",
            !wdm.HasInventoryItem("player_001", 3),
            ref passed, ref failed);

        // ── Test 8: SwapSlots ─────────────────────────────────────────────
        wdm.SetInventorySlot("player_001", 4, itemId: 11, quantity: 2);
        wdm.SetInventorySlot("player_001", 5, itemId: 22, quantity: 7);
        wdm.SwapInventorySlots("player_001", 4, 5);

        bool swapOk = wdm.TryGetInventorySlot("player_001", 4, out var swA)
                   && wdm.TryGetInventorySlot("player_001", 5, out var swB)
                   && swA.ItemId == 22 && swB.ItemId == 11;
        Assert("T12 SwapSlots", swapOk, ref passed, ref failed);

        // ── Test 9: CountItem ─────────────────────────────────────────────
        wdm.SetInventorySlot("player_001", 6,  itemId: 77, quantity: 10);
        wdm.SetInventorySlot("player_001", 7,  itemId: 77, quantity: 5);
        int total77 = wdm.CountInventoryItem("player_001", 77);
        Assert("T13 CountItem(77) = 15", total77 == 15, ref passed, ref failed);

        // ── Test 10: Network delta encode/decode roundtrip ────────────────
        byte[] delta = InventoryDataModule.EncodeSlotDelta("player_001", 0, 10, 6);
        bool deltaApplied = wdm.InventoryData.ApplySlotDelta(delta);
        Assert("T14 Delta encode/apply", deltaApplied, ref passed, ref failed);
        Assert("T15 Delta roundtrip value correct",
            wdm.TryGetInventorySlot("player_001", 0, out var s15) && s15.ItemId == 10 && s15.Quantity == 6,
            ref passed, ref failed);

        // ── Test 11: Serialize / Deserialize single inventory ─────────────
        byte[] invBytes = wdm.SerializeCharacterInventory("player_001");
        Assert("T16 Serialize not null", invBytes != null && invBytes.Length > 0, ref passed, ref failed);

        var loaded = CharacterInventory.FromBytes(invBytes);
        Assert("T17 Deserialize charId", loaded.CharacterId == "player_001", ref passed, ref failed);
        Assert("T18 Deserialize slot count > 0", loaded.OccupiedSlotCount > 0, ref passed, ref failed);

        // ── Test 12: SerializeAll / DeserializeAll ────────────────────────
        byte[] allBytes = wdm.SerializeAllInventories();
        Assert("T19 SerializeAll not null", allBytes != null && allBytes.Length > 0, ref passed, ref failed);

        // Register fresh module and restore
        var tempModule = new InventoryDataModule();
        tempModule.Initialize(wdm);
        tempModule.DeserializeAll(allBytes);
        Assert("T20 DeserializeAll player_001 exists", tempModule.HasCharacter("player_001"), ref passed, ref failed);
        Assert("T21 DeserializeAll player_002 exists", tempModule.HasCharacter("player_002"), ref passed, ref failed);

        // ── Test 13: Duplicate register returns existing ──────────────────
        var inv1 = wdm.RegisterCharacterInventory("player_001");
        var inv2 = wdm.RegisterCharacterInventory("player_001");
        Assert("T22 Duplicate register = same object", ReferenceEquals(inv1, inv2), ref passed, ref failed);

        // ── Test 14: Out-of-range slot index ─────────────────────────────
        bool rangeOk = !wdm.SetInventorySlot("player_001", 255, 1, 1); // maxSlots=36 so 255 is invalid
        Assert("T23 Out-of-range slot rejected", rangeOk, ref passed, ref failed);

        // ── Test 15: Dirty flags ──────────────────────────────────────────
        var inv = wdm.GetCharacterInventory("player_001");
        Assert("T24 Dirty flag set after change", inv != null && inv.IsDirty, ref passed, ref failed);

        var dirtyIds = wdm.InventoryData.GetDirtyCharacterIds();
        Assert("T25 GetDirtyCharacterIds contains player_001", dirtyIds.Contains("player_001"), ref passed, ref failed);

        wdm.InventoryData.ClearAllDirtyFlags();
        Assert("T26 ClearDirtyFlags → IsDirty = false", !inv.IsDirty, ref passed, ref failed);

        // ── Summary ───────────────────────────────────────────────────────
        Debug.Log($"========== Results: {passed}/{passed + failed} passed ==========");

        if (failed > 0)
            Debug.LogError($"[InventoryModuleTest] {failed} test(s) FAILED — check above warnings.");
        else
            Debug.Log("[InventoryModuleTest] All tests PASSED ✓");

        if (showStats) wdm.LogStats();
    }

    private void Assert(string label, bool condition, ref int passed, ref int failed)
    {
        if (condition)
        {
            if (verboseLogs) Debug.Log($"  ✓ {label}");
            passed++;
        }
        else
        {
            Debug.LogWarning($"  ✗ FAIL: {label}");
            failed++;
        }
    }
}
