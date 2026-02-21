import { UpsertCharacterDto } from '../../character/dto/upsert-character.dto';

export class UpdateWorldDto {
  worldId: string;
  ownerId: string;

  // Optional world time/economy fields
  day?: number;
  month?: number;
  year?: number;
  hour?: number;
  minute?: number;
  gold?: number;

  // Up to 4 characters to upsert
  characters?: UpsertCharacterDto[];
}
