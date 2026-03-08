import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type GameConfigDocument = GameConfig & Document;

@Schema({ timestamps: true })
export class GameConfig {
  @Prop({ required: true, unique: true })
  configKey: string;

  @Prop({ default: '' })
  currentBackgroundUrl: string;

  @Prop({ default: 0 })
  version: number;
}

export const GameConfigSchema = SchemaFactory.createForClass(GameConfig);
