import { IsString } from 'class-validator';

export class DeleteDroppedItemDto {
  @IsString()
  dropId: string;
}
