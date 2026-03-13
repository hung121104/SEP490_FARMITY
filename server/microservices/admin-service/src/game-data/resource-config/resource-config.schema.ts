import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type ResourceConfigDocument = ResourceConfig & Document;

@Schema({ _id: false })
export class ResourceDropEntry {
  @Prop({ required: true })
  itemId: string;

  @Prop({ required: true, min: 1 })
  minAmount: number;

  @Prop({ required: true, min: 1 })
  maxAmount: number;

  @Prop({ required: true, min: 0, max: 1 })
  dropChance: number;
}

@Schema({ timestamps: true })
export class ResourceConfig {
  @Prop({ required: true, unique: true })
  resourceId: string;

  @Prop({ required: true })
  name: string;

  @Prop({ required: true, min: 1 })
  maxHp: number;

  @Prop({ default: null })
  requiredToolId: string | null;

  @Prop({ default: null })
  spriteUrl: string | null;

  @Prop({ default: 'tree' })
  collisionType: string;

  @Prop({ type: [ResourceDropEntry], default: [] })
  dropTable: ResourceDropEntry[];
}

export const ResourceConfigSchema = SchemaFactory.createForClass(ResourceConfig);
ResourceConfigSchema.index({ resourceId: 1 }, { unique: true });
