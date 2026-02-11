import { Module, NestModule, MiddlewareConsumer, RequestMethod } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { GatewayController } from './gateway.controller';
import { AuthMiddleware } from './auth.middleware';

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
})
export class GatewayModule implements NestModule {
  configure(consumer: MiddlewareConsumer) {
    // Exclude public auth routes (register/login) from the auth middleware
    consumer
      .apply(AuthMiddleware)
      .exclude(
        { path: 'auth/register', method: RequestMethod.ALL },
        { path: 'auth/login-ingame', method: RequestMethod.ALL },
        { path: 'auth/register-admin', method: RequestMethod.ALL },
        { path: 'auth/login-admin', method: RequestMethod.ALL },
        { path: 'auth/admin-reset/request', method: RequestMethod.ALL },
        { path: 'auth/admin-reset/confirm', method: RequestMethod.ALL },
      )
      .forRoutes('*');
  }
}