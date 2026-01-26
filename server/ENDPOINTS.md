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