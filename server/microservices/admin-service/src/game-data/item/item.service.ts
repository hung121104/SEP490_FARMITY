import { Injectable, ConflictException, NotFoundException, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Item, ItemDocument } from './item.schema';
import { CreateItemDto } from './dto/create-item.dto';
import { UpdateItemDto } from './dto/update-item.dto';
import { CombatSkill, CombatSkillDocument } from '../combat-skill/combat-skill.schema';
import { CombatSkillOwnership } from '../combat-skill/combat-skill.enums';
import { Plant, PlantDocument } from '../plant/plant.schema';
import {
  CraftingRecipe,
  CraftingRecipeDocument,
} from '../crafting-recipe/crafting-recipe.schema';
import {
  ResourceConfig,
  ResourceConfigDocument,
} from '../resource-config/resource-config.schema';
import { Material, MaterialDocument } from '../material/material.schema';

const FERTILIZER_ITEM_TYPE = 14;
const WEAPON_ITEM_TYPE = 6;
const TOOL_ITEM_TYPE = 0;

@Injectable()
export class ItemService {
  constructor(
    @InjectModel(Item.name) private itemModel: Model<ItemDocument>,
    @InjectModel(CombatSkill.name) private combatSkillModel: Model<CombatSkillDocument>,
    @InjectModel(Plant.name) private plantModel: Model<PlantDocument>,
    @InjectModel(CraftingRecipe.name)
    private craftingRecipeModel: Model<CraftingRecipeDocument>,
    @InjectModel(ResourceConfig.name)
    private resourceConfigModel: Model<ResourceConfigDocument>,
    @InjectModel(Material.name)
    private materialModel: Model<MaterialDocument>,
  ) {}

  async create(createItemDto: CreateItemDto): Promise<Item> {
    const existing = await this.itemModel.findOne({ itemID: createItemDto.itemID }).exec();
    if (existing) {
      throw new ConflictException(`Item with itemID "${createItemDto.itemID}" already exists`);
    }

    await this.validateWeaponLinkedSkill(createItemDto.itemType, createItemDto.linkedSkillId);
    await this.validateMaterialReferencesOnCreate(createItemDto);

    const item = new this.itemModel(createItemDto);
    return item.save();
  }

  /** Returns the catalog payload expected by the Unity client:
   *  { items: [...] }  */
  async getCatalog(): Promise<{ items: Item[] }> {
    const items = await this.itemModel.find().exec();
    return { items };
  }

  async getCatalogByItemType(itemType: number): Promise<{ items: Item[] }> {
    const items = await this.itemModel.find({ itemType }).exec();
    return { items };
  }

  async findAll(): Promise<Item[]> {
    return this.itemModel.find().exec();
  }

  async findAllByItemType(itemType: number): Promise<Item[]> {
    return this.itemModel.find({ itemType }).exec();
  }

  async findById(id: string): Promise<Item | null> {
    return this.itemModel.findById(id).exec();
  }

  async findByIdAndItemType(id: string, itemType: number): Promise<Item | null> {
    return this.itemModel.findOne({ _id: id, itemType }).exec();
  }

  async findByItemID(itemID: string): Promise<Item | null> {
    return this.itemModel.findOne({ itemID }).exec();
  }

  async findByItemIDAndItemType(itemID: string, itemType: number): Promise<Item | null> {
    return this.itemModel.findOne({ itemID, itemType }).exec();
  }

  async update(itemID: string, dto: UpdateItemDto): Promise<Item> {
    const existing = await this.itemModel.findOne({ itemID }).exec();
    if (!existing) throw new NotFoundException(`Item with itemID "${itemID}" not found`);

    const effectiveItemType = dto.itemType !== undefined ? dto.itemType : existing.itemType;
    const effectiveLinkedSkillId = dto.linkedSkillId !== undefined
      ? dto.linkedSkillId
      : existing.linkedSkillId;
    await this.validateWeaponLinkedSkill(effectiveItemType, effectiveLinkedSkillId);

    const updated = await this.itemModel
      .findOneAndUpdate({ itemID }, { $set: dto }, { new: true })
      .exec();

    return updated;
  }

  async updateByItemType(
    itemID: string,
    itemType: number,
    dto: UpdateItemDto,
    entityName = 'Item',
  ): Promise<Item> {
    const updated = await this.itemModel
      .findOneAndUpdate(
        { itemID, itemType },
        { $set: { ...dto, itemType } },
        { new: true },
      )
      .exec();
    if (!updated) {
      throw new NotFoundException(`${entityName} with itemID "${itemID}" not found`);
    }
    return updated;
  }

  async delete(itemID: string): Promise<Item | null> {
    const existing = await this.itemModel.findOne({ itemID }).exec();
    if (!existing) throw new NotFoundException(`Item with itemID "${itemID}" not found`);

    await this.assertItemNotInUse(itemID, 'Item');

    return this.itemModel.findOneAndDelete({ itemID }).exec();
  }

  async deleteByItemType(
    itemID: string,
    itemType: number,
    entityName = 'Item',
  ): Promise<Item | null> {
    const existing = await this.itemModel.findOne({ itemID, itemType }).exec();
    if (!existing) {
      throw new NotFoundException(`${entityName} with itemID "${itemID}" not found`);
    }

    await this.assertItemNotInUse(itemID, entityName);

    return this.itemModel.findOneAndDelete({ itemID, itemType }).exec();
  }

  async createFertilizer(createItemDto: CreateItemDto): Promise<Item> {
    return this.create(this.normalizeFertilizerCreateDto(createItemDto));
  }

  async getFertilizerCatalog(): Promise<{ items: Item[] }> {
    return this.getCatalogByItemType(FERTILIZER_ITEM_TYPE);
  }

  async findAllFertilizers(): Promise<Item[]> {
    return this.findAllByItemType(FERTILIZER_ITEM_TYPE);
  }

  async findFertilizerById(id: string): Promise<Item | null> {
    return this.findByIdAndItemType(id, FERTILIZER_ITEM_TYPE);
  }

  async findFertilizerByItemID(itemID: string): Promise<Item | null> {
    return this.findByItemIDAndItemType(itemID, FERTILIZER_ITEM_TYPE);
  }

  async updateFertilizer(itemID: string, dto: UpdateItemDto): Promise<Item> {
    return this.updateByItemType(
      itemID,
      FERTILIZER_ITEM_TYPE,
      this.normalizeFertilizerUpdateDto(dto),
      'Fertilizer',
    );
  }

  async deleteFertilizer(itemID: string): Promise<Item | null> {
    return this.deleteByItemType(itemID, FERTILIZER_ITEM_TYPE, 'Fertilizer');
  }

  private normalizeFertilizerCreateDto(dto: CreateItemDto): CreateItemDto {
    return this.stripNonFertilizerFields({
      ...dto,
      itemType: FERTILIZER_ITEM_TYPE,
    });
  }

  private normalizeFertilizerUpdateDto(dto: UpdateItemDto): UpdateItemDto {
    return this.stripNonFertilizerFields({
      ...dto,
      itemType: FERTILIZER_ITEM_TYPE,
    });
  }

  private stripNonFertilizerFields<T extends CreateItemDto | UpdateItemDto>(dto: T): T {
    return {
      ...dto,
      plantId: undefined,
      toolType: undefined,
      toolLevel: undefined,
      toolPower: undefined,
      toolMaterialId: undefined,
      sourcePlantId: undefined,
      pollinationSuccessChance: undefined,
      viabilityDays: undefined,
      crossResults: undefined,
      viableRestore: undefined,
      healthRestore: undefined,
      damage: undefined,
      critChance: undefined,
      weaponMaterialId: undefined,
      weaponType: undefined,
      tier: undefined,
      attackCooldown: undefined,
      knockbackForce: undefined,
      projectileSpeed: undefined,
      projectileRange: undefined,
      projectileKnockback: undefined,
      linkedSkillId: undefined,
      weaponPrefabKey: undefined,
      difficulty: undefined,
      fishingSeasons: undefined,
      isLegendary: undefined,
      foragingSeasons: undefined,
      isOre: undefined,
      requiresSmelting: undefined,
      smeltedResultId: undefined,
      isUniversalLike: undefined,
      isUniversalLove: undefined,
      relatedQuestID: undefined,
      autoConsume: undefined,
    };
  }

  private async validateWeaponLinkedSkill(
    itemType: number,
    linkedSkillId?: string,
  ): Promise<void> {
    const normalized = typeof linkedSkillId === 'string' ? linkedSkillId.trim() : '';
    if (!normalized) return;

    if (itemType !== WEAPON_ITEM_TYPE) {
      throw new BadRequestException('linkedSkillId is only allowed for Weapon items');
    }

    const skill = await this.combatSkillModel.findOne({ skillId: normalized }).lean().exec();
    if (!skill) {
      throw new BadRequestException(`linkedSkillId '${normalized}' does not exist`);
    }

    if (skill.ownership !== CombatSkillOwnership.WeaponSkill) {
      throw new BadRequestException(
        `linkedSkillId '${normalized}' must reference a WeaponSkill ownership entry`,
      );
    }
  }

  private async validateMaterialReferencesOnCreate(dto: CreateItemDto): Promise<void> {
    const toolMaterialId = typeof dto.toolMaterialId === 'string' ? dto.toolMaterialId.trim() : '';
    const weaponMaterialId = typeof dto.weaponMaterialId === 'string' ? dto.weaponMaterialId.trim() : '';

    if (dto.itemType === TOOL_ITEM_TYPE && toolMaterialId) {
      const material = await this.materialModel.findOne({ materialId: toolMaterialId }).lean().exec();
      if (!material) {
        throw new BadRequestException(`toolMaterialId '${toolMaterialId}' does not exist`);
      }
    }

    if (dto.itemType === WEAPON_ITEM_TYPE && weaponMaterialId) {
      const material = await this.materialModel.findOne({ materialId: weaponMaterialId }).lean().exec();
      if (!material) {
        throw new BadRequestException(`weaponMaterialId '${weaponMaterialId}' does not exist`);
      }
    }
  }

  private async assertItemNotInUse(itemID: string, entityName: string): Promise<void> {
    type UsageEntry = {
      location: string;
      count: number;
      examples: string[];
    };

    const usages: UsageEntry[] = [];

    const [
      plantHarvestCount,
      plantPollenCount,
      recipeResultCount,
      recipeIngredientCount,
      resourceDropCount,
      smeltedResultCount,
    ] = await Promise.all([
      this.plantModel.countDocuments({ harvestedItemId: itemID }).exec(),
      this.plantModel.countDocuments({ pollenItemId: itemID }).exec(),
      this.craftingRecipeModel.countDocuments({ resultItemId: itemID }).exec(),
      this.craftingRecipeModel.countDocuments({ 'ingredients.itemId': itemID }).exec(),
      this.resourceConfigModel.countDocuments({ 'dropTable.itemId': itemID }).exec(),
      this.itemModel.countDocuments({ smeltedResultId: itemID, itemID: { $ne: itemID } }).exec(),
    ]);

    if (plantHarvestCount > 0) {
      const examples = await this.plantModel
        .find({ harvestedItemId: itemID })
        .select('plantId')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'Plant.harvestedItemId',
        count: plantHarvestCount,
        examples: examples.map((doc: any) => doc.plantId),
      });
    }

    if (plantPollenCount > 0) {
      const examples = await this.plantModel
        .find({ pollenItemId: itemID })
        .select('plantId')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'Plant.pollenItemId',
        count: plantPollenCount,
        examples: examples.map((doc: any) => doc.plantId),
      });
    }

    if (recipeResultCount > 0) {
      const examples = await this.craftingRecipeModel
        .find({ resultItemId: itemID })
        .select('recipeID')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'CraftingRecipe.resultItemId',
        count: recipeResultCount,
        examples: examples.map((doc: any) => doc.recipeID),
      });
    }

    if (recipeIngredientCount > 0) {
      const examples = await this.craftingRecipeModel
        .find({ 'ingredients.itemId': itemID })
        .select('recipeID')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'CraftingRecipe.ingredients.itemId',
        count: recipeIngredientCount,
        examples: examples.map((doc: any) => doc.recipeID),
      });
    }

    if (resourceDropCount > 0) {
      const examples = await this.resourceConfigModel
        .find({ 'dropTable.itemId': itemID })
        .select('resourceId')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'ResourceConfig.dropTable.itemId',
        count: resourceDropCount,
        examples: examples.map((doc: any) => doc.resourceId),
      });
    }

    if (smeltedResultCount > 0) {
      const examples = await this.itemModel
        .find({ smeltedResultId: itemID, itemID: { $ne: itemID } })
        .select('itemID')
        .limit(5)
        .lean()
        .exec();
      usages.push({
        location: 'Item.smeltedResultId',
        count: smeltedResultCount,
        examples: examples.map((doc: any) => doc.itemID),
      });
    }

    if (usages.length === 0) {
      return;
    }

    const usageSummary = usages
      .map(
        (usage) =>
          `${usage.location} (${usage.count}) [${usage.examples.join(', ')}${usage.count > usage.examples.length ? ', ...' : ''}]`,
      )
      .join('; ');

    throw new ConflictException(
      `Cannot delete ${entityName} "${itemID}" because it is currently used in other collections: ${usageSummary}`,
    );
  }

}
