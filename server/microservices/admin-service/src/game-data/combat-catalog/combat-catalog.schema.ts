import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type CombatCatalogDocument = CombatCatalog & Document;

/** Represents one skill VFX tint config consumed by combat runtime. */
@Schema({ timestamps: true })
export class CombatCatalog {
  @Prop({ required: true, unique: true })
  configId: string;

  @Prop({ required: true, default: 'skill_vfx' })
  type: string;

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
