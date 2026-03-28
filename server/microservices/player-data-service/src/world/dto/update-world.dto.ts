import { UpsertCharacterDto } from '../../character/dto/upsert-character.dto';

// ── Tile delta types ────────────────────────────────────────────────────────

/**
 * Tile delta payload. `type` is the only structurally required field.
 * All crop-specific fields (plantId, cropStage, growthTimer, etc.) are passed
 * through as-is from the Unity client, so adding a new field to CropTileData
 * on the client requires NO change here or in the schema.
 */
export type TileDataDto = { type?: string } & Record<string, unknown>;

/** One chunk's worth of changed tiles.  tiles key = local tile index "0"–"899". */
export class ChunkDeltaDto {
  chunkX: number;
  chunkY: number;
  sectionId: number;
  /** Only the tiles that changed; key = string(localTileIndex) */
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

// ── Chest delta types ─────────────────────────────────────────────────────────

export class ChestSlotDeltaDto {
  itemId: string;
  quantity: number;
}

/** One chest's changed slots.  slots key = slot index "0"–"35". */
export class ChestDeltaDto {
  tileX: number;
  tileY: number;
  maxSlots: number;
  structureLevel: number;
  /** Only the slots that changed; key = string(slotIndex) */
  slots: Record<string, ChestSlotDeltaDto>;
}

/** Identifies a chest to be deleted from the database. */
export class DeletedChestDto {
  tileX: number;
  tileY: number;
}

// ── Main DTO ────────────────────────────────────────────────────────────────

export class UpdateWorldDto {
  worldId: string;
  ownerId: string;

  // Optional world time/economy fields
  day?: number;
  month?: number;
  year?: number;
  hour?: number;
  minute?: number;
  gold?: number;

  // Weather persistence
  weatherToday?: number;
  weatherTomorrow?: number;

  // Up to 4 characters to upsert
  characters?: UpsertCharacterDto[];

  /**
   * Tile deltas — only the chunks that changed since the last save.
   * Backend merges these into the `chunks` collection atomically.
   */
  deltas?: ChunkDeltaDto[];

  /**
   * Inventory deltas — only the players whose inventory changed since last save.
   * Backend merges these into the `characters` collection atomically.
   */
  inventoryDeltas?: PlayerInventoryDeltaDto[];

  /**
   * Chest deltas — only the chests whose slots changed since last save.
   * Backend upserts these into the `chests` collection atomically.
   */
  chestDeltas?: ChestDeltaDto[];

  /**
   * Chests that were destroyed since last save.
   * Backend deletes the matching documents from the `chests` collection.
   */
  deletedChests?: DeletedChestDto[];
}
