import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { EnemyStats, EnemyStatsSchema } from './enemy-stats.schema';
import { EnemyStatsController } from './enemy-stats.controller';
import { EnemyStatsService } from './enemy-stats.service';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: EnemyStats.name, schema: EnemyStatsSchema },
    ]),
  ],
  controllers: [EnemyStatsController],
  providers: [EnemyStatsService],
  exports: [EnemyStatsService],
})
export class EnemyStatsModule {}
