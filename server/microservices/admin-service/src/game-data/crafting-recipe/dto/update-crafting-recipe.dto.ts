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

  /** Minimum structure tier required: 0=Wood, 1=Bronze, 2=Iron, 3=Gold */
  @IsOptional()
  @IsInt()
  @Min(0)
  recipeLevel?: number;

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
