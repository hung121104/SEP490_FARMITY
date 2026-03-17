import { Controller, Body } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { ItemService } from './item.service';
import { CreateItemDto } from './dto/create-item.dto';
import { UpdateItemDto } from './dto/update-item.dto';

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

  /** Update an item by game-side itemID string */
  @MessagePattern('update-item')
  async updateItem(
    @Payload() payload: { itemID: string; dto: UpdateItemDto },
  ) {
    return this.itemService.update(payload.itemID, payload.dto);
  }

  /** Delete an item by game-side itemID string */
  @MessagePattern('delete-item')
  async deleteItem(@Payload() itemID: string) {
    return this.itemService.delete(itemID);
  }

  @MessagePattern('create-fertilizer')
  async createFertilizer(@Payload() createItemDto: CreateItemDto) {
    return this.itemService.createFertilizer(createItemDto);
  }

  @MessagePattern('get-fertilizer-catalog')
  async getFertilizerCatalog() {
    return this.itemService.getFertilizerCatalog();
  }

  @MessagePattern('get-all-fertilizers')
  async getAllFertilizers() {
    return this.itemService.findAllFertilizers();
  }

  @MessagePattern('get-fertilizer-by-id')
  async getFertilizerById(@Payload() id: string) {
    return this.itemService.findFertilizerById(id);
  }

  @MessagePattern('get-fertilizer-by-item-id')
  async getFertilizerByItemId(@Payload() itemID: string) {
    return this.itemService.findFertilizerByItemID(itemID);
  }

  @MessagePattern('update-fertilizer')
  async updateFertilizer(
    @Payload() payload: { itemID: string; dto: UpdateItemDto },
  ) {
    return this.itemService.updateFertilizer(payload.itemID, payload.dto);
  }

  @MessagePattern('delete-fertilizer')
  async deleteFertilizer(@Payload() itemID: string) {
    return this.itemService.deleteFertilizer(itemID);
  }
}
