import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type QuestDocument = Quest & Document;

@Schema({ _id: false })
export class QuestReward {
  @Prop({ required: true })
  itemId: string;

  @Prop({ required: true, min: 1 })
  quantity: number;
}

export const QuestRewardSchema = SchemaFactory.createForClass(QuestReward);

@Schema({ _id: false })
export class QuestObjective {
  @Prop({ required: true })
  objectiveId: string;

  @Prop({ required: true })
  description: string;

  @Prop({ required: true })
  itemId: string;

  @Prop({ required: true, min: 1 })
  requiredAmount: number;

  @Prop({ required: true, default: 0 })
  currentAmount: number;
}

export const QuestObjectiveSchema = SchemaFactory.createForClass(QuestObjective);

@Schema({ timestamps: true })
export class Quest {
  @Prop({ required: true, unique: true })
  questId: string;

  @Prop({ required: true })
  questName: string;

  @Prop({ required: true })
  description: string;

  @Prop({ required: true })
  NPCName: string;

  /** Sorting/priority weight for the quest */
  @Prop({ required: true, default: 1 })
  Weight: number;

  /** Id of the next quest in the chain (optional) */
  @Prop()
  nextQuestId?: string;

  @Prop({ type: QuestRewardSchema, required: true })
  reward: QuestReward;

  /** 'inactive' | 'active' | 'completed' | 'failed' */
  @Prop({ required: true, default: 'inactive' })
  status: string;

  @Prop({ type: [QuestObjectiveSchema], default: [] })
  objectives: QuestObjective[];
}

export const QuestSchema = SchemaFactory.createForClass(Quest);
