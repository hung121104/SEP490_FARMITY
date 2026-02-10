# API Endpoints

All requests go through the gateway at `https://0.0.0.0:3000` (HTTPS - accessible from network).

## Authentication

- **POST** `/auth/register`: Register a new user.
  - Body: `{ "username": "string", "password": "string", "email": "string", "gameSettings": { "audio": boolean, "keyBinds": object } }`
- **POST** `/auth/login-ingame`: Login user for in-game authentication (Photon PUN 2 compatible).
  - Body: `{ "username": "string", "password": "string" }`
  - Response: `{ "ResultCode": 1, "Message": "Login successful", "UserId": "string", "Nickname": "string", "Token": "string", "Data": {} }`
  - Note: ResultCode 1 indicates success. Token is a JWT for authentication.

## Player Data

- **POST** `/player-data/save-position`: Save player position.
  - Body: `{ "worldId": "string", "accountId": "string", "positionX": number, "positionY": number, "chunkIndex": number }`
- **GET** `/player-data/position?worldId=string&accountId=string`: Get player position.

## World (gateway HTTP)

- **POST** `/player-data/world`: Create or update a world.
  - Headers: `Authorization: Bearer <token>` (gateway middleware verifies JWT)
  - Body (create): `{ "worldName": "string" }`
  - Body (update): `{ "_id": "string", "worldName": "string" }`
  - Response: created/updated world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }`.
  - Notes: `ownerId` is taken from the verified token (`sub`) by the gateway and forwarded to the microservice.

- **GET** `/player-data/world?_id=string`: Get a world by id.
  - Headers: `Authorization: Bearer <token>`
  - Response: world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }` or `null`.

- **GET** `/player-data/worlds`: Get all worlds owned by the authenticated account.
  - Headers: `Authorization: Bearer <token>`
  - Optional query: `ownerId=string` (only allowed for admin accounts)
  - Response: array of world documents.

## World (player-data-service messages)

- **Message** `create-world`: Create or update a world.
  - Body: `{ "_id"?: "string", "worldName": "string", "ownerId": "string" }`
  - Notes: `ownerId` is an `accountId` (references `Account`). The gateway provides a verified `ownerId` (extracted from the JWT) when forwarding this message.

- **Message** `get-world`: Get a world by id.
  - Body: `{ "_id": "string" }`
  - Response: world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }` or `null`.

- **Message** `get-worlds-by-owner`: Get all worlds owned by an account.
  - Body: `{ "ownerId": "string" }`
  - Notes: `ownerId` should be provided by the gateway after verifying the caller's JWT; the microservice returns worlds for the supplied `ownerId`.