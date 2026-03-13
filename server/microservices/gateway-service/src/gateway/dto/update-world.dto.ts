// ── Tile delta types (mirrored from player-data-service) ────────────────────

export class TileDataDto {
  type?: string;
  plantId?: string | null;
  cropStage?: number;
  growthTimer?: number;
  pollenHarvestCount?: number;
  isWatered?: boolean;
  isFertilized?: boolean;
  isPollinated?: boolean;
}

/** One dirty chunk's changed tiles.  key = local tile index "0"–"899". */
export class ChunkDeltaDto {
  chunkX: number;
  chunkY: number;
  sectionId: number;
  tiles: Record<string, TileDataDto>;
}

// ── Inventory delta types ────────────────────────────────────────────────────

export class InventorySlotDeltaDto {
  itemId: string;
  quantity: number;
}

/** One player's changed inventory slots.  slots key = slot index "0"–"35". */
export class PlayerInventoryDeltaDto {
  accountId: string;
  /** Only the slots that changed; key = string(slotIndex) */
  slots: Record<string, InventorySlotDeltaDto>;
}

// ── Main DTO ────────────────────────────────────────────────────────────────

export class UpsertCharacterInWorldDto {
  _id?: string;
  accountId: string;
  positionX: number;
  positionY: number;
  sectionIndex?: number;
}

export class UpdateWorldDto {
  worldId: string;

  // Optional world fields to update
  day?: number;
  month?: number;
  year?: number;
  hour?: number;
  minute?: number;
  gold?: number;

  // Up to 4 characters
  characters?: UpsertCharacterInWorldDto[];

  /**
   * Tile deltas — only from dirty chunks.
   * Forwarded transparently to player-data-service save-world handler.
   */
  deltas?: ChunkDeltaDto[];

  /**
   * Inventory deltas — only players whose inventory changed since last save.
   * Forwarded transparently to player-data-service save-world handler.
   */
  inventoryDeltas?: PlayerInventoryDeltaDto[];
}
