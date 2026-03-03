import {
  IsString,
  IsInt,
  IsBoolean,
  IsOptional,
  IsArray,
  IsNotEmpty,
  Min,
  ValidateNested,
} from 'class-validator';
import { Type } from 'class-transformer';

export class RecipeIngredientDto {
  @IsString()
  @IsNotEmpty()
  itemId: string;

  @IsInt()
  @Min(1)
  quantity: number;
}

export class CreateCraftingRecipeDto {
  // ── Identity ─────────────────────────────────────────────────────────────

  @IsString()
  @IsNotEmpty()
  recipeID: string;

  @IsString()
  @IsNotEmpty()
  recipeName: string;

  @IsString()
  @IsNotEmpty()
  description: string;

  // ── Classification ────────────────────────────────────────────────────────

  /** 0 = Crafting, 1 = Cooking */
  @IsInt()
  recipeType: number;

  /** CraftingCategory enum cast from int. */
  @IsInt()
  category: number;

  // ── Result ────────────────────────────────────────────────────────────────

  @IsString()
  @IsNotEmpty()
  resultItemId: string;

  @IsInt()
  @Min(1)
  resultQuantity: number;

  /** Quality enum cast from int. */
  @IsInt()
  resultQuality: number;

  // ── Ingredients ───────────────────────────────────────────────────────────

  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => RecipeIngredientDto)
  ingredients: RecipeIngredientDto[];

  // ── Unlock ────────────────────────────────────────────────────────────────

  @IsOptional()
  @IsBoolean()
  isUnlockedByDefault?: boolean;
}
