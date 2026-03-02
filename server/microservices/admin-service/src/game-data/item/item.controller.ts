import { Controller, Body } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { ItemService } from './item.service';
import { CreateItemDto } from './dto/create-item.dto';

@Controller()
export class ItemController {
  constructor(private readonly itemService: ItemService) {}

  /** Create a new item definition */
  @MessagePattern('create-item')

  async createItem(@Payload() createItemDto: CreateItemDto) {
    return this.itemService.create(createItemDto);
  }

  /** Return full catalog: { items: [...] } – consumed by Unity client */
  @MessagePattern('get-item-catalog')
  async getItemCatalog() {
    return this.itemService.getCatalog();
  }

  /** Return flat array of all items */
  @MessagePattern('get-all-items')
  async getAllItems() {
    return this.itemService.findAll();
  }

  /** Find one item by MongoDB _id */
  @MessagePattern('get-item-by-id')
  async getItemById(@Payload() id: string) {
    return this.itemService.findById(id);
  }

  /** Find one item by the game-side itemID string */
  @MessagePattern('get-item-by-item-id')
  async getItemByItemId(@Payload() itemID: string) {
    return this.itemService.findByItemID(itemID);
  }

  /** Delete an item by MongoDB _id */
  @MessagePattern('delete-item')
  async deleteItem(@Payload() id: string) {
    return this.itemService.delete(id);
  }
}
