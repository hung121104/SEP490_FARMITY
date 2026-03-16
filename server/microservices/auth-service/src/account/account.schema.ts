import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type AccountDocument = Account & Document;

export class AccountAchievementProgress {
  @Prop({ type: [Number], default: [] })
  progress: number[];

  @Prop({ default: null })
  achievedAt: Date | null;
}


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

  @Prop({
    type: Map,
    of: {
      progress: { type: [Number], default: [] },
      achievedAt: { type: Date, default: null },
    },
    default: () => new Map(),
  })
  achievementProgress: Map<string, AccountAchievementProgress>;
}

export const AccountSchema = SchemaFactory.createForClass(Account);