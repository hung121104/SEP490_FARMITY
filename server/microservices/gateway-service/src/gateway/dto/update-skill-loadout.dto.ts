import { ArrayMaxSize, IsArray, IsOptional, IsString } from 'class-validator';

export class UpdateSkillLoadoutDto {
  @IsString()
  worldId: string;

  @IsArray()
  @ArrayMaxSize(4)
  @IsOptional()
  @IsString({ each: true })
  playerSkillSlotIds: string[];
}
