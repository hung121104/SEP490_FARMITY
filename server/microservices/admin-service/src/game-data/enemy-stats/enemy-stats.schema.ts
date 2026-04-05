import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type EnemyStatsDocument = EnemyStats & Document;

@Schema({ timestamps: true })
export class EnemyStats {
  @Prop({ required: true, unique: true })
  enemyId: string;

  @Prop({ required: true })
  enemyName: string;

  @Prop({ default: 20 })
  respawnDelaySeconds: number;

  @Prop({ default: 10 })
  maxHealth: number;

  @Prop({ default: 1 })
  damageAmount: number;

  @Prop({ default: 10 })
  baseExp: number;

  @Prop({ default: 30 })
  knockbackForce: number;

  @Prop({ default: true })
  enableOutOfCombatRegen: boolean;

  @Prop({ default: 10 })
  regenDelaySeconds: number;

  @Prop({ default: 2 })
  regenHpPerSecond: number;

  @Prop({ default: true })
  regenRequireNearGuardAnchor: boolean;

  @Prop({ default: 1.5 })
  regenGuardProximity: number;

  @Prop({ default: 2 })
  moveSpeed: number;

  @Prop({ default: 3 })
  chaseSpeed: number;

  @Prop({ default: 1 })
  wanderSpeed: number;

  @Prop({ default: 5 })
  wanderRange: number;

  @Prop({ default: true })
  enableSeparation: boolean;

  @Prop({ default: 0.8 })
  separationRadius: number;

  @Prop({ default: 2.5 })
  separationForce: number;

  @Prop({ default: 8 })
  detectionRange: number;

  @Prop({ default: 1.5 })
  attackRange: number;

  @Prop({ default: 120 })
  fieldOfViewAngle: number;

  @Prop({ default: 2 })
  guardDuration: number;

  @Prop({ default: 1 })
  guardLookDuration: number;

  @Prop({ default: 0.5 })
  damageThrottleTime: number;

  @Prop({ default: true })
  useActiveAttack: boolean;

  @Prop({ default: 1.2 })
  attackCooldown: number;

  @Prop({ default: 0.1 })
  attackRecovery: number;

  @Prop({ default: 0.25 })
  attackFrontDotThreshold: number;

  @Prop({ default: 0.3 })
  knockbackDuration: number;

  @Prop({ default: 0.05 })
  squashPixels: number;

  @Prop({ default: 0.05 })
  stretchPixels: number;

  @Prop({ default: 0.3 })
  waveDuration: number;

  @Prop({ default: 0.2 })
  flashDuration: number;

  @Prop({ default: 2 })
  flashCount: number;
}

export const EnemyStatsSchema = SchemaFactory.createForClass(EnemyStats);
EnemyStatsSchema.index({ enemyId: 1 }, { unique: true });
