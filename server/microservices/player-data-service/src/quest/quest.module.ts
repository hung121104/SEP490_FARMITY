import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Quest, QuestSchema } from './quest.schema';
import { QuestService } from './quest.service';
import { QuestController } from './quest.controller';

@Module({
  imports: [MongooseModule.forFeature([{ name: Quest.name, schema: QuestSchema }])],
  controllers: [QuestController],
  providers: [QuestService],
  exports: [QuestService],
})
export class QuestModule {}
