import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type CatalogVersionDocument = CatalogVersion & Document;

@Schema({ timestamps: true })
export class CatalogVersion {
  @Prop({ required: true, unique: true })
  configKey: string;

  @Prop({ default: 0 })
  version: number;
}

export const CatalogVersionSchema =
  SchemaFactory.createForClass(CatalogVersion);
