import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Plant, PlantSchema } from './plant.schema';
import { PlantService } from './plant.service';
import { PlantController } from './plant.controller';
import { Item, ItemSchema } from '../item/item.schema';

@Module({
  imports: [MongooseModule.forFeature([
    { name: Plant.name, schema: PlantSchema },
    { name: Item.name, schema: ItemSchema },
  ])],
  controllers: [PlantController],
  providers: [PlantService],
  exports: [PlantService],
})
export class PlantModule {}
