import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { CharacterService } from './character.service';
import { GetSkillLoadoutDto } from './dto/get-skill-loadout.dto';
import { UpdateSkillLoadoutDto } from './dto/update-skill-loadout.dto';
import { GetCharacterProgressionDto } from './dto/get-character-progression.dto';
import { UpdateCharacterProgressionDto } from './dto/update-character-progression.dto';

@Controller()
export class CharacterController {
  constructor(private readonly characterService: CharacterService) {}

  @MessagePattern('get-character-skill-loadout')
  async getCharacterSkillLoadout(@Body() dto: GetSkillLoadoutDto) {
    return this.characterService.getSkillLoadout(dto.worldId, dto.accountId);
  }

  @MessagePattern('update-character-skill-loadout')
  async updateCharacterSkillLoadout(@Body() dto: UpdateSkillLoadoutDto) {
    return this.characterService.updateSkillLoadout(
      dto.worldId,
      dto.accountId,
      dto.playerSkillSlotIds,
    );
  }

  @MessagePattern('get-character-progression')
  async getCharacterProgression(@Body() dto: GetCharacterProgressionDto) {
    return this.characterService.getCharacterProgression(dto.worldId, dto.accountId);
  }

  @MessagePattern('update-character-progression')
  async updateCharacterProgression(@Body() dto: UpdateCharacterProgressionDto) {
    return this.characterService.updateCharacterProgression(
      dto.worldId,
      dto.accountId,
      dto.level,
      dto.currentExp,
      dto.expToNextLevel,
      dto.baseStrength,
      dto.baseVitality,
    );
  }
}
