import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type AccountDocument = Account & Document;


@Schema()
export class Account {
  @Prop({ required: true, unique: true })
  username: string;

  @Prop({ required: true })
  password: string;

  @Prop({ required: true, unique: true })
  email: string;


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