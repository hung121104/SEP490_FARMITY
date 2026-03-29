import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { CombatSkillService } from './combat-skill.service';
import { CreateCombatSkillDto } from './dto/create-combat-skill.dto';
import { UpdateCombatSkillDto } from './dto/update-combat-skill.dto';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class CombatSkillController {
  constructor(private readonly combatSkillService: CombatSkillService) {}

  @MessagePattern('create-combat-skill')
  async create(@Payload() dto: CreateCombatSkillDto): Promise<CatalogChange> {
    const data = await this.combatSkillService.create(dto);
    return {
      data,
      changeType: 'create',
      entityType: 'combat-skill',
    };
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
  async update(
    @Payload() payload: { skillId: string } & UpdateCombatSkillDto,
  ): Promise<CatalogChange> {
    const { skillId, ...dto } = payload;
    const data = await this.combatSkillService.update(skillId, dto);
    return {
      data,
      changeType: 'update',
      entityType: 'combat-skill',
    };
  }

  @MessagePattern('delete-combat-skill')
  async remove(
    @Payload() payload: { skillId: string },
  ): Promise<CatalogChange> {
    const data = await this.combatSkillService.delete(payload.skillId);
    return {
      data,
      changeType: 'delete',
      entityType: 'combat-skill',
    };
  }
}
