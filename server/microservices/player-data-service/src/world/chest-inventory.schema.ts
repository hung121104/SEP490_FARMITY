import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type ChestInventoryDocument = ChestInventory & Document;

// ────────────────────────────────────────────────────────────────────────────
//  ChestSlotData sub-document
//  Stored inside the ChestInventory's `slots` Map.
//  Key = slot index as string ("0"–"35"), value = this sub-document.
//  Same Map pattern as Character.inventory for targeted $set updates.
// ────────────────────────────────────────────────────────────────────────────
@Schema({ _id: false })
export class ChestSlotData {
  @Prop({ required: true })
  itemId: string;

  @Prop({ required: true })
  quantity: number;
}

export const ChestSlotDataSchema = SchemaFactory.createForClass(ChestSlotData);

// ────────────────────────────────────────────────────────────────────────────
//  ChestInventory document
//  One document = one placed chest in the world, identified by
//  (worldId, tileX, tileY).
//  `slots` is a MongoDB Map: key = slot index "0"–"35" (string),
//  value = ChestSlotData sub-document.
//  Using a Map lets us use targeted $set operators
//  like  { $set: { "slots.5": { ... } } }  without touching other slots.
// ────────────────────────────────────────────────────────────────────────────
@Schema({ collection: 'chestinventories' })
export class ChestInventory {
  @Prop({ type: Types.ObjectId, ref: 'World', required: true, index: true })
  worldId: Types.ObjectId;

  @Prop({ required: true })
  tileX: number;

  @Prop({ required: true })
  tileY: number;

  @Prop({ required: true })
  maxSlots: number;

  @Prop({ required: true, default: 1 })
  structureLevel: number;

  /**
   * Map<slotIndex, ChestSlotData>.
   * slotIndex = "0"–"35" (string key).
   * Targeted $set operators like { $set: { "slots.5": { ... } } }
   * update individual slots without touching others.
   */
  @Prop({ type: Map, of: ChestSlotDataSchema, default: () => new Map() })
  slots: Map<string, ChestSlotData>;
}

export const ChestInventorySchema = SchemaFactory.createForClass(ChestInventory);

// Compound unique index: one chest per (world, tileX, tileY)
ChestInventorySchema.index({ worldId: 1, tileX: 1, tileY: 1 }, { unique: true });
