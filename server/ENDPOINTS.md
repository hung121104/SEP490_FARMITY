# API Endpoints

All requests go through the gateway at `https://0.0.0.0:3000` (HTTPS - accessible from network).

## Authentication

- **POST** `/auth/register`: Register a new user.
  - Body: `{ "username": "string", "password": "string", "email": "string", "gameSettings": { "audio": boolean, "keyBinds": object } }`
- **POST** `/auth/login-ingame`: Login user for in-game authentication (Photon PUN 2 compatible).
  - Body: `{ "username": "string", "password": "string" }`
  - Response: `{ "ResultCode": 1, "Message": "Login successful", "UserId": "string", "Nickname": "string", "Token": "string", "Data": {} }`
  - Note: ResultCode 1 indicates success. Token is a JWT for authentication.

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

## Player Data

- **POST** `/player-data/save-position`: Save player position.
  - Body: `{ "worldId": "string", "accountId": "string", "positionX": number, "positionY": number, "chunkIndex": number }`
- **GET** `/player-data/position?worldId=string&accountId=string`: Get player position.

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
