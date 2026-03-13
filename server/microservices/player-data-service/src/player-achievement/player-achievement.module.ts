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
        options: { host: 'localhost', port: 3006 },
      },
    ]),
  ],
  controllers: [PlayerAchievementController],
  providers: [PlayerAchievementService],
})
export class PlayerAchievementModule {}