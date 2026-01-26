import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { GatewayController } from './gateway.controller';

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
    ]),
  ],
  controllers: [GatewayController],
})
export class GatewayModule {}