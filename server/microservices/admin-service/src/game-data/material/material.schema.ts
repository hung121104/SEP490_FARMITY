import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type MaterialDocument = Material & Document;

/**
 * Represents a tool/weapon composition material (e.g. Copper, Steel, Diamond).
 *
 *  materialId     – stable string key referenced by tool/weapon item fields
 *                   (e.g. 'mat_copper', 'mat_steel').
 *                   Unity MaterialCatalogService registers the spritesheet into
 *                   SkinCatalogManager under this same key, so DynamicSpriteSwapper
 *                   can look it up as a configId directly.
 *
 *  materialName   – human-readable name shown in the admin panel and UI.
 *  materialTier   – numeric tier for future stat scaling (1=Basic, 2=Copper, 3=Steel…).
 *  spritesheetUrl – Cloudinary URL of the tool animation PNG for this material.
 *                   One sheet covers all tool animations (hoe, pickaxe, axe, rod…).
 *  cellSize       – uniform sprite cell width/height in pixels. Default 64.
 *  description    – optional flavour text.
 */
@Schema({ timestamps: true })
export class Material {
  @Prop({ required: true, unique: true })
  materialId: string;

  @Prop({ required: true })
  materialName: string;

  @Prop({ required: true, default: 1 })
  materialTier: number;

  @Prop({ required: true })
  spritesheetUrl: string;

  @Prop({ required: true, default: 64 })
  cellSize: number;

  @Prop({ default: '' })
  description: string;
}

export const MaterialSchema = SchemaFactory.createForClass(Material);
MaterialSchema.index({ materialId: 1 }, { unique: true });
MaterialSchema.index({ materialTier: 1 });
