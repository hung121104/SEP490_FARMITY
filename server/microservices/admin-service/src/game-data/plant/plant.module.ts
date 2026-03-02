import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Plant, PlantSchema } from './plant.schema';
import { PlantService } from './plant.service';
import { PlantController } from './plant.controller';

@Module({
  imports: [MongooseModule.forFeature([{ name: Plant.name, schema: PlantSchema }])],
  controllers: [PlantController],
  providers: [PlantService],
  exports: [PlantService],
})
export class PlantModule {}
