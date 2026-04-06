export class GetCharacterInWorldDto {
  worldId: string;
  accountId: string;
  ownerId?: string; // For authorization - should be the world owner
}
