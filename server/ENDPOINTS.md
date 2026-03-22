# API Endpoints

All requests go through the gateway at `https://0.0.0.0:3000` (HTTPS - accessible from network).

---

## Table of Contents

1. [Authentication & Authorization](#authentication--authorization)
   - [User Authentication](#user-authentication)
   - [Admin Authentication](#admin-authentication)
   - [Admin Password Reset](#admin-password-reset)
2. [Game Config](#game-config)
   - [Main Menu](#main-menu)
3. [Content Management](#content-management)
   - [Blog (Development Diary)](#blog-development-diary)
   - [News & Announcements](#news--announcements)
   - [Media Gallery](#media-gallery)
4. [Game Data Management](#game-data-management)
   - [Items Catalog](#items-catalog)

- [Fertilizer Catalog](#fertilizer-catalog)
- [Plants Catalog](#plants-catalog)
- [Crafting Recipes](#crafting-recipes)
- [Skin Configs (Paper Doll)](#skin-configs-paper-doll)

- [Resource Config Catalog](#resource-config-catalog)
- [Material Catalog](#material-catalog)
- [Quest Catalog](#quest-catalog)

5. [Player Data](#player-data)
   - [World Management](#world-management)
  - [World Blacklist](#world-blacklist)
  - [Player Achievements](#player-achievements)
   - [Character Management](#character-management)

6. [Achievement Definitions (Admin)](#achievement-definitions-admin)

---

## Authentication & Authorization

### User Authentication

- **POST** `/auth/register`: Register a new user account.
  - Body:
    ```json
    {
      "username": "string",
      "password": "string",
      "email": "string",
      "gameSettings": {
        "audio": "boolean",
        "keyBinds": "object"
      }
    }
    ```
  - Response: User account details
  - Note: Used for creating player accounts for the game.

- **POST** `/auth/login-ingame`: Login for in-game authentication (Photon PUN 2 compatible).
  - Body: `{ "username": "string", "password": "string" }`
  - Response:
    ```json
    {
      "ResultCode": 1,
      "Message": "Login successful",
      "UserId": "string",
      "Nickname": "string",
      "Token": "string",
      "Data": {}
    }
    ```
  - Note: `ResultCode` 1 indicates success. Token is a JWT for authentication.

### Admin Authentication

- **POST** `/auth/register-admin`: Register a new admin account.
  - Body:
    ```json
    {
      "username": "string",
      "password": "string",
      "email": "string",
      "adminSecret": "string"
    }
    ```
  - Note: Requires `adminSecret` matching `ADMIN_CREATION_SECRET` environment variable.

- **POST** `/auth/login-admin`: Login as admin for web management.
  - Body: `{ "username": "string", "password": "string" }`
  - Response:
    ```json
    {
      "userId": "string",
      "username": "string",
      "access_token": "string"
    }
    ```
  - Note: Sets HTTP-only cookie `access_token` with 60-minute inactivity timeout.

- **GET** `/auth/admin-check`: Check admin session validity (passive verification).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Response: `{ "username": "string", "sub": "string", "isAdmin": true }`

- **POST** `/auth/logout`: Logout admin and revoke session.
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Response: `{ "ok": true }`

### Password Reset

- **POST** `/auth/reset/request`: Request OTP for password reset.
  - Body: `{ "email": "string" }`
  - Response: `{ "ok": true }`
  - Note: Sends 6-digit OTP to the account email, valid for 2 minutes.

- **POST** `/auth/reset/confirm`: Confirm OTP and set new password.
  - Body: `{ "email": "string", "otp": "string", "newPassword": "string" }`
  - Response: `{ "ok": true }`

## Game Config

### Main Menu

- **GET** `/game-config/main-menu`: Get the current main-menu background config (public).
  - Response:
    ```json
    {
      "currentBackgroundUrl": "string",
      "version": "number"
    }
    ```
  - Note: Returns `null` if no background has been set yet.

- **PUT** `/game-config/main-menu`: Update the main-menu background image (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:
    - `background` _(file, required)_ — Background image (max 10 MB). Uploaded to Cloudinary folder `game-config` automatically; `currentBackgroundUrl` set from the resulting `secure_url`.
  - Response:
    ```json
    {
      "currentBackgroundUrl": "string",
      "version": "number"
    }
    ```

---

## Content Management

### Blog (Development Diary)

#### Endpoints

- **POST** `/blog/create`: Create a new blog post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string" }`
  - Response: Created blog document

- **GET** `/blog/all`: Get all blog posts (public).
  - Response: Array of blogs sorted by publish date descending.

- **GET** `/blog/:id`: Get a single blog post by ID (public).
  - Path param: `id` - MongoDB ObjectId string
  - Response: Blog object or `null`

- **POST** `/blog/update/:id`: Update a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Body: `{ "title": "string", "content": "string" }`
  - Note: All fields are optional.

- **DELETE** `/blog/delete/:id`: Delete a blog post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Response: Deleted blog document

---

### News & Announcements

#### Upload Flow (with Cloudinary)

**Step 1: Get Upload Signature (admin only)**

- **POST** `/news/upload-signature`
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: `{ "folder": "news" }`
  - Response:
    ```json
    {
      "cloudName": "string",
      "apiKey": "string",
      "timestamp": "number",
      "signature": "string",
      "folder": "string"
    }
    ```
  - Note: Configure Cloudinary credentials in `admin-service/.env` file.

**Step 2: Upload Image to Cloudinary**

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
  - Note: This is a Cloudinary endpoint. Frontend uploads directly to Cloudinary. For testing, use Postman with form-data body.

#### CRUD Operations

- **POST** `/news/create`: Create a news post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: `{ "title": "string", "content": "string", "thumbnailUrl": "string" }`
  - Note: `thumbnailUrl` is optional (Cloudinary image URL from step 2).

- **GET** `/news/all`: Get all news posts (public).
  - Response: Array of news sorted by publish date descending.

- **GET** `/news/:id`: Get a single news post by ID (public).
  - Path param: `id` - MongoDB ObjectId string
  - Response: News object or `null`

- **POST** `/news/update/:id`: Update a news post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Body: `{ "title": "string", "content": "string", "thumbnailUrl": "string" }`
  - Note: All fields are optional.

- **DELETE** `/news/delete/:id`: Delete a news post (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string

---

### Media Gallery

#### Upload Flow (with Cloudinary)

**Step 1: Get Upload Signature (admin only)**

- **POST** `/media/upload-signature`
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: `{ "folder": "media" }`
  - Response:
    ```json
    {
      "cloudName": "string",
      "apiKey": "string",
      "timestamp": "number",
      "signature": "string",
      "folder": "string"
    }
    ```
  - Note: Configure Cloudinary credentials in `admin-service/.env` file.

**Step 2: Upload Image to Cloudinary**

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
  - Note: This is a Cloudinary endpoint. Frontend uploads directly to Cloudinary. For testing, use Postman with form-data body.

#### CRUD Operations

- **POST** `/media/create`: Upload media to gallery (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: `{ "file_url": "string", "description": "string" }`
  - Note: `file_url` is required (Cloudinary image URL from step 2). `description` is optional.

- **GET** `/media/all`: Get all media (public).
  - Response: Array of media sorted by upload date descending.

- **GET** `/media/:id`: Get a single media by ID (public).
  - Path param: `id` - MongoDB ObjectId string
  - Response: Media object or `null`

- **POST** `/media/update/:id`: Update media (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Body: `{ "file_url": "string", "description": "string" }`
  - Note: All fields are optional.

- **DELETE** `/media/delete/:id`: Delete media (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string

---

## Player Data

### World Management

#### HTTP Endpoints

- **POST** `/player-data/world`: Create or update a world.
  - Headers: `Authorization: Bearer <token>` (gateway verifies JWT)
  - Body (create):
    ```json
    {
      "worldName": "string",
      "day": "number (optional, default: 0)",
      "month": "number (optional, default: 0)",
      "year": "number (optional, default: 0)",
      "hour": "number (optional, default: 0)",
      "minute": "number (optional, default: 0)",
      "gold": "number (optional, default: 0)"
    }
    ```
  - Response:
    ```json
    {
      "_id": "string",
      "worldName": "string",
      "ownerId": "string",
      "day": "number",
      "month": "number",
      "year": "number",
      "hour": "number",
      "minute": "number",
      "gold": "number"
    }
    ```
  - Note: `ownerId` is extracted from JWT token by gateway and forwarded to the microservice.

- **GET** `/player-data/world?_id=string`: Get a world by ID.
  - Headers: `Authorization: Bearer <token>`
  - Query params: `_id` - MongoDB ObjectId string
  - Response: World document or `null`

- **GET** `/player-data/worlds`: Get all worlds owned by authenticated account.
  - Headers: `Authorization: Bearer <token>`
  - Optional query: `ownerId=string` (only allowed for admin accounts)
  - Response: Array of world documents

- **PUT** `/player-data/world`: Update world fields and/or upsert up to 4 player characters.
  - Headers: `Authorization: Bearer <token>` (gateway verifies JWT; `ownerId` injected from token)
  - Body:
    ```json
    {
      "worldId": "string",
      "day": "number (optional)",
      "month": "number (optional)",
      "year": "number (optional)",
      "hour": "number (optional)",
      "minute": "number (optional)",
      "gold": "number (optional)",
      "characters": [
        {
          "accountId": "string",
          "positionX": "number",
          "positionY": "number",
          "sectionIndex": "number (optional)"
        }
      ]
    }
    ```
  - Response: Updated world document with `characters` array
  - Note:
    - All fields except `worldId` are optional
    - `characters` array is optional and capped at 4 entries
    - Each character is matched by `(worldId, accountId)` and created or updated
    - Only world owner can call this endpoint

- **DELETE** `/player-data/world?_id=string`: Delete a world by ID.
  - Headers: `Authorization: Bearer <token>` (gateway verifies JWT)
  - Query params: `_id` - MongoDB ObjectId string
  - Response: Deleted world document or `null`
  - Note: Only world owner can delete

### World Blacklist

- **GET** `/player-data/world/blacklist?_id=string`: Get blacklist for a world.
  - Headers: `Authorization: Bearer <token>`
  - Query params: `_id` - MongoDB ObjectId string (world ID)
  - Access: Any authenticated user
  - Response:
    ```json
    {
      "worldId": "string",
      "blacklistedPlayerIds": ["string"],
      "blacklistedPlayers": [
        {
          "accountId": "string",
          "username": "string | null"
        }
      ]
    }
    ```
  - Note: `blacklistedPlayerIds` is source-of-truth storage. `blacklistedPlayers` is runtime-enriched for UI display and not persisted.

- **POST** `/player-data/world/blacklist`: Add one player account to a world blacklist.
  - Headers: `Authorization: Bearer <token>`
  - Access: World owner or admin
  - Body:
    ```json
    {
      "_id": "string",
      "playerId": "string"
    }
    ```
  - Response:
    ```json
    {
      "worldId": "string",
      "playerId": "string",
      "added": "boolean",
      "blacklistedPlayerIds": ["string"],
      "blacklistedPlayers": [
        {
          "accountId": "string",
          "username": "string | null"
        }
      ]
    }
    ```
  - Notes:
    - Deduplicated: adding an existing ID returns `added: false`
    - Owner cannot blacklist self

- **DELETE** `/player-data/world/blacklist`: Remove one player account from a world blacklist.
  - Headers: `Authorization: Bearer <token>`
  - Access: World owner or admin
  - Body:
    ```json
    {
      "_id": "string",
      "playerId": "string"
    }
    ```
  - Response:
    ```json
    {
      "worldId": "string",
      "playerId": "string",
      "removed": "boolean",
      "blacklistedPlayerIds": ["string"],
      "blacklistedPlayers": [
        {
          "accountId": "string",
          "username": "string | null"
        }
      ]
    }
    ```
  - Note: Idempotent: removing a non-existing ID returns `removed: false`

### Player Achievements

- **GET** `/player-data/achievement`: Get all achievement definitions merged with the authenticated player's progress.
  - Headers: `Authorization: Bearer <token>`
  - Response: Array of achievement objects:
    ```json
    [
      {
        "achievementId": "string",
        "name": "string",
        "description": "string",
        "requirements": [
          {
            "type": "string",
            "target": "number",
            "entityId": "string (optional)",
            "label": "string"
          }
        ],
        "progress": ["number"],
        "achievedAt": "string | null",
        "isAchieved": "boolean"
      }
    ]
    ```

- **PUT** `/player-data/achievement/progress`: Update one requirement progress using absolute value.
  - Headers: `Authorization: Bearer <token>`
  - Body:
    ```json
    {
      "achievementId": "string",
      "requirementIndex": 0,
      "progress": 10
    }
    ```
  - Notes:
    - `requirementIndex` is zero-based
    - Progress is monotonic (stale/lower value is ignored)

- **PUT** `/player-data/achievement/progress/batch`: Update multiple requirement progress values in one call.
  - Headers: `Authorization: Bearer <token>`
  - Body:
    ```json
    [
      {
        "achievementId": "string",
        "requirementIndex": 0,
        "progress": 10
      }
    ]
    ```
  - Response:
    ```json
    {
      "summary": {
        "total": "number",
        "updated": "number",
        "noop": "number",
        "failed": "number"
      },
      "results": [
        {
          "index": "number",
          "achievementId": "string",
          "requirementIndex": "number",
          "submittedProgress": "number",
          "status": "updated | noop | failed",
          "message": "string (optional)",
          "achievement": "object (optional)"
        }
      ],
      "updatedAchievements": ["object"]
    }
    ```
  - Notes:
    - Idempotent/retry-safe
    - Per-item partial failure reporting
    - Stale/lower values return `noop`

---

### Character Management

- **GET** `/player-data/worlds/:worldId/characters/:accountId/position`: Get or create a character for a player in a world.
  - Headers: `Authorization: Bearer <token>` (world owner only)
  - Path params:
    - `worldId` - MongoDB ObjectId string (ID of the world)
    - `accountId` - MongoDB ObjectId string (ID of player's account)
  - Response:
    ```json
    {
      "worldId": "string",
      "accountId": "string",
      "positionX": "number",
      "positionY": "number",
      "sectionIndex": "number"
    }
    ```
  - Note:
    - Only world owner can access this endpoint
    - Returns existing character or creates new one with default position (0, 0, 0)
    - Used when a player joins a world owned by another player

---

## Game Data Management

> Managed by `admin-service`. These endpoints control the game's item, fertilizer, and plant catalogs consumed by Unity clients.

### Items Catalog

#### HTTP Endpoints

- **POST** `/game-data/items/create`: Create a new item definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:
    - `icon` _(file, required)_ — Item icon image (max 5 MB). Uploaded to Cloudinary internally; `iconUrl` set automatically.
    - All other item fields as form-data text fields (see [Base Fields](#base-fields) and [Item Type Fields](#itemtype-discriminator--extra-fields) tables)
  - Response: Saved item document including `_id` and `iconUrl` (Cloudinary `secure_url`)
  - Note: Returns `409 Conflict` if an item with the same `itemID` already exists

- **GET** `/game-data/items/catalog`: Get full item catalog in Unity-client format.
  - Response: `{ "items": [ ...itemObjects ] }`
  - Note: Consumed by `ItemCatalogService.cs` in Unity client

- **GET** `/game-data/items/all`: Get flat array of all item documents.
  - Response: `[ ...itemObjects ]`

- **GET** `/game-data/items/by-item-id/:itemID`: Find item by game-side string ID.
  - Path param: `itemID` - Snake_case string identifier (e.g., `tool_hoe_basic`)
  - Response: Item document or `null`

- **GET** `/game-data/items/:id`: Find item by MongoDB `_id`.
  - Path param: `id` - MongoDB ObjectId string
  - Response: Item document or `null`

- **PUT** `/game-data/items/:itemID`: Update an existing item by game-side `itemID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Path param: `itemID` - game-side string identifier (e.g., `tool_hoe_basic`)
  - Fields: Any subset of item fields as form-data text fields (all optional). Include an `icon` file to replace the icon (max 5 MB, re-uploaded to Cloudinary automatically).
  - Response: Updated item document
  - Note: Returns `404` if item not found

- **DELETE** `/game-data/items/:itemID`: Delete an item by game-side `itemID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `itemID` - game-side string identifier (e.g., `tool_hoe_basic`)
  - Response: Deleted item document
  - Note: Returns `404` if item not found

#### Base Fields

Required for ALL item types:

| Field                    | Type     | Description                                                                        |
| ------------------------ | -------- | ---------------------------------------------------------------------------------- |
| `itemID`                 | string   | Unique game-side identifier (e.g., `"iron_hoe"`)                                   |
| `itemName`               | string   | Display name                                                                       |
| `description`            | string   | Flavour text                                                                       |
| `icon`                   | file     | Item sprite (auto-set on upload)                                                   |
| `itemType`               | int      | Type discriminator (see [Item Type Fields](#itemtype-discriminator--extra-fields)) |
| `itemCategory`           | int      | 0=Farming, 1=Mining, 2=Fishing, 3=Cooking, etc.                                    |
| `maxStack`               | int      | Maximum stack size (set to 1 for tools/weapons)                                    |
| `isStackable`            | bool     | `false` for tools/weapons                                                          |
| `basePrice`              | int      | Base sell value                                                                    |
| `buyPrice`               | int      | Store buy price (`0` = cannot be bought)                                           |
| `canBeSold`              | bool     | Whether item can be sold                                                           |
| `canBeBought`            | bool     | Whether item can be bought                                                         |
| `isQuestItem`            | bool     | Quest-related flag                                                                 |
| `isArtifact`             | bool     | Artifact flag                                                                      |
| `isRareItem`             | bool     | Rarity flag                                                                        |
| `npcPreferenceNames`     | string[] | Optional NPC name list                                                             |
| `npcPreferenceReactions` | int[]    | Optional reaction values (-2 to 2, maps 1-to-1 with names)                         |

#### `itemType` Discriminator & Extra Fields

Depending on `itemType`, specific extra fields must be included:

| `itemType` | Name       | Required Extra Fields                                                              | Type & Notes                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ---------- | ---------- | ---------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `0`        | Tool       | `toolType`<br>`toolLevel`<br>`toolPower`<br>`toolMaterialId`                       | int: 0=Hoe, 1=WateringCan, 2=Pickaxe, 3=Axe, 4=FishingRod<br>int: Tool level (e.g., 1)<br>int: Tool power (e.g., 1)<br>string: `materialId` of a Material document (e.g., `"mat_copper"`). See [Material Catalog](#material-catalog).                                                                                                                                                                                                                                 |
| `1`        | Seed       | `plantId`                                                                          | string: ID of `PlantData` entry this seed grows (e.g., `"plant_corn"`)                                                                                                                                                                                                                                                                                                                                                                                                |
| `2`        | Crop       | _(none)_                                                                           |                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `3`        | Pollen     | `sourcePlantId`<br>`pollinationSuccessChance`<br>`viabilityDays`<br>`crossResults` | string: `plantId` of the plant that produced this pollen (e.g., `"plant_corn"`)<br>float: Chance of pollination success (e.g., `0.5`)<br>int: Days the pollen remains viable (e.g., `3`)<br>array: Cross-breeding table — `[{ "targetPlantId": "string", "resultPlantId": "string" }]`. Each entry maps a receiver `plantId` to the hybrid `plantId` that spawns when this pollen is applied to it. Consumed by `PollenData.FindResultPlantId()` in the Unity client. |
| `4`        | Consumable | `energyRestore`<br>`healthRestore`<br>`bufferDuration`                             | int: Stamina restored<br>int: Health restored<br>float: Buff duration                                                                                                                                                                                                                                                                                                                                                                                                 |
| `5`        | Material   | _(none)_                                                                           |                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `6`        | Weapon     | `damage`<br>`critChance`<br>`attackSpeed`<br>`weaponMaterialId`                    | int: Base damage (e.g., 10)<br>int: Crit chance % (e.g., 5)<br>float: Attack speed (e.g., 1.0)<br>string: `materialId` of a Material document (e.g., `"mat_steel"`). See [Material Catalog](#material-catalog).                                                                                                                                                                                                                                                       |
| `7`        | Fish       | `difficulty`<br>`fishingSeasons`<br>`isLegendary`                                  | int: Difficulty level (e.g., 1)<br>int[]: 0=Sunny, 1=Rainy (e.g., `[0,1]`)<br>bool: (default `false`)                                                                                                                                                                                                                                                                                                                                                                 |
| `8`        | Cooking    | `energyRestore`<br>`healthRestore`<br>`bufferDuration`                             | int: Stamina restored<br>int: Health restored<br>float: Buff duration                                                                                                                                                                                                                                                                                                                                                                                                 |
| `9`        | Forage     | `foragingSeasons`<br>`energyRestore`                                               | int[]: 0=Sunny, 1=Rainy (e.g., `[0,1]`)<br>int: (default `5`)                                                                                                                                                                                                                                                                                                                                                                                                         |
| `10`       | Resource   | `isOre`<br>`requiresSmelting`<br>`smeltedResultId`                                 | bool: (default `false`)<br>bool: (default `false`)<br>string: ID of smelt output (default `""`)                                                                                                                                                                                                                                                                                                                                                                       |
| `11`       | Gift       | `isUniversalLike`<br>`isUniversalLove`                                             | bool: (default `false`)<br>bool: (default `false`)                                                                                                                                                                                                                                                                                                                                                                                                                    |
| `12`       | Quest      | `relatedQuestID`<br>`autoConsume`                                                  | string: Related quest ID (e.g., `"quest_goblins_01"`)<br>bool: (default `false`)                                                                                                                                                                                                                                                                                                                                                                                      |
| `13`       | Structure  | `structureInteractionType`                                                         | int: 0=Storage, 1=Crafting, 2=Smelting, 3=Fence, 4=Decoration                                                                                                                                                                                                                                                                                                                                                                                                         |
| `14`       | Fertilizer | _(none)_                                                                           | Stackable fertilizer item consumed on successful crop fertilization                                                                                                                                                                                                                                                                                                                                                                                                   |

---

### Fertilizer Catalog

> Dedicated CRUD facade for fertilizer entries in the item catalog. These routes always persist `itemType = 14` and are intended for stackable fertilizer items used by the crop fertilizing mechanic.

#### HTTP Endpoints

- **POST** `/game-data/fertilizers/create`: Create a new fertilizer definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:
    - `icon` _(file, required)_ — Fertilizer icon image (max 5 MB). Uploaded to Cloudinary internally; `iconUrl` set automatically.
    - Shared base item fields as form-data text fields: `itemID`, `itemName`, `description`, `itemCategory`, `maxStack`, `isStackable`, `basePrice`, `buyPrice`, `canBeSold`, `canBeBought`, `isQuestItem`, `isArtifact`, `isRareItem`, `npcPreferenceNames`, `npcPreferenceReactions`.
  - Response: Saved fertilizer document including `_id`, `iconUrl`, and `itemType: 14`
  - Note: Returns `409 Conflict` if an item with the same `itemID` already exists

- **GET** `/game-data/fertilizers/catalog`: Get the fertilizer catalog in Unity-client format.
  - Response: `{ "items": [ ...fertilizerObjects ] }`

- **GET** `/game-data/fertilizers/all`: Get flat array of all fertilizer documents.
  - Response: `[ ...fertilizerObjects ]`

- **GET** `/game-data/fertilizers/by-item-id/:itemID`: Find fertilizer by game-side string ID.
  - Path param: `itemID` - Snake_case string identifier (e.g., `fertilizer_basic`)
  - Response: Fertilizer document or `null`

- **GET** `/game-data/fertilizers/:id`: Find fertilizer by MongoDB `_id`.
  - Path param: `id` - MongoDB ObjectId string
  - Response: Fertilizer document or `null`

- **PUT** `/game-data/fertilizers/:itemID`: Update an existing fertilizer by game-side `itemID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Path param: `itemID` - game-side string identifier (e.g., `fertilizer_basic`)
  - Fields: Any subset of the shared base item fields as form-data text fields (all optional). Include an `icon` file to replace the icon (max 5 MB, re-uploaded to Cloudinary automatically).
  - Response: Updated fertilizer document
  - Note: Returns `404` if fertilizer not found

- **DELETE** `/game-data/fertilizers/:itemID`: Delete a fertilizer by game-side `itemID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `itemID` - game-side string identifier (e.g., `fertilizer_basic`)
  - Response: Deleted fertilizer document
  - Note: Returns `404` if fertilizer not found

#### Fertilizer Fields

| Field                    | Type     | Required     | Notes                                                   |
| ------------------------ | -------- | ------------ | ------------------------------------------------------- |
| `itemID`                 | string   | ✅           | Unique fertilizer identifier used by the client         |
| `itemName`               | string   | ✅           | Display name                                            |
| `description`            | string   | ✅           | Item tooltip text                                       |
| `icon`                   | file     | ✅ on create | Uploaded icon image; gateway stores resulting `iconUrl` |
| `itemCategory`           | int      | ✅           | Category enum value, typically Farming                  |
| `maxStack`               | int      | ✅           | Maximum stack size                                      |
| `isStackable`            | bool     | ✅           | Typically `true` for fertilizer                         |
| `basePrice`              | int      | ✅           | Base sell price                                         |
| `buyPrice`               | int      | ✅           | Shop price                                              |
| `canBeSold`              | bool     | ✅           | Whether fertilizer can be sold                          |
| `canBeBought`            | bool     | ✅           | Whether fertilizer can be bought                        |
| `isQuestItem`            | bool     | ✅           | Quest flag                                              |
| `isArtifact`             | bool     | ✅           | Artifact flag                                           |
| `isRareItem`             | bool     | ✅           | Rare item flag                                          |
| `npcPreferenceNames`     | string[] | —            | Optional NPC preference names                           |
| `npcPreferenceReactions` | int[]    | —            | Optional NPC reaction values                            |

`itemType` is forced to `14` by the gateway and does not need to be supplied.

---

### Combat Skills Catalog

> DB-driven combat-skill definitions consumed by the Unity combat system. Skills are independent catalog entities and not item documents.

#### HTTP Endpoints

- **POST** `/game-data/combat-skills/create`: Create a new combat skill (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `application/json`
  - Body: Skill definition payload (see fields table below)
  - Response: Saved combat skill document
  - Note: Returns `409 Conflict` if `skillId` already exists

- **GET** `/game-data/combat-skills/catalog`: Get combat skill catalog in Unity format.
  - Response: `{ "skills": [ ...combatSkillObjects ] }`
  - Note: Consumed by Game-side combat skill catalog service

- **GET** `/game-data/combat-skills/all`: Get flat array of all combat skill documents.
  - Response: `[ ...combatSkillObjects ]`

- **GET** `/game-data/combat-skills/by-skill-id/:skillId`: Find combat skill by game-side ID.
  - Path param: `skillId` - string ID (e.g., `skill_weapon_staff_special`)
  - Response: Combat skill document

- **PUT** `/game-data/combat-skills/:skillId`: Update combat skill by `skillId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `application/json`
  - Body: Any subset of skill fields
  - Response: Updated combat skill document
  - Note: Returns `404` if skill not found

- **DELETE** `/game-data/combat-skills/:skillId`: Delete combat skill by `skillId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `skillId`
  - Response: Deleted combat skill document
  - Note: Returns `404` if skill not found

#### Combat Skill Fields

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `skillId` | string | ✅ | Unique game-side skill ID |
| `skillName` | string | ✅ | Display name |
| `skillDescription` | string | — | Tooltip/description |
| `iconUrl` | string | — | CDN icon URL |
| `ownership` | enum | — | `PlayerSkill` or `WeaponSkill` |
| `category` | enum | — | `None`, `Projectile`, `Slash`, `AoE`, `Buff`, `Summon` |
| `requiredWeaponType` | number | — | Numeric weapon type gate for weapon skills |
| `cooldown` | number | — | Seconds |
| `diceTier` | enum | — | `D6`, `D8`, `D10`, `D12`, `D20` |
| `skillMultiplier` | number | — | Damage multiplier |
| `projectilePrefabKey` | string | — | Client prefab key resolver input |
| `projectileSpeed` | number | — | Projectile speed |
| `projectileRange` | number | — | Projectile max range |
| `projectileKnockback` | number | — | Projectile knockback force |
| `slashVfxKey` | string | — | Client slash VFX prefab key |
| `slashVfxDuration` | number | — | Slash VFX lifetime |
| `slashVfxSpawnOffset` | number | — | Forward spawn offset |
| `slashVfxPositionOffsetX` | number | — | Additional X offset |
| `slashVfxPositionOffsetY` | number | — | Additional Y offset |
| `slashKnockbackForce` | number | — | Slash hit knockback force |
| `damagePopupPrefabKey` | string | — | Client damage popup prefab key |

---

### Plants Catalog

> Mirrors Unity `PlantData` / `PlantCatalogResponse` model consumed by `PlantCatalogService.cs`. The `plantId` field on Seed items (`itemType: 1`) links to plant documents here.

#### HTTP Endpoints

- **POST** `/game-data/plants/create`: Create a new plant definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - **File fields** (each max 5 MB, uploaded to Cloudinary folder `plant-sprites` automatically):

    | Field name           | Required    | Description                                                                                                                                                            |
    | -------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
    | `stageSprites`       | ✅          | Repeated file field — files are assigned to stages by their order in the form (first file → stage 0, second → stage 1, etc.). Count must match `growthStages` entries. |
    | `hybridFlowerSprite` | Hybrid only | Sprite at `pollenStage` (sets `hybridFlowerIconUrl`)                                                                                                                   |
    | `hybridMatureSprite` | Hybrid only | Sprite at `pollenStage + 1` (sets `hybridMatureIconUrl`)                                                                                                               |

  - **Text fields**: All other plant fields as form-data strings, except:
    - `growthStages` — Send as **JSON string**, e.g., `[{"stageNum":0,"growthDurationMinutes":0},{"stageNum":1,"growthDurationMinutes":30}]`. `stageIconUrl` filled automatically from uploaded sprites.
  - Response: Saved plant document including `_id` and all resolved `stageIconUrl` CDN URLs
  - Note: Returns `409 Conflict` if a plant with the same `plantId` already exists

- **GET** `/game-data/plants/catalog`: Get full plant catalog in Unity-client format.
  - Response: `{ "plants": [ ...plantObjects ] }`
  - Note: Consumed by `PlantCatalogService.cs` (`catalogApiUrl` field)

- **GET** `/game-data/plants/all`: Get flat array of all plant documents.
  - Response: `[ ...plantObjects ]`

- **GET** `/game-data/plants/by-plant-id/:plantId`: Find plant by game-side string ID.
  - Path param: `plantId` - Snake_case string identifier (e.g., `plant_corn`)
  - Response: Plant document or `null`

- **GET** `/game-data/plants/:id`: Find plant by MongoDB `_id`.
  - Path param: `id` - MongoDB ObjectId string
  - Response: Plant document or `null`

- **PUT** `/game-data/plants/:plantId`: Update an existing plant by game-side `plantId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Path param: `plantId` - game-side string identifier (e.g., `plant_corn`)
  - **Optional file fields** (each max 5 MB, re-uploaded to Cloudinary `plant-sprites`):

    | Field name           | Description                                                                                                                                                                                                                                                                                                                          |
    | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
    | `stageSprites`       | Repeated file field — replaces all stage sprites. Assigned by form order (1st file → stage 0, etc.). Must be sent together with a `growthStages` JSON body field; file count must match stage count. When omitted, each stage in the `growthStages` JSON **must include its existing `stageIconUrl`** (Mongoose requires the field). |
    | `hybridFlowerSprite` | Replaces `hybridFlowerIconUrl`. Can be sent independently without `growthStages`.                                                                                                                                                                                                                                                    |
    | `hybridMatureSprite` | Replaces `hybridMatureIconUrl`. Can be sent independently without `growthStages`.                                                                                                                                                                                                                                                    |

  - **Optional text fields**: Any subset of plant fields (all optional). `growthStages` as JSON string if replacing stages. When updating stage data without uploading new sprites, each stage entry must include `stageIconUrl` (copy existing CDN URL) to pass Mongoose validation.
  - Response: Updated plant document
  - Note: Returns `404` if plant not found

- **DELETE** `/game-data/plants/:plantId`: Delete a plant by game-side `plantId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `plantId` - game-side string identifier (e.g., `plant_corn`)
  - Response: Deleted plant document
  - Note: Returns `404` if plant not found

### Achievement Definitions (Admin)

- **POST** `/game-data/achievements/create`: Create a new achievement definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body:
    ```json
    {
      "achievementId": "string",
      "name": "string",
      "description": "string",
      "requirements": [
        {
          "type": "KILL | HARVEST | PLANT | CRAFT | FISH | COLLECT | DISCOVER | QUEST_COMPLETE | REACH_LEVEL | COOK | TRADE",
          "target": 1,
          "entityId": "string (optional)",
          "label": "string"
        }
      ]
    }
    ```

- **GET** `/game-data/achievements/all`: Get all achievement definitions.
  - Response: Array of achievement definitions

- **GET** `/game-data/achievements/:achievementId`: Get one achievement definition by `achievementId`.
  - Response: Achievement definition object

- **PUT** `/game-data/achievements/:achievementId`: Update one achievement definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body: Any subset of `name`, `description`, `requirements`

- **DELETE** `/game-data/achievements/:achievementId`: Delete one achievement definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`

#### Plant Fields

| Field                       | Type        | Required | Default | Notes                                                                                                                                                                                                                                             |
| --------------------------- | ----------- | -------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `plantId`                   | string      | ✅       | —       | Unique game-side ID (e.g., `"plant_corn"`)                                                                                                                                                                                                        |
| `plantName`                 | string      | ✅       | —       | Display name                                                                                                                                                                                                                                      |
| `growthStages`              | JSON string | ✅       | —       | Stringified array of `{ stageNum, growthDurationMinutes }` objects (at least 1 entry). On create, omit `stageIconUrl` — filled automatically from uploaded sprites. On update, include `stageIconUrl` in each entry if not uploading new sprites. |
| `harvestedItemId`           | string      | ✅       | —       | `itemID` of crop/item dropped on harvest (from ItemCatalog)                                                                                                                                                                                       |
| `canProducePollen`          | bool        | —        | `false` | Whether pollen can be collected                                                                                                                                                                                                                   |
| `pollenStage`               | int         | —        | `3`     | Stage index at which pollen becomes collectible                                                                                                                                                                                                   |
| `pollenItemId`              | string      | —        | —       | `itemID` of pollen item given on collection                                                                                                                                                                                                       |
| `maxPollenHarvestsPerStage` | int         | —        | `1`     | `0` = unlimited                                                                                                                                                                                                                                   |
| `growingSeason`             | int         | —        | `0`     | `0` = Sunny, `1` = Rainy                                                                                                                                                                                                                          |
| `isHybrid`                  | bool        | —        | `false` | `true` for cross-breeding result plants                                                                                                                                                                                                           |
| `receiverPlantId`           | string      | —        | —       | `plantId` of plant that received pollen _(hybrid only)_                                                                                                                                                                                           |
| `pollenPlantId`             | string      | —        | —       | `plantId` of plant whose pollen was applied _(hybrid only)_                                                                                                                                                                                       |
| `hybridFlowerIconUrl`       | string      | —        | —       | **Auto-filled** from `hybridFlowerSprite` file upload _(hybrid only)_                                                                                                                                                                             |
| `hybridMatureIconUrl`       | string      | —        | —       | **Auto-filled** from `hybridMatureSprite` file upload _(hybrid only)_                                                                                                                                                                             |
| `dropSeeds`                 | bool        | —        | `false` | When `false`, harvest never generates seeds _(hybrid only)_                                                                                                                                                                                       |

#### `growthStages` Entry Fields

| Field                   | Type   | Notes                                                                                                                                                                                                                                                                                                     |
| ----------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `stageNum`              | int    | Stage index (0-based)                                                                                                                                                                                                                                                                                     |
| `growthDurationMinutes` | float  | In-game minutes to grow through this stage (e.g., `60` = 1 in-game hour)                                                                                                                                                                                                                                  |
| `stageIconUrl`          | string | **Auto-filled** by gateway from `stageSprites` uploads (assigned by form order: 1st file → stage 0, 2nd → stage 1, etc.). On **create**, do not include in the JSON (gateway injects it). On **update without sprites**, include the existing CDN URL in each stage object — Mongoose requires the field. |

---

### Crafting Recipes

> Crafting recipe definitions consumed by the game client. Managed by `admin-service`.

#### HTTP Endpoints

- **POST** `/game-data/crafting-recipes/create`: Create a new crafting recipe (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Body (application/json):
    ```json
    {
      "recipeID": "string",
      "recipeName": "string",
      "description": "string",
      "recipeType": 0,
      "category": 0, //int 0-general, 1-tool, 2-food, 3-materials, 4-furniture, 5-equipment
      "resultItemId": "string",
      "resultQuantity": 1,
      "resultQuality": 0,
      "ingredients": [{ "itemId": "string", "quantity": 1 }],
      "isUnlockedByDefault": false
    }
    ```
  - Response: Created recipe document (includes `_id` and all fields)
  - Note: Returns `409 Conflict` if a recipe with the same `recipeID` already exists.

- **GET** `/game-data/crafting-recipes/catalog`: Get full recipe catalog in Unity-client format.
  - Response: `{ "recipes": [ ...recipeObjects ] }`

- **GET** `/game-data/crafting-recipes/all`: Get flat array of all recipe documents.
  - Response: `[ ...recipeObjects ]`

- **GET** `/game-data/crafting-recipes/by-recipe-id/:recipeID`: Find recipe by game-side string ID.
  - Path param: `recipeID` - Snake_case string identifier (e.g., `recipe_wooden_plank`)
  - Response: Recipe document or `null`

- **GET** `/game-data/crafting-recipes/:id`: Find recipe by MongoDB `_id`.
  - Path param: `id` - MongoDB ObjectId string
  - Response: Recipe document or `null`

- **PUT** `/game-data/crafting-recipes/:recipeID`: Update an existing recipe by game-side `recipeID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `recipeID` - game-side string identifier
  - Body (application/json, all fields optional):
    ```json
    {
      "recipeName": "string",
      "description": "string",
      "recipeType": 0,
      "category": 0,
      "resultItemId": "string",
      "resultQuantity": 1,
      "resultQuality": 0,
      "ingredients": [{ "itemId": "string", "quantity": 1 }],
      "isUnlockedByDefault": false
    }
    ```
  - Response: Updated recipe document

- **DELETE** `/game-data/crafting-recipes/:recipeID`: Delete a recipe by game-side `recipeID` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `recipeID` - game-side string identifier (e.g., `recipe_wooden_plank`)
  - Response: Deleted recipe document
  - Note: Returns `404` if recipe not found

---

### Skin Configs (Paper Doll)

> Spritesheet catalog for the layered Paper Doll system. Unity's `SkinCatalogManager` fetches this on startup to build its sprite dictionary. Each entry maps a `configId` to a hosted PNG spritesheet that is sliced at runtime into a `Sprite[]`.

#### HTTP Endpoints

- **GET** `/game-data/skin-configs`: Get the full skin catalog (public, no auth required).
  - Optional query param: `layer` — filter by layer (e.g., `?layer=outfit`).
  - Response:
    ```json
    [
      {
        "_id": "string",
        "configId": "string",
        "spritesheetUrl": "string",
        "cellSize": 64,
        "displayName": "string",
        "layer": "string",
        "createdAt": "ISO date",
        "updatedAt": "ISO date"
      }
    ]
    ```
  - Note: Consumed by `SkinCatalogManager.cs` in Unity client on startup.

- **POST** `/game-data/skin-configs`: Create a new skin config entry (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:

    | Field         | Type       | Required | Notes                                                                                                   |
    | ------------- | ---------- | -------- | ------------------------------------------------------------------------------------------------------- |
    | `spritesheet` | file (PNG) | ✅       | Spritesheet image, max 10 MB. Uploaded to Cloudinary folder `skin-spritesheets` automatically.          |
    | `configId`    | text       | ✅       | Stable string key used by Unity (e.g., `"farmer_base"`, `"frog_outfit"`). Used as Cloudinary public ID. |
    | `displayName` | text       | ✅       | Human-readable label shown in the admin panel.                                                          |
    | `cellSize`    | text (int) | —        | Width and height of each sprite cell in pixels. Default: `64`.                                          |
    | `layer`       | text       | —        | Paper Doll layer: `body`, `tool`, `hair`, `hat`, `outfit`, etc. Default: `body`.                        |

  - Response: Created skin config document including `_id` and the Cloudinary `spritesheetUrl`.
  - Note: Returns `409 Conflict` if a skin config with the same `configId` already exists.

- **PUT** `/game-data/skin-configs/:configId`: Update an existing skin config (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Path param: `configId` — the stable string key (e.g., `farmer_base`).
  - Fields (all optional):

    | Field         | Type       | Notes                                                                                                                                                          |
    | ------------- | ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
    | `spritesheet` | file (PNG) | New spritesheet, max 10 MB. When provided, re-uploads to Cloudinary using `configId` as public ID and updates `spritesheetUrl`. Omit to keep the existing URL. |
    | `displayName` | text       | New display name.                                                                                                                                              |
    | `cellSize`    | text (int) | New cell size in pixels.                                                                                                                                       |
    | `layer`       | text       | New layer tag.                                                                                                                                                 |

  - Response: Updated skin config document.
  - Note: Returns `404` if no entry with this `configId` exists.

- **DELETE** `/game-data/skin-configs/:configId`: Delete a skin config entry (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `configId` — the stable string key (e.g., `farmer_base`).
  - Response: `204 No Content`
  - Note: Returns `404` if not found. Does **not** delete the Cloudinary asset — remove it manually in the Cloudinary dashboard if needed.

#### Skin Config Fields

| Field            | Type   | Required | Default  | Notes                                                                                          |
| ---------------- | ------ | -------- | -------- | ---------------------------------------------------------------------------------------------- |
| `configId`       | string | ✅       | —        | Unique stable key used by Unity (e.g., `"farmer_base"`, `"frog_outfit"`, `"copper_tool"`).     |
| `spritesheetUrl` | string | ✅       | —        | Public Cloudinary URL of the PNG. **Set automatically** from the uploaded `spritesheet` file.  |
| `cellSize`       | int    | —        | `64`     | Width = height of each sprite frame in pixels. Must divide evenly into the sheet dimensions.   |
| `displayName`    | string | ✅       | —        | Label shown in the admin panel.                                                                |
| `layer`          | string | —        | `"body"` | Which Paper Doll layer this sheet belongs to. Values: `body`, `tool`, `hair`, `hat`, `outfit`. |

#### Unity Integration Notes

- Unity `SkinCatalogManager` calls `GET /game-data/skin-configs` on startup.
- Each entry's `spritesheetUrl` PNG is downloaded (or served from disk cache at `Application.persistentDataPath/SkinCache/<configId>.png`), decoded, and sliced into a `Sprite[]` keyed by `configId`.
- `DynamicSpriteSwapper` on each Paper Doll layer reads `configId` from `EquipmentManager` and calls `SkinCatalogManager.GetSprites(configId)` every `LateUpdate`.
- To clear the client-side disk cache after updating a spritesheet, call `SkinCatalogManager.Instance.RefreshCatalog()` in play mode or delete `Application.persistentDataPath/SkinCache/`.
- **Tool layer spritesheets are NOT stored here.** Tool appearance is driven by the Material Catalog (see below). `MaterialCatalogService.cs` registers each material's spritesheet into `SkinCatalogManager` under its `materialId` on startup.

---

### Material Catalog

> Standalone collection for tool/weapon composition materials. Completely separate from `itemType: 5` (Material) inventory items. Each document owns the tool-layer spritesheet for that material tier. Unity `MaterialCatalogService` fetches this on startup and registers each sheet into `SkinCatalogManager` using `materialId` as the `configId`.
>
> **Seeding order:** Create Material documents **before** creating Tool/Weapon items that reference them via `toolMaterialId` / `weaponMaterialId`.

#### HTTP Endpoints

- **GET** `/game-data/materials/catalog`: Get full material catalog in Unity-client format (public).
  - Response: `{ "materials": [ ...materialObjects ] }` sorted by `materialTier` ascending.
  - Note: Consumed by `MaterialCatalogService.cs` in Unity on startup.

- **GET** `/game-data/materials`: Get flat array of all material documents (public).
  - Response: `[ ...materialObjects ]`

- **GET** `/game-data/materials/:materialId`: Get a single material by `materialId` (public).
  - Path param: `materialId` — stable string key (e.g., `mat_copper`)
  - Response: Material document or `null`

- **POST** `/game-data/materials`: Create a new material (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:

    | Field          | Type       | Required | Notes                                                                                                           |
    | -------------- | ---------- | -------- | --------------------------------------------------------------------------------------------------------------- |
    | `spritesheet`  | file (PNG) | ✅       | Tool-layer animation sheet, max 10 MB. Uploaded to Cloudinary folder `material-spritesheets` automatically.     |
    | `materialId`   | text       | ✅       | Stable string key (e.g., `mat_copper`). Used as Cloudinary public ID and as `configId` in `SkinCatalogManager`. |
    | `materialName` | text       | ✅       | Display name (e.g., `Copper`).                                                                                  |
    | `materialTier` | text (int) | —        | Numeric tier for stat scaling. Default: `1`.                                                                    |
    | `cellSize`     | text (int) | —        | Sprite cell width/height in pixels. Default: `64`.                                                              |
    | `description`  | text       | —        | Optional flavour text.                                                                                          |

  - Response: Created material document including `_id` and `spritesheetUrl`.
  - Note: Returns `409 Conflict` if a material with the same `materialId` already exists.

- **PUT** `/game-data/materials/:materialId`: Update an existing material (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Path param: `materialId` — stable string key (e.g., `mat_copper`)
  - Fields (all optional):

    | Field          | Type       | Notes                                                                                                      |
    | -------------- | ---------- | ---------------------------------------------------------------------------------------------------------- |
    | `spritesheet`  | file (PNG) | New sheet, max 10 MB. Re-uploads to Cloudinary using `materialId` as public ID. Omit to keep existing URL. |
    | `materialName` | text       | New display name.                                                                                          |
    | `materialTier` | text (int) | New tier value.                                                                                            |
    | `cellSize`     | text (int) | New cell size in pixels.                                                                                   |
    | `description`  | text       | New description.                                                                                           |

  - Response: Updated material document.
  - Note: Returns `404` if not found.

- **DELETE** `/game-data/materials/:materialId`: Delete a material (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `materialId` — stable string key (e.g., `mat_copper`)
  - Response: Deleted material document.
  - Note: Returns `404` if not found. Does **not** delete the Cloudinary asset.

#### Material Fields

| Field            | Type   | Required | Default | Notes                                                                                                 |
| ---------------- | ------ | -------- | ------- | ----------------------------------------------------------------------------------------------------- |
| `materialId`     | string | ✅       | —       | Stable string key (e.g., `"mat_copper"`). Also used as `configId` in `SkinCatalogManager`.            |
| `materialName`   | string | ✅       | —       | Display name (e.g., `"Copper"`).                                                                      |
| `materialTier`   | int    | —        | `1`     | Numeric tier for future stat scaling. Higher = stronger material.                                     |
| `spritesheetUrl` | string | ✅       | —       | Cloudinary URL of the tool animation PNG. **Set automatically** from the uploaded `spritesheet` file. |
| `cellSize`       | int    | —        | `64`    | Uniform sprite cell width/height in pixels.                                                           |
| `description`    | string | —        | `""`    | Optional flavour text.                                                                                |

#### Unity Integration Notes

- `MaterialCatalogService.cs` calls `GET /game-data/materials/catalog` on startup.
- For each entry, `SkinCatalogManager.Instance.LoadExternalSheet(materialId, spritesheetUrl, cellSize)` is called, registering the sheet under `materialId` as the configId.
- `ItemUsageController` looks up `tool.toolMaterialId` via `MaterialCatalogService.Instance.GetMaterial(id)` and passes `materialEntry.materialId` directly to `EquipmentManager.EquipTool()` as the configId.
- To add a new material tier: `POST /game-data/materials` once — no code changes required.

---

### Quest Catalog

> Defines quest templates (quest chain config, objectives, NPC assignments, rewards) served via `player-data-service`. All mutation endpoints require admin authentication.

- **POST** `/game-data/quests`: Create a new quest definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `application/json`
  - Body:
    ```json
    {
      "questId": "string",
      "questName": "string",
      "description": "string",
      "NPCName": "string",
      "Weight": 1,
      "nextQuestId": "string (optional)",
      "reward": { "itemId": "string", "quantity": 1 },
      "status": "inactive (optional, default 'inactive')",
      "objectives": [
        {
          "objectiveId": "string",
          "description": "string",
          "itemId": "string",
          "requiredAmount": 5,
          "currentAmount": 0
        }
      ]
    }
    ```
  - Response: Created quest document.
  - Note: Returns `409` if `questId` already exists.

- **GET** `/game-data/quests/catalog`: Get full catalog `{ quests: [...] }` for Unity client (public).
  - Response:
    ```json
    { "quests": [ { "questId": "string", ... } ] }
    ```

- **GET** `/game-data/quests/all`: Get flat array of all quests (public).
  - Response: Array of quest documents.

- **GET** `/game-data/quests/by-quest-id/:questId`: Find a quest by game-side `questId` string (public).
  - Path param: `questId` — game-side identifier (e.g., `quest_001`).
  - Response: Quest document or `null`.

- **GET** `/game-data/quests/:id`: Find a quest by MongoDB `_id` (public).
  - Path param: `id` — MongoDB ObjectId string.
  - Response: Quest document.

- **PUT** `/game-data/quests/:questId`: Update a quest by game-side `questId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `application/json`
  - Path param: `questId` — game-side questId (e.g., `quest_001`).
  - Body: Any subset of quest fields (all optional).
  - Response: Updated quest document.
  - Note: Returns `404` if not found.

- **DELETE** `/game-data/quests/:questId`: Delete a quest by game-side `questId` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `questId`
  - Response: Deleted quest document.
  - Note: Returns `404` if not found.

#### Quest Fields

| Field         | Type   | Required | Default      | Notes                                                                |
| ------------- | ------ | -------- | ------------ | -------------------------------------------------------------------- |
| `questId`     | string | ✅       | —           | Unique game-side identifier (e.g., `"quest_001"`).                   |
| `questName`   | string | ✅       | —           | Display name of the quest.                                           |
| `description` | string | ✅       | —           | Quest description shown to the player.                               |
| `NPCName`     | string | ✅       | —           | Name of the NPC who assigns the quest.                               |
| `Weight`      | number | ✅       | `1`          | Sorting/priority weight. Higher = appears first.                     |
| `nextQuestId` | string | ❌       | —           | `questId` of the next quest in the chain (optional).                 |
| `reward`      | object | ✅       | —           | `{ itemId: string, quantity: number }` — reward on completion.       |
| `status`      | string | ❌       | `"inactive"` | Quest lifecycle status: `inactive`, `active`, `completed`, `failed`. |
| `objectives`  | array  | ❌       | `[]`         | Array of `QuestObjective` sub-documents (see below).                 |

#### QuestObjective Fields

| Field            | Type   | Required | Default | Notes                                           |
| ---------------- | ------ | -------- | ------- | ----------------------------------------------- |
| `objectiveId`    | string | ✅       | —       | Unique identifier within the quest.             |
| `description`    | string | ✅       | —       | Description of the objective.                   |
| `itemId`         | string | ✅       | —       | Target item `itemId` for this objective.         |
| `requiredAmount` | int    | ✅       | —       | Number of items needed.                         |
| `currentAmount`  | int    | ❌       | `0`     | Current progress (player-side tracking).        |

---

### Resource Config Catalog

> Defines harvestable world resources (trees, rocks, etc.) consumed by Unity `ResourceCatalogManager` and runtime host-authoritative spawn/HP logic.
>
> **Status:** In progress. Schema/DTO support is in place in `admin-service`, but gateway HTTP routes are not yet fully wired.

#### Planned HTTP Endpoints

- **GET** `/game-data/resource-configs/catalog`: Get full resource catalog in Unity-client format (public).
  - Response:
    ```json
    {
      "resources": [
        {
          "resourceId": "string",
          "name": "string",
          "maxHp": 100,
          "requiredToolType": "string",
          "minToolPower": 1,
          "spriteUrl": "string|null",
          "dropTable": [
            {
              "itemId": "string",
              "minAmount": 1,
              "maxAmount": 3,
              "dropChance": 0.5
            }
          ]
        }
      ]
    }
    ```
  - Note: Consumed by `ResourceCatalogManager.cs` on client startup.

- **POST** `/game-data/resource-configs` _(admin only, planned)_: Create a resource config.
  - Content-Type: `multipart/form-data`
  - Fields: `sprite` (file) and all Resource Config text fields (with `dropTable` as a JSON string).
- **PUT** `/game-data/resource-configs/:resourceId` _(admin only, planned)_: Update a resource config.
  - Content-Type: `multipart/form-data`
  - Fields: Optional `sprite` file to replace the current sprite, and any text fields.
- **DELETE** `/game-data/resource-configs/:resourceId` _(admin only, planned)_: Delete a resource config.

#### Resource Config Fields

| Field              | Type         | Required | Notes                                                                                   |
| ------------------ | ------------ | -------- | --------------------------------------------------------------------------------------- |
| `sprite`           | file (PNG)   | —        | Resource sprite. Uploaded to Cloudinary automatically; sets `spriteUrl`.                |
| `resourceId`       | string       | ✅       | Stable game-side identifier (e.g., `oak_tree`, `stone_rock`)                            |
| `name`             | string       | ✅       | Display name                                                                            |
| `maxHp`            | int          | ✅       | Initial HP used by host RAM state                                                       |
| `resourceType`     | string       | ✅       | Classification of the resource (`tree`, `rock`, or `ore`) for prefab/collider selection |
| `spawnWeight`      | int          | —        | Relative probability weight for random spawning within chunks (default is 1)            |
| `requiredToolType` | string       | —        | Required tool type to harvest this resource (default `Axe`)                             |
| `minToolPower`     | int          | —        | Minimum tool power required to harvest (default 1)                                      |
| `spriteUrl`        | string\|null | —        | Cloudinary URL for the resource sprite. **Auto-filled** if a `sprite` file is uploaded. |
| `dropTable`        | array        | ✅       | Array of item drops with chance and amount range                                        |

---

### Resource Interaction (Photon PUN RPC)

> Resource hit/destroy is **not** an HTTP endpoint. It is host-authoritative PUN2 RPC flow.

- Client -> Host request:
  - `RequestHitResource(chunkX, chunkY, tileIndex, damage, toolId)`
  - Sends `RPC_Host_ProcessHit(..., PhotonMessageInfo)` to `RpcTarget.MasterClient`.

- Host processing (single source of truth):
  - Validates tile is a resource in RAM.
  - Applies damage to `currentHp`.
  - Marks chunk dirty (`IsDirty = true`, `WorldSaveManager.TryMarkChunkDirty(...)`).
  - If HP > 0: broadcasts `RPC_Client_PlayHitEffect(...)`.
  - If HP <= 0: removes resource from RAM, rolls dropTable loot, spawns loot room objects, broadcasts `RPC_Client_DestroyResource(...)`.

- Client visual sync:
  - `RPC_Client_PlayHitEffect(...)` plays local hit VFX/animation.
  - `RPC_Client_DestroyResource(...)` destroys local spawned resource visual.
