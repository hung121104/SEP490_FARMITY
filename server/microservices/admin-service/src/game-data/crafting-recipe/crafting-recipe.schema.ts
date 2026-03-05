import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type CraftingRecipeDocument = CraftingRecipe & Document;

// ── Embedded sub-document ─────────────────────────────────────────────────────

export class RecipeIngredient {
  /** itemID (from ItemCatalog) of the required ingredient. */
  itemId: string;
  /** How many of this ingredient are needed. */
  quantity: number;
}

// ── Root document ─────────────────────────────────────────────────────────────

@Schema({ timestamps: true })
export class CraftingRecipe {
  // ── Identity ───────────────────────────────────────────────────────────────

  /** Unique game-side identifier, e.g. "recipe_iron_sword". */
  @Prop({ required: true, unique: true })
  recipeID: string;

  @Prop({ required: true })
  recipeName: string;

  @Prop({ required: true })
  description: string;

  // ── Classification ─────────────────────────────────────────────────────────

  /** 0 = Crafting, 1 = Cooking */
  @Prop({ required: true, default: 0 })
  recipeType: number;

  /** CraftingCategory enum cast from int. */
  @Prop({ required: true, default: 0 })
  category: number;

  // ── Result ─────────────────────────────────────────────────────────────────

  /** itemID (from ItemCatalog) of the crafted result. */
  @Prop({ required: true })
  resultItemId: string;

  @Prop({ required: true, default: 1 })
  resultQuantity: number;

  /** Quality enum cast from int. */
  @Prop({ required: true, default: 0 })
  resultQuality: number;

  // ── Ingredients ────────────────────────────────────────────────────────────

  @Prop({
    type: [
      {
        itemId:   { type: String, required: true },
        quantity: { type: Number, required: true },
      },
    ],
    default: [],
  })
  ingredients: RecipeIngredient[];

  // ── Unlock ─────────────────────────────────────────────────────────────────

  @Prop({ default: true })
  isUnlockedByDefault: boolean;
}

export const CraftingRecipeSchema = SchemaFactory.createForClass(CraftingRecipe);
