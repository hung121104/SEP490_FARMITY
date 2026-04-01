import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Material, MaterialSchema } from './material.schema';
import { MaterialService } from './material.service';
import { MaterialController } from './material.controller';
import { Item, ItemSchema } from '../item/item.schema';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: Material.name, schema: MaterialSchema },
      { name: Item.name, schema: ItemSchema },
    ]),
  ],
  controllers: [MaterialController],
  providers: [MaterialService],
  exports: [MaterialService],
})
export class MaterialModule {}
