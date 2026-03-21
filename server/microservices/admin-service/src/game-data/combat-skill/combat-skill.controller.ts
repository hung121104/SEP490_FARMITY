import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CombatSkillService } from './combat-skill.service';
import { CreateCombatSkillDto } from './dto/create-combat-skill.dto';
import { UpdateCombatSkillDto } from './dto/update-combat-skill.dto';

@Controller()
export class CombatSkillController {
  constructor(private readonly combatSkillService: CombatSkillService) {}

  @MessagePattern('create-combat-skill')
  async create(@Payload() dto: CreateCombatSkillDto) {
    return this.combatSkillService.create(dto);
  }

  @MessagePattern('get-combat-skill-catalog')
  async getCatalog() {
    return this.combatSkillService.getCatalog();
  }

  @MessagePattern('get-all-combat-skills')
  async findAll() {
    return this.combatSkillService.findAll();
  }

  @MessagePattern('get-combat-skill-by-id')
  async findById(@Payload() payload: { skillId: string }) {
    return this.combatSkillService.findBySkillId(payload.skillId);
  }

  @MessagePattern('update-combat-skill')
  async update(@Payload() payload: { skillId: string } & UpdateCombatSkillDto) {
    const { skillId, ...dto } = payload;
    return this.combatSkillService.update(skillId, dto);
  }

  @MessagePattern('delete-combat-skill')
  async remove(@Payload() payload: { skillId: string }) {
    return this.combatSkillService.delete(payload.skillId);
  }
}
