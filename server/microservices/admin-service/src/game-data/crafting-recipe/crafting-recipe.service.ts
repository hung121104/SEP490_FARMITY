import {
  Injectable,
  BadRequestException,
  ConflictException,
  NotFoundException,
} from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { CraftingRecipe, CraftingRecipeDocument } from './crafting-recipe.schema';
import { Item, ItemDocument } from '../item/item.schema';
import { CreateCraftingRecipeDto } from './dto/create-crafting-recipe.dto';
import { UpdateCraftingRecipeDto } from './dto/update-crafting-recipe.dto';

@Injectable()
export class CraftingRecipeService {
  constructor(
    @InjectModel(CraftingRecipe.name)
    private recipeModel: Model<CraftingRecipeDocument>,

    @InjectModel(Item.name)
    private itemModel: Model<ItemDocument>,
  ) {}

  // ── Private helpers ─────────────────────────────────────────────────────────

  /** Verify that every itemID string actually exists in the Item collection.
   *  Throws BadRequestException listing the unknown IDs. */
  private async validateItemIds(ids: string[]): Promise<void> {
    const unique = [...new Set(ids)];
    const found = await this.itemModel
      .find({ itemID: { $in: unique } })
      .select('itemID')
      .lean()
      .exec();

    const foundIds = new Set(found.map((doc: any) => doc.itemID));
    const missing = unique.filter(id => !foundIds.has(id));

    if (missing.length > 0) {
      throw new BadRequestException(
        `The following item IDs do not exist in the database: ${missing.join(', ')}`,
      );
    }
  }

  // ── CRUD ────────────────────────────────────────────────────────────────────

  async create(dto: CreateCraftingRecipeDto): Promise<CraftingRecipe> {
    // 1. Duplicate check
    const existing = await this.recipeModel
      .findOne({ recipeID: dto.recipeID })
      .exec();
    if (existing) {
      throw new ConflictException(
        `CraftingRecipe with recipeID "${dto.recipeID}" already exists`,
      );
    }

    // 2. Validate all item IDs
    const allItemIds = [
      dto.resultItemId,
      ...dto.ingredients.map(i => i.itemId),
    ];
    await this.validateItemIds(allItemIds);

    // 3. Persist
    const recipe = new this.recipeModel(dto);
    return recipe.save();
  }

  /** Returns the catalog payload for the Unity client: { recipes: [...] } */
  async getCatalog(): Promise<{ recipes: CraftingRecipe[] }> {
    const recipes = await this.recipeModel.find().exec();
    return { recipes };
  }

  async findAll(): Promise<CraftingRecipe[]> {
    return this.recipeModel.find().exec();
  }

  async findById(id: string): Promise<CraftingRecipe | null> {
    return this.recipeModel.findById(id).exec();
  }

  async findByRecipeID(recipeID: string): Promise<CraftingRecipe | null> {
    return this.recipeModel.findOne({ recipeID }).exec();
  }

  async update(recipeID: string, dto: UpdateCraftingRecipeDto): Promise<CraftingRecipe> {
    // Validate item IDs only when they appear in the update payload
    const idsToCheck: string[] = [];
    if (dto.resultItemId) idsToCheck.push(dto.resultItemId);
    if (dto.ingredients?.length) idsToCheck.push(...dto.ingredients.map(i => i.itemId));
    if (idsToCheck.length > 0) await this.validateItemIds(idsToCheck);

    const updated = await this.recipeModel
      .findOneAndUpdate({ recipeID }, { $set: dto }, { new: true })
      .exec();

    if (!updated) {
      throw new NotFoundException(`CraftingRecipe with recipeID "${recipeID}" not found`);
    }
    return updated;
  }

  async delete(recipeID: string): Promise<CraftingRecipe | null> {
    const recipe = await this.recipeModel.findOneAndDelete({ recipeID }).exec();
    if (!recipe) {
      throw new NotFoundException(`CraftingRecipe with recipeID "${recipeID}" not found`);
    }
    return recipe;
  }
}
