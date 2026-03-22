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

  @Prop({ required: true })
  spritesheetUrl: string;

  @Prop({ required: true, default: 64 })
  cellSize: number;

  @Prop({ required: true })
  displayName: string;
}

export const CombatCatalogSchema = SchemaFactory.createForClass(CombatCatalog);
CombatCatalogSchema.index({ type: 1 });
CombatCatalogSchema.index({ configId: 1 }, { unique: true });
