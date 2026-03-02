import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Item, ItemSchema } from './item.schema';
import { ItemService } from './item.service';
import { ItemController } from './item.controller';

@Module({
  imports: [MongooseModule.forFeature([{ name: Item.name, schema: ItemSchema }])],
  controllers: [ItemController],
  providers: [ItemService],
  exports: [ItemService],
})
export class ItemModule {}
