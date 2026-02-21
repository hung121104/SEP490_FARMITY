export class UpsertCharacterInWorldDto {
  _id?: string;
  accountId: string;
  positionX: number;
  positionY: number;
  sectionIndex?: number;
}

export class UpdateWorldDto {
  worldId: string;

  // Optional world fields to update
  day?: number;
  month?: number;
  year?: number;
  hour?: number;
  minute?: number;
  gold?: number;

  // Up to 4 characters
  characters?: UpsertCharacterInWorldDto[];
}
