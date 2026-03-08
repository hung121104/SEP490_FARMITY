import { Injectable, ConflictException, NotFoundException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Item, ItemDocument } from './item.schema';
import { CreateItemDto } from './dto/create-item.dto';
import { UpdateItemDto } from './dto/update-item.dto';


@Injectable()
export class ItemService {
  constructor(
    @InjectModel(Item.name) private itemModel: Model<ItemDocument>,
  ) {}

  async create(createItemDto: CreateItemDto): Promise<Item> {
    const existing = await this.itemModel.findOne({ itemID: createItemDto.itemID }).exec();
    if (existing) {
      throw new ConflictException(`Item with itemID "${createItemDto.itemID}" already exists`);
    }
    const item = new this.itemModel(createItemDto);
    return item.save();
  }

  /** Returns the catalog payload expected by the Unity client:
   *  { items: [...] }  */
  async getCatalog(): Promise<{ items: Item[] }> {
    const items = await this.itemModel.find().exec();
    return { items };
  }

  async findAll(): Promise<Item[]> {
    return this.itemModel.find().exec();
  }

  async findById(id: string): Promise<Item | null> {
    return this.itemModel.findById(id).exec();
  }

  async findByItemID(itemID: string): Promise<Item | null> {
    return this.itemModel.findOne({ itemID }).exec();
  }

  async update(itemID: string, dto: UpdateItemDto): Promise<Item> {
    const updated = await this.itemModel
      .findOneAndUpdate({ itemID }, { $set: dto }, { new: true })
      .exec();
    if (!updated) throw new NotFoundException(`Item with itemID "${itemID}" not found`);
    return updated;
  }

  async delete(itemID: string): Promise<Item | null> {
    const item = await this.itemModel.findOneAndDelete({ itemID }).exec();
    if (!item) throw new NotFoundException(`Item with itemID "${itemID}" not found`);
    return item;
  }
}
