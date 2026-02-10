import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type NewsDocument = News & Document;

@Schema({ timestamps: true })
export class News {
  @Prop({ required: true })
  title: string;

  @Prop({ required: true })
  content: string;

  @Prop()
  thumbnailUrl?: string;

  @Prop({ default: Date.now })
  publishDate: Date;
}

export const NewsSchema = SchemaFactory.createForClass(News);
