import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type PlayerAchievementDocument = PlayerAchievement & Document;

@Schema({ timestamps: true })
export class PlayerAchievement {
  @Prop({ type: Types.ObjectId, ref: 'Account', required: true })
  accountId: Types.ObjectId;

  @Prop({ required: true })
  achievementId: string;

  /** One number per requirement, index-matched. All zeros when first created. */
  @Prop({ type: [Number], default: [] })
  progress: number[];

  /** Null until all requirements are met. Cannot be un-set. */
  @Prop({ default: null })
  achievedAt: Date | null;
}

export const PlayerAchievementSchema = SchemaFactory.createForClass(PlayerAchievement);
PlayerAchievementSchema.index({ accountId: 1, achievementId: 1 }, { unique: true });