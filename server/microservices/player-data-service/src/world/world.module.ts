import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { World, WorldSchema } from './world.schema';
import { Chunk, ChunkSchema } from './chunk.schema';
import { ChestInventory, ChestInventorySchema } from './chest.schema';
import { WorldService } from './world.service';
import { WorldController } from './world.controller';
import { CharacterModule } from '../character/character.module';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: World.name, schema: WorldSchema },
      { name: Chunk.name, schema: ChunkSchema },
      { name: ChestInventory.name, schema: ChestInventorySchema },
    ]),
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
