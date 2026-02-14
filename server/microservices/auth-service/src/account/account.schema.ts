import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type AccountDocument = Account & Document;

@Schema()
export class GameSettings {
  @Prop({ default: true })
  audio: boolean;

  @Prop({ type: Object, default: { moveup: 'w', attack: 'Left_Click' } })
  keyBinds: Record<string, string>;
}

export const GameSettingsSchema = SchemaFactory.createForClass(GameSettings);

@Schema()
export class Account {
  @Prop({ required: true, unique: true })
  username: string;

  @Prop({ required: true })
  password: string;

  @Prop({ required: true, unique: true })
  email: string;

  @Prop({ type: GameSettingsSchema, default: () => ({}) })
  gameSettings: GameSettings;

  @Prop({ default: false })
  isAdmin: boolean;

  @Prop()
  resetOtpHash?: string;

  @Prop()
  resetOtpExpiresAt?: Date;

  @Prop({ default: false })
  resetOtpUsed?: boolean;

  @Prop()
  resetOtpRequestedAt?: Date;
}

export const AccountSchema = SchemaFactory.createForClass(Account);