/**
 * Sync the full inventory of a character (overwrite all slots).
 * Used when the Photon Master needs to persist the full inventory state to DB
 * (e.g. when the session ends or on a periodic checkpoint).
 */
export class SyncInventoryDto {
  characterId: string;

  /** List of non-empty slots. Slots absent from this list will be cleared. */
  slots: {
    slotIndex: number; // 0 – 35
    itemId: string;
    quantity: number;
  }[];
}
