/**
 * Update or clear a specific slot in the inventory.
 * If itemId is null or quantity <= 0, the slot is removed.
 */
export class UpdateSlotDto {
  characterId: string;

  /** Slot index, 0 – 35 */
  slotIndex: number;

  /** null = remove the item from this slot */
  itemId: string | null;

  /** Quantity; <= 0 will trigger slot removal */
  quantity: number;
}
