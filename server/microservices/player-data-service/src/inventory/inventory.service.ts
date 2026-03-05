import { Injectable, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types, ClientSession } from 'mongoose';
import { Inventory, InventoryDocument } from './inventory.schema';
import { UpdateSlotDto } from './dto/update-slot.dto';
import { SyncInventoryDto } from './dto/sync-inventory.dto';
import { BulkUpdateSlotsDto } from './dto/bulk-update-slots.dto';

@Injectable()
export class InventoryService {
  constructor(
    @InjectModel(Inventory.name)
    private inventoryModel: Model<InventoryDocument>,
  ) {}

  // ──────────────────────────────────────────────
  // Create an empty inventory for a character.
  // Called immediately after a Character document is created.
  // ──────────────────────────────────────────────
  async createInventory(
    characterId: Types.ObjectId | string,
    options?: { session?: ClientSession },
  ): Promise<Inventory> {
    const charOid =
      typeof characterId === 'string'
        ? new Types.ObjectId(characterId)
        : characterId;

    const created = await this.inventoryModel.create(
      [{ characterId: charOid, slots: [] }],
      { session: options?.session },
    );
    return Array.isArray(created)
      ? created[0]
      : (created as unknown as Inventory);
  }

  // ──────────────────────────────────────────────
  // Retrieve the full inventory of a character.
  // ──────────────────────────────────────────────
  async getInventory(characterId: Types.ObjectId | string): Promise<Inventory> {
    const charOid =
      typeof characterId === 'string'
        ? new Types.ObjectId(characterId)
        : characterId;

    const inventory = await this.inventoryModel.findOne({
      characterId: charOid,
    });
    if (!inventory) {
      throw new NotFoundException(
        `Inventory not found for character ${charOid.toString()}`,
      );
    }
    return inventory;
  }

  // ──────────────────────────────────────────────
  // Retrieve inventories for multiple characters at once.
  // Used when the gateway/Photon master needs all inventory data for the
  // characters currently active in a world section.
  // ──────────────────────────────────────────────
  async getInventoriesByCharacterIds(
    characterIds: (Types.ObjectId | string)[],
  ): Promise<Inventory[]> {
    const oids = characterIds.map((id) =>
      typeof id === 'string' ? new Types.ObjectId(id) : id,
    );
    return this.inventoryModel.find({ characterId: { $in: oids } }).exec();
  }

  // ──────────────────────────────────────────────
  // Update or remove a specific slot.
  // itemId === null or quantity <= 0 → clears the slot.
  // ──────────────────────────────────────────────
  async updateSlot(dto: UpdateSlotDto): Promise<Inventory> {
    const charOid = new Types.ObjectId(dto.characterId);
    const shouldClear = !dto.itemId || dto.quantity <= 0;

    let updated: InventoryDocument | null;

    if (shouldClear) {
      // Remove the slot from the array
      updated = await this.inventoryModel.findOneAndUpdate(
        { characterId: charOid },
        { $pull: { slots: { slotIndex: dto.slotIndex } } },
        { new: true },
      );
    } else {
      const itemOid = new Types.ObjectId(dto.itemId);

      // Try updating the slot if it already exists
      updated = await this.inventoryModel.findOneAndUpdate(
        { characterId: charOid, 'slots.slotIndex': dto.slotIndex },
        {
          $set: {
            'slots.$.itemId': itemOid,
            'slots.$.quantity': dto.quantity,
          },
        },
        { new: true },
      );

      // Slot does not exist yet → push a new entry into the array
      if (!updated) {
        updated = await this.inventoryModel.findOneAndUpdate(
          { characterId: charOid },
          {
            $push: {
              slots: {
                slotIndex: dto.slotIndex,
                itemId: itemOid,
                quantity: dto.quantity,
              },
            },
          },
          { new: true, upsert: true },
        );
      }
    }

    if (!updated) {
      throw new NotFoundException(
        `Inventory not found for character ${dto.characterId}`,
      );
    }
    return updated;
  }

  // ──────────────────────────────────────────────
  // Apply multiple slot changes for one character in a single DB round-trip.
  // Used by the Photon master when one in-game action affects several slots
  // at once (e.g. crafting, trading, looting a bag).
  // ──────────────────────────────────────────────
  async bulkUpdateSlots(dto: BulkUpdateSlotsDto): Promise<Inventory> {
    const charOid = new Types.ObjectId(dto.characterId);

    const toClear = dto.changes
      .filter((c) => !c.itemId || c.quantity <= 0)
      .map((c) => c.slotIndex);

    const toUpsert = dto.changes
      .filter((c) => c.itemId && c.quantity > 0)
      .map((c) => ({
        slotIndex: c.slotIndex,
        itemId: new Types.ObjectId(c.itemId),
        quantity: c.quantity,
      }));

    // Step 1: remove all slots marked for clearing
    if (toClear.length > 0) {
      await this.inventoryModel.updateOne(
        { characterId: charOid },
        { $pull: { slots: { slotIndex: { $in: toClear } } } },
      );
    }

    // Step 2: upsert each slot that has a valid item
    for (const slot of toUpsert) {
      const matched = await this.inventoryModel.updateOne(
        { characterId: charOid, 'slots.slotIndex': slot.slotIndex },
        {
          $set: {
            'slots.$.itemId': slot.itemId,
            'slots.$.quantity': slot.quantity,
          },
        },
      );
      if (matched.matchedCount === 0) {
        await this.inventoryModel.updateOne(
          { characterId: charOid },
          { $push: { slots: slot } },
        );
      }
    }

    const result = await this.inventoryModel.findOne({ characterId: charOid });
    if (!result) {
      throw new NotFoundException(
        `Inventory not found for character ${dto.characterId}`,
      );
    }
    return result;
  }

  // ──────────────────────────────────────────────
  // Overwrite the entire inventory (full slot sync).
  // Used when a Photon session ends or on a periodic checkpoint.
  // ──────────────────────────────────────────────
  async syncInventory(dto: SyncInventoryDto): Promise<Inventory> {
    const charOid = new Types.ObjectId(dto.characterId);

    const newSlots = dto.slots
      .filter((s) => s.quantity > 0 && s.itemId)
      .map((s) => ({
        slotIndex: s.slotIndex,
        itemId: new Types.ObjectId(s.itemId),
        quantity: s.quantity,
      }));

    const updated = await this.inventoryModel.findOneAndUpdate(
      { characterId: charOid },
      { $set: { slots: newSlots } },
      { new: true, upsert: true },
    );

    return updated as InventoryDocument;
  }

  // ──────────────────────────────────────────────
  // Delete the inventory when its Character is removed.
  // ──────────────────────────────────────────────
  async deleteByCharacterId(
    characterId: Types.ObjectId | string,
  ): Promise<void> {
    const charOid =
      typeof characterId === 'string'
        ? new Types.ObjectId(characterId)
        : characterId;
    await this.inventoryModel.deleteOne({ characterId: charOid });
  }

  // ──────────────────────────────────────────────
  // Delete all inventories belonging to characters in a world.
  // Called when the World is deleted.
  // ──────────────────────────────────────────────
  async deleteByCharacterIds(
    characterIds: (Types.ObjectId | string)[],
  ): Promise<number> {
    const oids = characterIds.map((id) =>
      typeof id === 'string' ? new Types.ObjectId(id) : id,
    );
    const result = await this.inventoryModel.deleteMany({
      characterId: { $in: oids },
    });
    return result.deletedCount ?? 0;
  }
}
