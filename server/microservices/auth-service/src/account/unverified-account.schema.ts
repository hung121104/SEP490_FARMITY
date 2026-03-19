import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type UnverifiedAccountDocument = UnverifiedAccount & Document;

@Schema({ timestamps: true })
export class UnverifiedAccount {
  @Prop({ required: true, unique: true })
  email: string;

  @Prop({ required: true })
  username: string;

  @Prop({ required: true })
  passwordHash: string;

  @Prop({ required: true })
  verifyOtpHash: string;

  @Prop({ type: Date, default: Date.now, expires: 600 })
  createdAt: Date;
}

export const UnverifiedAccountSchema =
  SchemaFactory.createForClass(UnverifiedAccount);
