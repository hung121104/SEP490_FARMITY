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

## Admin Authentication

- **POST** `/auth/register-admin`: Register a new admin account.
  - Body: `{ "username": "string", "password": "string", "email": "string", "adminSecret": "string" }`
  - Note: Requires `adminSecret` matching `ADMIN_CREATION_SECRET` environment variable.
- **POST** `/auth/login-admin`: Login as admin for web management.
  - Body: `{ "username": "string", "password": "string" }`
  - Response: `{ "userId": "string", "username": "string", "access_token": "string" }`
  - Note: Sets HTTP-only cookie `access_token` with 60-minute inactivity timeout.
- **GET** `/auth/admin-check`: Check admin session validity (passive verification).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Response: `{ "username": "string", "sub": "string", "isAdmin": true }`
- **POST** `/auth/logout`: Logout admin and revoke session.
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Response: `{ "ok": true }`

## Admin Password Reset

- **POST** `/auth/admin-reset/request`: Request OTP for password reset.
  - Body: `{ "email": "string" }`
  - Response: `{ "ok": true }`
  - Note: Sends 6-digit OTP to admin email, valid for 2 minutes.
- **POST** `/auth/admin-reset/confirm`: Confirm OTP and set new password.
  - Body: `{ "email": "string", "otp": "string", "newPassword": "string" }`
  - Response: `{ "ok": true }`

## Blog (Development Diary)

- **POST** `/blog/create`: Create a new blog post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string" }`
- **GET** `/blog/all`: Get all blog posts (public).
  - Response: Array of blogs sorted by publish date descending.
- **GET** `/blog/:id`: Get a single blog post by ID (public).
  - Response: Blog object or null.
- **POST** `/blog/update/:id`: Update a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string" }`
  - Note: All fields are optional.
- **DELETE** `/blog/delete/:id`: Delete a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`

## News & Announcements

### Upload Flow (with Cloudinary)

**Step 1: Get Upload Signature (admin only)**
- **POST** `/news/upload-signature`
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "folder": "news" }`
  - Response: `{ "cloudName": "string", "apiKey": "string", "timestamp": number, "signature": "string", "folder": "string" }`
  - Note: Configure your Cloudinary credentials in `admin-service/.env` file.

**Step 2: Upload Image to Cloudinary (For upload test)**
- **POST** `https://api.cloudinary.com/v1_1/{cloudName}/image/upload`
  - Body (form-data): 
    ```
    file: (your image file)
    api_key: (from step 1 response)
    timestamp: (from step 1 response)
    signature: (from step 1 response)
    folder: (from step 1 response)
    ```
  - Response: `{ "secure_url": "https://res.cloudinary.com/.../news/image.jpg", ... }`
  - Note: This endpoint is provided by Cloudinary. Frontend uploads directly to Cloudinary, not to backend. For testing, use Postman with form-data body.

### CRUD Operations

- **POST** `/news/create`: Create a news post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string", "thumbnailUrl": "string" }`
  - Note: `thumbnailUrl` is optional (Cloudinary image URL from step 2).
- **GET** `/news/all`: Get all news posts (public).
  - Response: Array of news sorted by publish date descending.
- **GET** `/news/:id`: Get a single news post by ID (public).
  - Response: News object or null.
- **POST** `/news/update/:id`: Update a news post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string", "thumbnailUrl": "string" }`
  - Note: All fields are optional.
- **DELETE** `/news/delete/:id`: Delete a news post (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`

## Media Gallery

### Upload Flow (with Cloudinary)

**Step 1: Get Upload Signature (admin only)**
- **POST** `/media/upload-signature`
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "folder": "media" }`
  - Response: `{ "cloudName": "string", "apiKey": "string", "timestamp": number, "signature": "string", "folder": "string" }`
  - Note: Configure your Cloudinary credentials in `admin-service/.env` file.

**Step 2: Upload Image to Cloudinary (For upload test)**
- **POST** `https://api.cloudinary.com/v1_1/{cloudName}/image/upload`
  - Body (form-data): 
    ```
    file: (your image file)
    api_key: (from step 1 response)
    timestamp: (from step 1 response)
    signature: (from step 1 response)
    folder: (from step 1 response)
    ```
  - Response: `{ "secure_url": "https://res.cloudinary.com/.../media/image.jpg", ... }`
  - Note: This endpoint is provided by Cloudinary. Frontend uploads directly to Cloudinary, not to your backend. For testing, use Postman with form-data body.

### CRUD Operations

- **POST** `/media/create`: Upload media to gallery (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "file_url": "string", "description": "string" }`
  - Note: `file_url` is required (Cloudinary image URL from step 2). `description` is optional.
- **GET** `/media/all`: Get all media (public).
  - Response: Array of media sorted by upload date descending.
- **GET** `/media/:id`: Get a single media by ID (public).
  - Response: Media object or null.
- **POST** `/media/update/:id`: Update media (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`
  - Body: `{ "file_url": "string", "description": "string" }`
  - Note: All fields are optional.
- **DELETE** `/media/delete/:id`: Delete media (admin only).
  - Headers: `Authorization: Bearer <token>` or Cookie: `access_token`

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

- **PUT** `/player-data/world`: Update world fields and/or upsert up to 4 player characters.
  - Headers: `Authorization: Bearer <token>` (gateway middleware verifies JWT; `ownerId` is injected from token)
  - Body:
    ```json
    {
      "worldId": "string",
      "day": 5,
      "month": 3,
      "year": 1,
      "hour": 12,
      "minute": 30,
      "gold": 500,
      "characters": [
        { "accountId": "string", "positionX": 10, "positionY": 20 },
        { "accountId": "string", "positionX": 5,  "positionY": 15, "sectionIndex": 2 }
      ]
    }
    ```
  - All fields except `worldId` are optional.
  - `characters` is optional and capped at **4** entries. Each character is matched by `(worldId, accountId)` and created if it does not exist or updated if it does.
  - Response: updated world document with a `characters` array of upserted character documents.
  - Notes: Only the owner of the world (verified via JWT) may call this endpoint.

- **GET** `/player-data/worlds/:worldId/characters/:accountId/position`: Get or create a character for a player in a world.
  - Headers: `Authorization: Bearer <token>` (world owner only)
  - Path parameters:
    - `worldId`: string - The ID of the world
    - `accountId`: string - The ID of the player's account
  - Response: Character document `{ "worldId": "string", "accountId": "string", "positionX": number, "positionY": number, "sectionIndex": number }`
  - Notes: 
    - Only the owner of the world can access this endpoint.
    - If the player already has a character in the world, it returns the existing character.
    - If the player doesn't have a character, it creates a new one with default position (0, 0, 0) and returns it.
    - This endpoint is typically used when a player joins a world owned by another player.

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

- **Message** `update-world`: Update world fields and/or upsert up to 4 player characters.
  - Body:
    ```json
    {
      "worldId": "string",
      "ownerId": "string",
      "day"?: number,
      "month"?: number,
      "year"?: number,
      "hour"?: number,
      "minute"?: number,
      "gold"?: number,
      "characters"?: [
        { "accountId": "string", "positionX": number, "positionY": number, "sectionIndex"?: number }
      ]
    }
    ```
  - Notes: `ownerId` is injected by the gateway from the verified JWT. World fields are only updated when present. `characters` is capped at 4; each entry is upserted by `(worldId, accountId)` — insert on first appearance, update on subsequent calls.
  - Response: updated world document with a `characters` array of upserted character documents.

- **Message** `delete-world`: Delete a world by id.
  - Body: `{ "_id": "string", "ownerId": "string" }`
  - Notes: The gateway provides a verified `ownerId` (extracted from the JWT) and the microservice enforces that the sender's `ownerId` matches the world's `ownerId` before deleting.
  - Response: deleted world document `{ "_id": "string", "worldName": "string", "ownerId": "string" }` or `null`.
    - Notes: The gateway provides a verified `ownerId` (extracted from the JWT) and the microservice enforces that the sender's `ownerId` matches the world's `ownerId` before deleting.
    - Response: deleted world document `{ "_id": "string", "worldName": "string", "ownerId": "string", "day": number, "month": number, "year": number, "hour": number, "minute": number, "gold": number }` or `null`.
