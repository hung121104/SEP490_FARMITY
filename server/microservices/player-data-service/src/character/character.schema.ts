import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type CharacterDocument = Character & Document;

// ────────────────────────────────────────────────────────────────────────────
//  InventorySlotData sub-document
//  Stored inside the Character's `inventory` Map.
//  Key = slot index as string ("0"–"35"), value = this sub-document.
//  Same Map pattern as Chunk.tiles for targeted $set updates.
// ────────────────────────────────────────────────────────────────────────────
@Schema({ _id: false })
export class InventorySlotData {
  @Prop({ required: true })
  itemId: string;

  @Prop({ required: true })
  quantity: number;
}

export const InventorySlotDataSchema = SchemaFactory.createForClass(InventorySlotData);

@Schema()
export class Character {
  @Prop({ required: true })
  @Prop({ type: Types.ObjectId, ref: 'World', required: true })
  worldId: Types.ObjectId;

  @Prop({ type: Types.ObjectId, ref: 'Account', required: true })
  accountId: Types.ObjectId;

  @Prop({ required: true })
  positionX: number;

  @Prop({ required: true })
  positionY: number;

  @Prop({ required: true })
  sectionIndex: number;

  /**
   * Map<slotIndex, InventorySlotData>.
   * slotIndex = "0"–"35" (string key).
   * Targeted $set operators like { $set: { "inventory.5": { ... } } }
   * update individual slots without touching others.
   */
  @Prop({ type: Map, of: InventorySlotDataSchema, default: () => new Map() })
  inventory: Map<string, InventorySlotData>;
}

export const CharacterSchema = SchemaFactory.createForClass(Character);
CharacterSchema.index({ worldId: 1, accountId: 1 }, { unique: true });