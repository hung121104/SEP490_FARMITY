import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { DroppedItem, DroppedItemSchema } from './dropped-item.schema';
import { DroppedItemService } from './dropped-item.service';
import { DroppedItemController } from './dropped-item.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: DroppedItem.name, schema: DroppedItemSchema },
    ]),
  ],
  controllers: [DroppedItemController],
  providers: [DroppedItemService],
})
export class DroppedItemModule {}
