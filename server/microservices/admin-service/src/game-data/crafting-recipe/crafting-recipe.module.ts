import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { CraftingRecipe, CraftingRecipeSchema } from './crafting-recipe.schema';
import { Item, ItemSchema } from '../item/item.schema';
import { CraftingRecipeService } from './crafting-recipe.service';
import { CraftingRecipeController } from './crafting-recipe.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: CraftingRecipe.name, schema: CraftingRecipeSchema },
      // Needed by CraftingRecipeService to validate item IDs
      { name: Item.name, schema: ItemSchema },
    ]),
  ],
  controllers: [CraftingRecipeController],
  providers: [CraftingRecipeService],
  exports: [CraftingRecipeService],
})
export class CraftingRecipeModule {}
