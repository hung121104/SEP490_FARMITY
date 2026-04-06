import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';
import { RequirementType } from './requirement-type.enum';

export type AchievementDocument = Achievement & Document;

export class AchievementRequirement {
  @Prop({ required: true, enum: RequirementType })
  type: RequirementType;

  @Prop({ required: true })
  target: number;

  @Prop()
  entityId?: string;

  @Prop({ required: true })
  label: string;
}

@Schema({ timestamps: true })
export class Achievement {
  @Prop({ required: true, unique: true })
  achievementId: string;

  @Prop({ required: true })
  name: string;

  @Prop({ required: true })
  description: string;

  @Prop({
    type: [
      {
        type: { type: String, enum: RequirementType, required: true },
        target: { type: Number, required: true },
        entityId: { type: String },
        label: { type: String, required: true },
      },
    ],
    default: [],
  })
  requirements: AchievementRequirement[];
}

export const AchievementSchema = SchemaFactory.createForClass(Achievement);