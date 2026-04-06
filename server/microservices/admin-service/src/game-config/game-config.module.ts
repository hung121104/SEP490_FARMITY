import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { GameConfig, GameConfigSchema } from './game-config.schema';
import { GameConfigService } from './game-config.service';
import { GameConfigController } from './game-config.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: GameConfig.name, schema: GameConfigSchema },
    ]),
  ],
  controllers: [GameConfigController],
  providers: [GameConfigService],
  exports: [GameConfigService],
})
export class GameConfigModule {}
