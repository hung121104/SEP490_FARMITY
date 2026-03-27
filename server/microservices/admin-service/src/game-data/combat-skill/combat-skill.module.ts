import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { CombatSkill, CombatSkillSchema } from './combat-skill.schema';
import { CombatSkillController } from './combat-skill.controller';
import { CombatSkillService } from './combat-skill.service';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: CombatSkill.name, schema: CombatSkillSchema },
    ]),
  ],
  controllers: [CombatSkillController],
  providers: [CombatSkillService],
  exports: [CombatSkillService],
})
export class CombatSkillModule {}
