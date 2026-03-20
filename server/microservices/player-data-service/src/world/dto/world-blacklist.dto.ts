export class GetWorldBlacklistDto {
  worldId: string;
  requesterId: string;
  requesterIsAdmin?: boolean;
}

export class AddWorldBlacklistDto {
  worldId: string;
  requesterId: string;
  requesterIsAdmin?: boolean;
  playerId: string;
}

export class RemoveWorldBlacklistDto {
  worldId: string;
  requesterId: string;
  requesterIsAdmin?: boolean;
  playerId: string;
}
