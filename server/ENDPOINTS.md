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

## Notes

- **Admin-only endpoints** require authentication via `Authorization: Bearer <token>` header or `access_token` cookie.
- **Public endpoints** do not require authentication.
- **Cloudinary Setup**: Configure `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, and `CLOUDINARY_API_SECRET` in `admin-service/.env` file. See `admin-service/.env.md` for details.
- **Image Upload Flow**: Frontend requests upload signature from backend → Frontend uploads directly to Cloudinary → Frontend sends returned URL to backend for storage.