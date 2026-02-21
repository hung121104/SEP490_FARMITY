import { Controller } from '@nestjs/common';
import { CharacterService } from './character.service';

@Controller()
export class CharacterController {
  constructor(private readonly characterService: CharacterService) {}
}