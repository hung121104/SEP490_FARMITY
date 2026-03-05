/**
 * Apply multiple slot changes for a single character in one operation.
 * Used when one in-game action affects several slots at once
 * (e.g. crafting: consume 3 ingredients, produce 1 result).
 *
 * Rules per slot entry:
 *  - itemId null  OR quantity <= 0 → remove that slot
 *  - otherwise                     → upsert that slot
 */
export class BulkUpdateSlotsDto {
  characterId: string;

  changes: {
    slotIndex: number; // 0 – 35
    itemId: string | null; // null = clear the slot
    quantity: number; // <= 0 = clear the slot
  }[];
}
