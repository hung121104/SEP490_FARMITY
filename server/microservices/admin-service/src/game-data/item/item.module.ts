import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Item, ItemSchema } from './item.schema';
import { ItemService } from './item.service';
import { ItemController } from './item.controller';
import { CombatSkill, CombatSkillSchema } from '../combat-skill/combat-skill.schema';
import { Plant, PlantSchema } from '../plant/plant.schema';
import {
  CraftingRecipe,
  CraftingRecipeSchema,
} from '../crafting-recipe/crafting-recipe.schema';
import {
  ResourceConfig,
  ResourceConfigSchema,
} from '../resource-config/resource-config.schema';
import { Material, MaterialSchema } from '../material/material.schema';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: Item.name, schema: ItemSchema },
      { name: CombatSkill.name, schema: CombatSkillSchema },
      { name: Plant.name, schema: PlantSchema },
      { name: CraftingRecipe.name, schema: CraftingRecipeSchema },
      { name: ResourceConfig.name, schema: ResourceConfigSchema },
      { name: Material.name, schema: MaterialSchema },
    ]),
  ],
  controllers: [ItemController],
  providers: [ItemService],
  exports: [ItemService],
})
export class ItemModule {}
