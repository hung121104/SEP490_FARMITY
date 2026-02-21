export class UpsertCharacterDto {
  _id?: string;          // optional: existing character document _id
  accountId: string;     // identifies the character within the world
  positionX: number;
  positionY: number;
  sectionIndex?: number;
}
