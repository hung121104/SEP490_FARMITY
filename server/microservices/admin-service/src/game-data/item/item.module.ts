import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { Item, ItemSchema } from './item.schema';
import { ItemService } from './item.service';
import { ItemController } from './item.controller';
import { CombatSkill, CombatSkillSchema } from '../combat-skill/combat-skill.schema';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: Item.name, schema: ItemSchema },
      { name: CombatSkill.name, schema: CombatSkillSchema },
    ]),
  ],
  controllers: [ItemController],
  providers: [ItemService],
  exports: [ItemService],
})
export class ItemModule {}
