# API Endpoints

All requests go through the gateway at `https://0.0.0.0:3000` (HTTPS - accessible from network).

## Authentication

- **POST** `/auth/register`: Register a new user.
  - Body: `{ "username": "string", "password": "string", "email": "string", "gameSettings": { "audio": boolean, "keyBinds": object } }`
- **POST** `/auth/login-ingame`: Login user for in-game authentication (Photon PUN 2 compatible).
  - Body: `{ "username": "string", "password": "string" }`
  - Response: `{ "ResultCode": 1, "Message": "Login successful", "UserId": "string", "Nickname": "string", "Token": "string", "Data": {} }`
  - Note: ResultCode 1 indicates success. Token is a JWT for authentication.
 - **POST** `/auth/register-admin`: Register an admin account.
   - Body: `{ "username": "string", "password": "string", "email": "string" }
 - **POST** `/auth/login-admin`: Login for admin UI.
   - Body: `{ "username": "string", "password": "string" }`
   - Response: `{ "userId": "string", "username": "string", "access_token": "string" }` and sets `access_token` cookie.
 - **POST** `/auth/logout`: Logout (clears admin cookie)
   - Headers: `Authorization: Bearer <token>` or cookie `access_token`.
   - Response: `{ "ok": true }`
 - **GET** `/auth/admin-check`: Verify admin token (returns token payload if valid).
   - Headers: `Authorization: Bearer <token>` or cookie `access_token`.
 - **POST** `/auth/admin-reset/request`: Request an admin password reset.
   - Body: `RequestAdminResetDto`.
 - **POST** `/auth/admin-reset/confirm`: Confirm admin password reset.
   - Body: `ConfirmAdminResetDto`.

## Player Data

- **POST** `/player-data/save-position`: Save player position.
  - Body: `{ "worldId": "string", "accountId": "string", "positionX": number, "positionY": number, "chunkIndex": number }`
- **GET** `/player-data/position?worldId=string&accountId=string`: Get player position.

## World (gateway HTTP)

- **POST** `/player-data/world`: Create or update a world.
  - Headers: `Authorization: Bearer <token>` (gateway middleware verifies JWT)
    - Body (create): `{ "worldName": "string", "day"?: number, "month"?: number, "year"?: number, "hour"?: number, "minute"?: number, "gold"?: number }`.
      - All numeric time fields and `gold` are optional; they default to `0` when omitted.
    - Response: created/updated world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }`.
    - Notes: `ownerId` is taken from the verified token (`sub`) by the gateway and forwarded to the microservice.

- **GET** `/player-data/world?_id=string`: Get a world by id.
  - Headers: `Authorization: Bearer <token>`
    - Response: world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }` or `null`.

- **DELETE** `/player-data/world?_id=string`: Delete a world by id.
  - Headers: `Authorization: Bearer <token>` (gateway middleware verifies JWT)
  - Query: `_id=string`
    - Response: deleted world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }` or `null`.

- **GET** `/player-data/worlds`: Get all worlds owned by the authenticated account.
  - Headers: `Authorization: Bearer <token>`
  - Optional query: `ownerId=string` (only allowed for admin accounts)
    - Response: array of world documents (each includes `day`, `month`, `year`, `hour`, `minute`, `gold`, all numeric, default `0`).

## Blog

- **POST** `/blog/create`: Create a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `CreateBlogDto`
  - Forwards `create-blog` message to blog service.

- **GET** `/blog/all`: Get all blog posts.
  - No auth required.
  - Forwards `get-all-blogs` message.

- **GET** `/blog/:id`: Get blog by id.
  - Param: `id`
  - Forwards `get-blog-by-id` message.

- **POST** `/blog/update/:id`: Update a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `UpdateBlogDto`
  - Forwards `update-blog` message with `{ id, updateBlogDto }`.

- **DELETE** `/blog/delete/:id`: Delete blog post (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Forwards `delete-blog` message with `id`.

## News

- **POST** `/news/upload-signature`: Get upload signature for news media (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `UploadSignatureDto`
  - Forwards `news-upload-signature` message.

- **POST** `/news/create`: Create news (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `CreateNewsDto`
  - Forwards `create-news` message.

- **GET** `/news/all`: Get all news.
  - Forwards `get-all-news` message.

- **GET** `/news/:id`: Get news by id.
  - Param: `id`
  - Forwards `get-news-by-id` message.

- **POST** `/news/update/:id`: Update news (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `UpdateNewsDto`
  - Forwards `update-news` message with `{ id, updateNewsDto }`.

- **DELETE** `/news/delete/:id`: Delete news (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Forwards `delete-news` message with `id`.

## Media

- **POST** `/media/upload-signature`: Get upload signature for media (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `UploadSignatureDto`
  - Forwards `media-upload-signature` message.

- **POST** `/media/create`: Create media (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `CreateMediaDto`
  - Forwards `create-media` message.

- **GET** `/media/all`: Get all media.
  - Forwards `get-all-media` message.

- **GET** `/media/:id`: Get media by id.
  - Param: `id`
  - Forwards `get-media-by-id` message.

- **POST** `/media/update/:id`: Update media (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Body: `UpdateMediaDto`
  - Forwards `update-media` message with `{ id, updateMediaDto }`.

- **DELETE** `/media/delete/:id`: Delete media (admin only).
  - Headers: `Authorization: Bearer <token>` (admin)
  - Forwards `delete-media` message with `id`.

## World (player-data-service messages)

- **Message** `create-world`: Create or update a world.
  - Body: `{ "_id"?: "string", "worldName": "string", "ownerId": "string" }`
  - Notes: `ownerId` is an `accountId` (references `Account`). The gateway provides a verified `ownerId` (extracted from the JWT) when forwarding this message.
    - Body: `{ "_id"?: "string", "worldName": "string", "ownerId": "string", "day"?: number, "month"?: number, "year"?: number, "hour"?: number, "minute"?: number, "gold"?: number }`
    - Notes: `ownerId` is an `accountId` (references `Account`). The gateway provides a verified `ownerId` (extracted from the JWT) when forwarding this message. Numeric time fields and `gold` default to `0` if omitted.

- **Message** `get-world`: Get a world by id.
  - Body: `{ "_id": "string" }`
  - Response: world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }` or `null`.
    - Response: world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }` or `null`.

- **Message** `get-worlds-by-owner`: Get all worlds owned by an account.
  - Body: `{ "ownerId": "string" }`
  - Notes: `ownerId` should be provided by the gateway after verifying the caller's JWT; the microservice returns worlds for the supplied `ownerId`.

- **Message** `delete-world`: Delete a world by id.
  - Body: `{ "_id": "string", "ownerId": "string" }`
  - Notes: The gateway provides a verified `ownerId` (extracted from the JWT) and the microservice enforces that the sender's `ownerId` matches the world's `ownerId` before deleting.
  - Response: deleted world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }` or `null`.
    - Notes: The gateway provides a verified `ownerId` (extracted from the JWT) and the microservice enforces that the sender's `ownerId` matches the world's `ownerId` before deleting.
    - Response: deleted world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }` or `null`.