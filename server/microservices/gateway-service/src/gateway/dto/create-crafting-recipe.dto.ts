export class CreateCraftingRecipeDto {
  recipeID: string;
  recipeName: string;
  description: string;
  recipeType: number;
  category: number;
  resultItemId: string;
  resultQuantity: number;
  resultQuality: number;
  ingredients: { itemId: string; quantity: number }[];
  isUnlockedByDefault?: boolean;
}
