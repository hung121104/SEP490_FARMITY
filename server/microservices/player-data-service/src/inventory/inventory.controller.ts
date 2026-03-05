import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { InventoryService } from './inventory.service';
import { UpdateSlotDto } from './dto/update-slot.dto';
import { SyncInventoryDto } from './dto/sync-inventory.dto';
import { BulkUpdateSlotsDto } from './dto/bulk-update-slots.dto';

@Controller()
export class InventoryController {
  constructor(private readonly inventoryService: InventoryService) {}

  /**
   * Retrieve the inventory of a character by characterId.
   * Pattern: 'get-inventory'
   * Payload: { characterId: string }
   */
  @MessagePattern('get-inventory')
  async getInventory(@Payload() data: { characterId: string }) {
    return this.inventoryService.getInventory(data.characterId);
  }

  /**
   * Retrieve inventories for multiple characters at once.
   * Used to bulk-load inventory data for all characters currently active in a world section.
   * Pattern: 'get-inventories-by-character-ids'
   * Payload: { characterIds: string[] }
   */
  @MessagePattern('get-inventories-by-character-ids')
  async getInventoriesByCharacterIds(
    @Payload() data: { characterIds: string[] },
  ) {
    return this.inventoryService.getInventoriesByCharacterIds(
      data.characterIds,
    );
  }

  /**
   * Update or clear a specific slot in the inventory.
   * Pattern: 'update-inventory-slot'
   */
  @MessagePattern('update-inventory-slot')
  async updateSlot(@Payload() dto: UpdateSlotDto) {
    return this.inventoryService.updateSlot(dto);
  }

  /**
   * Apply multiple slot changes for one character in a single call.
   * Used by the Photon master when one action affects several slots at once
   * (e.g. crafting, trading, looting).
   * Pattern: 'bulk-update-inventory-slots'
   */
  @MessagePattern('bulk-update-inventory-slots')
  async bulkUpdateSlots(@Payload() dto: BulkUpdateSlotsDto) {
    return this.inventoryService.bulkUpdateSlots(dto);
  }

  /**
   * Overwrite the entire inventory of a character (full sync).
   * Pattern: 'sync-inventory'
   */
  @MessagePattern('sync-inventory')
  async syncInventory(@Payload() dto: SyncInventoryDto) {
    return this.inventoryService.syncInventory(dto);
  }

  /**
   * Delete the inventory when its Character is removed.
   * Pattern: 'delete-inventory'
   * Payload: { characterId: string }
   */
  @MessagePattern('delete-inventory')
  async deleteInventory(@Payload() data: { characterId: string }) {
    await this.inventoryService.deleteByCharacterId(data.characterId);
    return { success: true };
  }
}
