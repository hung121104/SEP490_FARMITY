export class UpdateCraftingRecipeDto {
  recipeName?: string;
  description?: string;
  recipeType?: number;
  category?: number;
  resultItemId?: string;
  resultQuantity?: number;
  resultQuality?: number;
  ingredients?: { itemId: string; quantity: number }[];
  isUnlockedByDefault?: boolean;
}
