import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type WorldEntitiesDocument = WorldEntities & Document;

@Schema({ _id: false })
export class EnemySpawnerActiveEnemy {
  @Prop({ required: true })
  runtimeId: string;

  @Prop({ required: true })
  enemyId: string;

  @Prop({ required: true })
  x: number;

  @Prop({ required: true })
  y: number;

  @Prop({ required: true })
  z: number;
}

export const EnemySpawnerActiveEnemySchema = SchemaFactory.createForClass(EnemySpawnerActiveEnemy);

@Schema({ _id: false })
export class EnemySpawnerPendingRespawn {
  @Prop({ required: true })
  enemyId: string;

  @Prop({ required: true })
  dueUnixMs: number;
}

export const EnemySpawnerPendingRespawnSchema = SchemaFactory.createForClass(EnemySpawnerPendingRespawn);

@Schema({ _id: false })
export class EnemySpawnerState {
  @Prop({ type: Number, default: 0 })
  runtimeSequence: number;

  @Prop({ type: [EnemySpawnerActiveEnemySchema], default: [] })
  active: EnemySpawnerActiveEnemy[];

  @Prop({ type: [EnemySpawnerPendingRespawnSchema], default: [] })
  pending: EnemySpawnerPendingRespawn[];
}

export const EnemySpawnerStateSchema = SchemaFactory.createForClass(EnemySpawnerState);

@Schema({ collection: 'WorldEntities' })
export class WorldEntities {
  @Prop({ type: Types.ObjectId, ref: 'World', required: true, unique: true, index: true })
  worldId: Types.ObjectId;

  @Prop({ type: EnemySpawnerStateSchema, default: null })
  enemySpawnerState?: EnemySpawnerState;
}

export const WorldEntitiesSchema = SchemaFactory.createForClass(WorldEntities);
