import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { World, WorldSchema } from './world.schema';
import { WorldService } from './world.service';
import { WorldController } from './world.controller';
import { CharacterModule } from '../character/character.module';

@Module({
  imports: [
    MongooseModule.forFeature([{ name: World.name, schema: WorldSchema }]),
    CharacterModule,
    ClientsModule.register([
      {
        name: 'AUTH_SERVICE',
        transport: Transport.TCP,
        options: { host: 'localhost', port: 8877 },
      },
    ]),
  ],
  controllers: [WorldController],
  providers: [WorldService],
})
export class WorldModule {}
