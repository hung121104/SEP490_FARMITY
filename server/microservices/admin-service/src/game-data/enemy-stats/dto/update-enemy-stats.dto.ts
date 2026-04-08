import { IsBoolean, IsNumber, IsOptional, IsString, Max, Min } from 'class-validator';

export class UpdateEnemyStatsDto {
  @IsOptional()
  @IsString()
  enemyName?: string;

  @IsOptional()
  @IsNumber()
  @Min(0)
  respawnDelaySeconds?: number;

  @IsOptional()
  @IsNumber()
  @Min(1)
  maxHealth?: number;

  @IsOptional()
  @IsNumber()
  @Min(1)
  damageAmount?: number;

  @IsOptional()
  @IsNumber()
  @Min(1)
  baseExp?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  knockbackForce?: number;

  @IsOptional()
  @IsBoolean()
  enableOutOfCombatRegen?: boolean;

  @IsOptional()
  @IsNumber()
  @Min(0)
  regenDelaySeconds?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  regenHpPerSecond?: number;

  @IsOptional()
  @IsBoolean()
  regenRequireNearGuardAnchor?: boolean;

  @IsOptional()
  @IsNumber()
  @Min(0)
  regenGuardProximity?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  moveSpeed?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  chaseSpeed?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  wanderSpeed?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  wanderRange?: number;

  @IsOptional()
  @IsBoolean()
  enableSeparation?: boolean;

  @IsOptional()
  @IsNumber()
  @Min(0)
  separationRadius?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  separationForce?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  detectionRange?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  attackRange?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  @Max(360)
  fieldOfViewAngle?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  guardDuration?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  guardLookDuration?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  damageThrottleTime?: number;

  @IsOptional()
  @IsBoolean()
  useActiveAttack?: boolean;

  @IsOptional()
  @IsNumber()
  @Min(0)
  attackCooldown?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  attackRecovery?: number;

  @IsOptional()
  @IsNumber()
  @Min(-1)
  @Max(1)
  attackFrontDotThreshold?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  knockbackDuration?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  squashPixels?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  stretchPixels?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  waveDuration?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  flashDuration?: number;

  @IsOptional()
  @IsNumber()
  @Min(0)
  flashCount?: number;
}
