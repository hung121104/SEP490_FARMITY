import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type WorldDocument = World & Document;

@Schema()
export class World {
  // use MongoDB `_id` as the primary identifier
  @Prop({ required: true })
  worldName: string;

  @Prop({ type: Types.ObjectId, ref: 'Account', required: true })
  ownerId: Types.ObjectId;
}

export const WorldSchema = SchemaFactory.createForClass(World);
// `_id` is the primary key; no separate world_id index required
// Ensure a single account cannot create two worlds with the same name
WorldSchema.index({ ownerId: 1, worldName: 1 }, { unique: true });
