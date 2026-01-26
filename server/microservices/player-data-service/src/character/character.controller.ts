import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { CharacterService } from './character.service';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';

@Controller()
export class CharacterController {
  constructor(private readonly characterService: CharacterService) {}

  @MessagePattern('save-position')
  async savePosition(@Body() savePositionDto: SavePositionDto) {
    return this.characterService.savePosition(savePositionDto);
  }

  @MessagePattern('get-position')
  async getPosition(@Body() getPositionDto: GetPositionDto) {
    return this.characterService.getPosition(getPositionDto);
  }
}