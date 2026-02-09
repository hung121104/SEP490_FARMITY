import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type MediaDocument = Media & Document;

@Schema({ timestamps: true })
export class Media {
  @Prop({ required: true })
  file_url: string;

  @Prop()
  description?: string;

  @Prop({ default: Date.now })
  upload_date: Date;
}

export const MediaSchema = SchemaFactory.createForClass(Media);