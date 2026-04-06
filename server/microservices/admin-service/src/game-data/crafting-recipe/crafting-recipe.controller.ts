import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CraftingRecipeService } from './crafting-recipe.service';
import { CreateCraftingRecipeDto } from './dto/create-crafting-recipe.dto';
import { UpdateCraftingRecipeDto } from './dto/update-crafting-recipe.dto';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class CraftingRecipeController {
  constructor(private readonly craftingRecipeService: CraftingRecipeService) {}

  /** Create a new crafting recipe.
   *  Validates that resultItemId and all ingredient itemIds exist in the DB. */
  @MessagePattern('create-crafting-recipe')
  async createCraftingRecipe(
    @Payload() dto: CreateCraftingRecipeDto,
  ): Promise<CatalogChange> {
    const data = await this.craftingRecipeService.create(dto);
    return {
      data,
      changeType: 'create',
      entityType: 'recipe',
    };
  }

  /** Return full catalog: { recipes: [...] } — consumed by Unity client */
  @MessagePattern('get-crafting-recipe-catalog')
  async getCraftingRecipeCatalog() {
    return this.craftingRecipeService.getCatalog();
  }

  /** Return flat array of all crafting recipes */
  @MessagePattern('get-all-crafting-recipes')
  async getAllCraftingRecipes() {
    return this.craftingRecipeService.findAll();
  }

  /** Find one crafting recipe by MongoDB _id */
  @MessagePattern('get-crafting-recipe-by-id')
  async getCraftingRecipeById(@Payload() id: string) {
    return this.craftingRecipeService.findById(id);
  }

  /** Find one crafting recipe by game-side recipeID string */
  @MessagePattern('get-crafting-recipe-by-recipe-id')
  async getCraftingRecipeByRecipeId(@Payload() recipeID: string) {
    return this.craftingRecipeService.findByRecipeID(recipeID);
  }

  /** Update a crafting recipe by game-side recipeID.
   *  Validates any updated item IDs against the DB. */
  @MessagePattern('update-crafting-recipe')
  async updateCraftingRecipe(
    @Payload() payload: { recipeID: string; dto: UpdateCraftingRecipeDto },
  ): Promise<CatalogChange> {
    const data = await this.craftingRecipeService.update(
      payload.recipeID,
      payload.dto,
    );
    return {
      data,
      changeType: 'update',
      entityType: 'recipe',
    };
  }

  /** Delete a crafting recipe by game-side recipeID string */
  @MessagePattern('delete-crafting-recipe')
  async deleteCraftingRecipe(
    @Payload() recipeID: string,
  ): Promise<CatalogChange> {
    const data = await this.craftingRecipeService.delete(recipeID);
    return {
      data,
      changeType: 'delete',
      entityType: 'recipe',
    };
  }
}
