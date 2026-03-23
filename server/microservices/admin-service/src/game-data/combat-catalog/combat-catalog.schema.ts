import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type CombatCatalogDocument = CombatCatalog & Document;

/**
 * Represents one combat visual spritesheet entry consumed by combat runtime.
 * type examples: 'weapon', 'skill_vfx'
 */
@Schema({ timestamps: true })
export class CombatCatalog {
  @Prop({ required: true, unique: true })
  configId: string;

  @Prop({ required: true, default: 'weapon' })
  type: string;

  @Prop({ default: '' })
  spritesheetUrl: string;

  @Prop({ required: true, default: 64 })
  cellSize: number;

  @Prop({ required: true })
  displayName: string;

  @Prop({ default: '' })
  primaryColorHex: string;

  @Prop({ default: '' })
  secondaryColorHex: string;

  @Prop({ default: 1 })
  colorIntensity: number;

  @Prop({ default: 1 })
  tintAlpha: number;
}

export const CombatCatalogSchema = SchemaFactory.createForClass(CombatCatalog);
CombatCatalogSchema.index({ type: 1 });
CombatCatalogSchema.index({ configId: 1 }, { unique: true });
