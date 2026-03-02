import { Injectable } from '@nestjs/common';
import { RpcException } from '@nestjs/microservices';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { DroppedItem, DroppedItemDocument } from './dropped-item.schema';
import { CreateDroppedItemDto } from './dto/create-dropped-item.dto';
import { GetDroppedItemsDto } from './dto/get-dropped-items.dto';
import { DeleteDroppedItemDto } from './dto/delete-dropped-item.dto';

@Injectable()
export class DroppedItemService {
  constructor(
    @InjectModel(DroppedItem.name)
    private droppedItemModel: Model<DroppedItemDocument>,
  ) {}

  /**
   * Persist a newly dropped item to MongoDB.
   * Called by Master client after generating dropId and expireAt.
   */
  async createDroppedItem(dto: CreateDroppedItemDto): Promise<DroppedItem> {
    try {
      return await this.droppedItemModel.create({
        dropId: dto.dropId,
        roomName: dto.roomName,
        itemId: dto.itemId,
        itemName: dto.itemName,
        itemType: dto.itemType,
        itemCategory: dto.itemCategory,
        quality: dto.quality ?? 0,
        quantity: dto.quantity ?? 1,
        iconUrl: dto.iconUrl ?? '',
        isStackable: dto.isStackable ?? true,
        worldX: dto.worldX,
        worldY: dto.worldY,
        chunkX: dto.chunkX,
        chunkY: dto.chunkY,
        sectionId: dto.sectionId ?? 0,
        droppedByActorId: dto.droppedByActorId ?? 0,
        droppedAt: dto.droppedAt ? new Date(dto.droppedAt) : new Date(),
        expireAt: dto.expireAt
          ? new Date(dto.expireAt)
          : new Date(Date.now() + 360_000),
      });
    } catch (err) {
      if ((err as any)?.code === 11000) {
        throw new RpcException({
          status: 409,
          message: `Dropped item with dropId '${dto.dropId}' already exists`,
        });
      }
      throw err;
    }
  }

  /**
   * Delete a dropped item by its dropId.
   * Called when an item is picked up or manually despawned.
   */
  async deleteDroppedItem(
    dto: DeleteDroppedItemDto,
  ): Promise<{ deleted: boolean }> {
    const result = await this.droppedItemModel
      .findOneAndDelete({ dropId: dto.dropId })
      .exec();

    if (!result) {
      // Item may have already been TTL-deleted or picked up by another player
      return { deleted: false };
    }

    return { deleted: true };
  }

  /**
   * Find dropped items by room name, optionally filtered by chunk coordinates.
   * Used for late-join sync and chunk-based loading.
   */
  async getDroppedItems(dto: GetDroppedItemsDto): Promise<DroppedItem[]> {
    if (!dto.roomName) {
      throw new RpcException({ status: 400, message: 'roomName is required' });
    }

    const filter: Record<string, any> = { roomName: dto.roomName };

    // Optional chunk-level filtering
    if (dto.chunkX !== undefined && dto.chunkX !== null) {
      filter.chunkX = dto.chunkX;
    }
    if (dto.chunkY !== undefined && dto.chunkY !== null) {
      filter.chunkY = dto.chunkY;
    }

    return this.droppedItemModel.find(filter).exec();
  }
}
