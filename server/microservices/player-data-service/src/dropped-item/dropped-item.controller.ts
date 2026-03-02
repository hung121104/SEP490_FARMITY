import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { DroppedItemService } from './dropped-item.service';
import { CreateDroppedItemDto } from './dto/create-dropped-item.dto';
import { DeleteDroppedItemDto } from './dto/delete-dropped-item.dto';
import { GetDroppedItemsDto } from './dto/get-dropped-items.dto';

@Controller()
export class DroppedItemController {
  constructor(private readonly droppedItemService: DroppedItemService) {}

  @MessagePattern('create-dropped-item')
  async createDroppedItem(@Body() dto: CreateDroppedItemDto) {
    return this.droppedItemService.createDroppedItem(dto);
  }

  @MessagePattern('delete-dropped-item')
  async deleteDroppedItem(@Body() dto: DeleteDroppedItemDto) {
    return this.droppedItemService.deleteDroppedItem(dto);
  }

  @MessagePattern('get-dropped-items')
  async getDroppedItems(@Body() dto: GetDroppedItemsDto) {
    return this.droppedItemService.getDroppedItems(dto);
  }
}
