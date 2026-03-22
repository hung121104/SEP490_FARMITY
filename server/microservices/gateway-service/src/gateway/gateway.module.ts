import {
  Module,
  NestModule,
  MiddlewareConsumer,
  RequestMethod,
} from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { GatewayController } from './gateway.controller';
import { AuthorizationMiddleware } from './authorization.middleware';
import { AuthenticationMiddleware } from './authentication.middleware';
import { GatewayCloudinaryService } from './cloudinary.service';

@Module({
  imports: [
    ClientsModule.register([
      {
        name: 'AUTH_SERVICE',
        transport: Transport.TCP,
        options: { host: 'localhost', port: 8877 },
      },
      {
        name: 'PLAYER_DATA_SERVICE',
        transport: Transport.TCP,
        options: { host: 'localhost', port: 8878 },
      },
      {
        name: 'ADMIN_SERVICE',
        transport: Transport.TCP,
        options: { host: 'localhost', port: 3006 },
      },
    ]),
  ],
  controllers: [GatewayController],
  providers: [GatewayCloudinaryService],
})
export class GatewayModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    // attach user for all protected routes
    consumer
      .apply(AuthorizationMiddleware)
      .forRoutes(
        { path: 'auth/admin-check', method: RequestMethod.GET },
        { path: 'auth/logout', method: RequestMethod.POST },
        { path: 'player-data/world', method: RequestMethod.ALL },
        { path: 'player-data/world/blacklist', method: RequestMethod.ALL },
        { path: 'player-data/worlds', method: RequestMethod.ALL },
        {
          path: 'player-data/worlds/:worldId/characters/:accountId',
          method: RequestMethod.GET,
        },
        { path: 'player-data/dropped-items', method: RequestMethod.ALL },
        {
          path: 'player-data/dropped-items/:dropId',
          method: RequestMethod.ALL,
        },
        { path: 'blog/create', method: RequestMethod.POST },
        { path: 'blog/update/:id', method: RequestMethod.POST },
        { path: 'blog/delete/:id', method: RequestMethod.DELETE },
        { path: 'news/upload-signature', method: RequestMethod.POST },
        { path: 'news/create', method: RequestMethod.POST },
        { path: 'news/update/:id', method: RequestMethod.POST },
        { path: 'news/delete/:id', method: RequestMethod.DELETE },
        { path: 'media/upload-signature', method: RequestMethod.POST },
        { path: 'media/create', method: RequestMethod.POST },
        { path: 'media/update/:id', method: RequestMethod.POST },
        { path: 'media/delete/:id', method: RequestMethod.DELETE },
        { path: 'game-data/items/create', method: RequestMethod.POST },
        { path: 'game-data/items/:itemID', method: RequestMethod.PUT },
        { path: 'game-data/items/:itemID', method: RequestMethod.DELETE },
        { path: 'game-data/combat-skills/create', method: RequestMethod.POST },
        { path: 'game-data/combat-skills/:skillId', method: RequestMethod.PUT },
        { path: 'game-data/combat-skills/:skillId', method: RequestMethod.DELETE },
        { path: 'game-data/plants/create', method: RequestMethod.POST },
        { path: 'game-data/plants/:plantId', method: RequestMethod.PUT },
        { path: 'game-data/plants/:plantId', method: RequestMethod.DELETE },
        { path: 'game-config/main-menu', method: RequestMethod.PUT },
        { path: 'player-data/achievement', method: RequestMethod.ALL },
        { path: 'player-data/achievement/progress', method: RequestMethod.PUT },
        { path: 'player-data/achievement/progress/batch', method: RequestMethod.PUT },
        { path: 'player-data/combat/skill-loadout', method: RequestMethod.GET },
        { path: 'player-data/combat/skill-loadout', method: RequestMethod.PUT },
        { path: 'game-data/achievements/create', method: RequestMethod.POST },
        { path: 'game-data/achievements/:achievementId', method: RequestMethod.PUT },
        { path: 'game-data/achievements/:achievementId', method: RequestMethod.DELETE },
        { path: 'game-data/skin-configs', method: RequestMethod.POST },
        { path: 'game-data/skin-configs/:configId', method: RequestMethod.PUT },
        { path: 'game-data/skin-configs/:configId', method: RequestMethod.DELETE },
        { path: 'game-data/materials', method: RequestMethod.POST },
        { path: 'game-data/materials/:materialId', method: RequestMethod.PUT },
        { path: 'game-data/materials/:materialId', method: RequestMethod.DELETE },
        { path: 'game-data/quests', method: RequestMethod.POST },
        { path: 'game-data/quests/:questId', method: RequestMethod.PUT },
        { path: 'game-data/quests/:questId', method: RequestMethod.DELETE },
      );

    // enforce admin only on admin routes
    consumer
      .apply(AuthenticationMiddleware)
      .forRoutes(
        { path: 'auth/admin-check', method: RequestMethod.GET },
        { path: 'blog/create', method: RequestMethod.POST },
        { path: 'blog/update/:id', method: RequestMethod.POST },
        { path: 'blog/delete/:id', method: RequestMethod.DELETE },
        { path: 'news/upload-signature', method: RequestMethod.POST },
        { path: 'news/create', method: RequestMethod.POST },
        { path: 'news/update/:id', method: RequestMethod.POST },
        { path: 'news/delete/:id', method: RequestMethod.DELETE },
        { path: 'media/upload-signature', method: RequestMethod.POST },
        { path: 'media/create', method: RequestMethod.POST },
        { path: 'media/update/:id', method: RequestMethod.POST },
        { path: 'media/delete/:id', method: RequestMethod.DELETE },
        { path: 'game-data/items/create', method: RequestMethod.POST },
        { path: 'game-data/items/:itemID', method: RequestMethod.PUT },
        { path: 'game-data/items/:itemID', method: RequestMethod.DELETE },
        { path: 'game-data/combat-skills/create', method: RequestMethod.POST },
        { path: 'game-data/combat-skills/:skillId', method: RequestMethod.PUT },
        { path: 'game-data/combat-skills/:skillId', method: RequestMethod.DELETE },
        { path: 'game-data/plants/create', method: RequestMethod.POST },
        { path: 'game-data/plants/:plantId', method: RequestMethod.PUT },
        { path: 'game-data/plants/:plantId', method: RequestMethod.DELETE },
        { path: 'game-config/main-menu', method: RequestMethod.PUT },
        { path: 'game-data/achievements/create', method: RequestMethod.POST },
        { path: 'game-data/achievements/:achievementId', method: RequestMethod.PUT },
        { path: 'game-data/achievements/:achievementId', method: RequestMethod.DELETE },
        { path: 'game-data/skin-configs', method: RequestMethod.POST },
        { path: 'game-data/skin-configs/:configId', method: RequestMethod.PUT },
        { path: 'game-data/skin-configs/:configId', method: RequestMethod.DELETE },
        { path: 'game-data/materials', method: RequestMethod.POST },
        { path: 'game-data/materials/:materialId', method: RequestMethod.PUT },
        { path: 'game-data/materials/:materialId', method: RequestMethod.DELETE },
        { path: 'game-data/quests', method: RequestMethod.POST },
        { path: 'game-data/quests/:questId', method: RequestMethod.PUT },
        { path: 'game-data/quests/:questId', method: RequestMethod.DELETE },
      );
  }
}
