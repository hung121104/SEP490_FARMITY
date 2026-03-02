import { Module, NestModule, MiddlewareConsumer, RequestMethod } from '@nestjs/common';
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
        { path: 'player-data/worlds', method: RequestMethod.ALL },
        { path: 'player-data/worlds/:worldId/characters/:accountId', method: RequestMethod.GET },
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
        { path: 'game-data/items/:id', method: RequestMethod.DELETE },
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
        { path: 'game-data/items/:id', method: RequestMethod.DELETE },
      );
  }
}