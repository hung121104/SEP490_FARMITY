import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type CharacterDocument = Character & Document;

@Schema()
export class Character {
  @Prop({ required: true })
  @Prop({ type: Types.ObjectId, ref: 'World', required: true })
  worldId: Types.ObjectId;

  @Prop({ type: Types.ObjectId, ref: 'Account', required: true })
  accountId: Types.ObjectId;

  @Prop({ required: true })
  positionX: number;

  @Prop({ required: true })
  positionY: number;

  @Prop({ required: true })
  sectionIndex: number;
}

export const CharacterSchema = SchemaFactory.createForClass(Character);
CharacterSchema.index({ worldId: 1, accountId: 1 }, { unique: true });