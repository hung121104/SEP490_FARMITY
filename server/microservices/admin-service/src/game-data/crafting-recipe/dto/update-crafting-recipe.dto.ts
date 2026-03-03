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

export class UpdateIngredientDto {
  @IsString()
  @IsNotEmpty()
  itemId: string;

  @IsInt()
  @Min(1)
  quantity: number;
}

export class UpdateCraftingRecipeDto {
  @IsOptional()
  @IsString()
  @IsNotEmpty()
  recipeName?: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsInt()
  recipeType?: number;

  @IsOptional()
  @IsInt()
  category?: number;

  @IsOptional()
  @IsString()
  @IsNotEmpty()
  resultItemId?: string;

  @IsOptional()
  @IsInt()
  @Min(1)
  resultQuantity?: number;

  @IsOptional()
  @IsInt()
  resultQuality?: number;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => UpdateIngredientDto)
  ingredients?: UpdateIngredientDto[];

  @IsOptional()
  @IsBoolean()
  isUnlockedByDefault?: boolean;
}
