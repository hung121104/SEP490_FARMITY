import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type InventoryDocument = Inventory & Document;

/**
 * Represents a single slot in the inventory.
 * Only non-empty slots are stored — empty slots are omitted from the array.
 */
@Schema({ _id: false })
export class InventorySlot {
  /** Slot position index, ranging from 0 to 35 */
  @Prop({ required: true, min: 0, max: 35 })
  slotIndex: number;

  /** Item ID (references the Item collection in admin-service) */
  @Prop({ type: Types.ObjectId, required: true })
  itemId: Types.ObjectId;

  /** Stack quantity of the item */
  @Prop({ required: true, min: 1 })
  quantity: number;
}

export const InventorySlotSchema = SchemaFactory.createForClass(InventorySlot);

/**
 * Stores the inventory bag of a character.
 * Each Character has exactly one Inventory document (1-1 relationship).
 * Maximum 36 slots; only non-empty slots are persisted.
 */
@Schema({ timestamps: true })
export class Inventory {
  /** Reference to the owning Character (1-1, unique) */
  @Prop({ type: Types.ObjectId, ref: 'Character', required: true })
  characterId: Types.ObjectId;

  /**
   * List of occupied slots.
   * Empty slots are not stored to save space.
   * slotIndex must be unique within this array.
   */
  @Prop({ type: [InventorySlotSchema], default: [] })
  slots: InventorySlot[];
}

export const InventorySchema = SchemaFactory.createForClass(Inventory);

// Each Character has exactly one Inventory
InventorySchema.index({ characterId: 1 }, { unique: true });
