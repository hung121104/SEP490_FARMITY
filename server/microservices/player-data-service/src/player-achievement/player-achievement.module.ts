import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { PlayerAchievement, PlayerAchievementSchema } from './player-achievement.schema';
import { PlayerAchievementService } from './player-achievement.service';
import { PlayerAchievementController } from './player-achievement.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: PlayerAchievement.name, schema: PlayerAchievementSchema },
    ]),
    ClientsModule.register([
      {
        name: 'ADMIN_SERVICE',
        transport: Transport.TCP,
        options: {
          host: process.env.ADMIN_SERVICE_HOST || 'localhost',
          port: parseInt(process.env.ADMIN_SERVICE_PORT || '3006'),
        },
      },
    ]),
  ],
  controllers: [PlayerAchievementController],
  providers: [PlayerAchievementService],
})
export class PlayerAchievementModule {}