import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type SkinConfigDocument = SkinConfig & Document;

/**
 * Represents one spritesheet entry in the Paper Doll skin catalog.
 *  configId       – stable string key used by Unity to look up this sheet
 *                   (e.g. 'farmer_base', 'gold_hoe', 'blonde_hair').
 *  spritesheetUrl – publicly accessible URL of the PNG (Cloudinary / CDN).
 *  cellSize       – width AND height of each cell in pixels (uniform grid).
 *                   Defaults to 64 (matches the game's standard 64×64 art).
 *  displayName    – human-readable label shown in the admin panel.
 *  layer          – which Paper Doll layer this sheet belongs to
 *                   ('body' | 'tool' | 'hair' | 'hat' | 'outfit' | …).
 *                   Optional metadata; Unity resolves layers by configId.
 */
@Schema({ timestamps: true })
export class SkinConfig {
  @Prop({ required: true, unique: true })
  configId: string;

  @Prop({ required: true })
  spritesheetUrl: string;

  @Prop({ required: true, default: 64 })
  cellSize: number;

  @Prop({ required: true })
  displayName: string;

  @Prop({ default: 'body' })
  layer: string;
}

export const SkinConfigSchema = SchemaFactory.createForClass(SkinConfig);
// Index used by Unity bulk-fetch (catalog query) and admin searches
SkinConfigSchema.index({ layer: 1 });
SkinConfigSchema.index({ configId: 1 }, { unique: true });
