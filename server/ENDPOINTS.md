# API Endpoints

All requests go through the gateway at `https://0.0.0.0:3000` (HTTPS - accessible from network).

---

## Table of Contents

1. [Authentication & Authorization](#authentication--authorization)
   - [User Authentication](#user-authentication)
   - [Admin Authentication](#admin-authentication)
   - [Admin Password Reset](#admin-password-reset)
2. [Content Management](#content-management)
   - [Blog (Development Diary)](#blog-development-diary)
   - [News & Announcements](#news--announcements)
   - [Media Gallery](#media-gallery)
3. [Game Data Management](#game-data-management)
   - [Items Catalog](#items-catalog)
   - [Plants Catalog](#plants-catalog)
4. [Player Data](#player-data)
   - [World Management](#world-management)
   - [Character Management](#character-management)

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

### Admin Password Reset

- **POST** `/auth/admin-reset/request`: Request OTP for password reset.
  - Body: `{ "email": "string" }`
  - Response: `{ "ok": true }`
  - Note: Sends 6-digit OTP to admin email, valid for 2 minutes.

- **POST** `/auth/admin-reset/confirm`: Confirm OTP and set new password.
  - Body: `{ "email": "string", "otp": "string", "newPassword": "string" }`
  - Response: `{ "ok": true }`

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

> Managed by `admin-service`. These endpoints control the game's item and plant catalogs consumed by Unity clients.

### Items Catalog

#### HTTP Endpoints

- **POST** `/game-data/items/create`: Create a new item definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - Fields:
    - `icon` *(file, required)* — Item icon image (max 5 MB). Uploaded to Cloudinary internally; `iconUrl` set automatically.
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

- **DELETE** `/game-data/items/:id`: Delete an item by MongoDB `_id` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Response: Deleted item document
  - Note: Returns `404` if item not found

#### Base Fields

Required for ALL item types:

| Field | Type | Description |
|---|---|---|
| `itemID` | string | Unique game-side identifier (e.g., `"iron_hoe"`) |
| `itemName` | string | Display name |
| `description` | string | Flavour text |
| `iconUrl` | string | Cloudinary/CDN URL of item sprite (auto-set on upload) |
| `itemType` | int | Type discriminator (see [Item Type Fields](#itemtype-discriminator--extra-fields)) |
| `itemCategory` | int | 0=Farming, 1=Mining, 2=Fishing, 3=Cooking, etc. |
| `maxStack` | int | Maximum stack size (set to 1 for tools/weapons) |
| `isStackable` | bool | `false` for tools/weapons |
| `basePrice` | int | Base sell value |
| `buyPrice` | int | Store buy price (`0` = cannot be bought) |
| `canBeSold` | bool | Whether item can be sold |
| `canBeBought` | bool | Whether item can be bought |
| `isQuestItem` | bool | Quest-related flag |
| `isArtifact` | bool | Artifact flag |
| `isRareItem` | bool | Rarity flag |
| `npcPreferenceNames` | string[] | Optional NPC name list |
| `npcPreferenceReactions` | int[] | Optional reaction values (-2 to 2, maps 1-to-1 with names) |

#### `itemType` Discriminator & Extra Fields

Depending on `itemType`, specific extra fields must be included:

| `itemType` | Name | Required Extra Fields | Type & Notes |
|---|---|---|---|
| `0` | Tool | `toolType`<br>`toolLevel`<br>`toolPower`<br>`toolMaterial` | int: 0=Hoe, 1=WateringCan, 2=Pickaxe, 3=Axe, 4=FishingRod<br>int: Tool level (e.g., 1)<br>int: Tool power (e.g., 1)<br>int: 0=Basic, 1=Copper, 2=Steel, 3=Gold, 4=Diamond |
| `1` | Seed | `plantId` | string: ID of `PlantData` entry this seed grows (e.g., `"plant_corn"`) |
| `2` | Crop | *(none)* | |
| `3` | Pollen | `pollinationSuccessChance`<br>`viabilityDays` | float: Chance of success (e.g., 0.5)<br>int: Days pollen remains viable (e.g., 3) |
| `4` | Consumable | `energyRestore`<br>`healthRestore`<br>`bufferDuration` | int: Stamina restored<br>int: Health restored<br>float: Buff duration |
| `5` | Material | *(none)* | |
| `6` | Weapon | `damage`<br>`critChance`<br>`attackSpeed`<br>`weaponMaterial` | int: Base damage (e.g., 10)<br>int: Crit chance % (e.g., 5)<br>float: Attack speed (e.g., 1.0)<br>int: 0=Basic, 1=Copper, 2=Steel, 3=Gold, 4=Diamond |
| `7` | Fish | `difficulty`<br>`fishingSeasons`<br>`isLegendary` | int: Difficulty level (e.g., 1)<br>int[]: 0=Sunny, 1=Rainy (e.g., `[0,1]`)<br>bool: (default `false`) |
| `8` | Cooking | `energyRestore`<br>`healthRestore`<br>`bufferDuration` | int: Stamina restored<br>int: Health restored<br>float: Buff duration |
| `9` | Forage | `foragingSeasons`<br>`energyRestore` | int[]: 0=Sunny, 1=Rainy (e.g., `[0,1]`)<br>int: (default `5`) |
| `10` | Resource | `isOre`<br>`requiresSmelting`<br>`smeltedResultId` | bool: (default `false`)<br>bool: (default `false`)<br>string: ID of smelt output (default `""`) |
| `11` | Gift | `isUniversalLike`<br>`isUniversalLove` | bool: (default `false`)<br>bool: (default `false`) |
| `12` | Quest | `relatedQuestID`<br>`autoConsume` | string: Related quest ID (e.g., `"quest_goblins_01"`)<br>bool: (default `false`) |

---

### Plants Catalog

> Mirrors Unity `PlantData` / `PlantCatalogResponse` model consumed by `PlantCatalogService.cs`. The `plantId` field on Seed items (`itemType: 1`) links to plant documents here.

#### HTTP Endpoints

- **POST** `/game-data/plants/create`: Create a new plant definition (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Content-Type: `multipart/form-data`
  - **File fields** (each max 5 MB, uploaded to Cloudinary folder `plant-sprites` automatically):
    
    | Field name | Required | Description |
    |---|---|---|
    | `stageSprites` | ✅ | Repeated file field — filenames **must** end with `_<stageIndex>` (e.g., `cabbage_0.png`, `cabbage_1.png`). Files can be uploaded in any order; gateway sorts by trailing index. Count must match `growthStages` entries. |
    | `hybridFlowerSprite` | Hybrid only | Sprite at `pollenStage` (sets `hybridFlowerIconUrl`) |
    | `hybridMatureSprite` | Hybrid only | Sprite at `pollenStage + 1` (sets `hybridMatureIconUrl`) |
  
  - **Text fields**: All other plant fields as form-data strings, except:
    - `growthStages` — Send as **JSON string**, e.g., `[{"stageNum":0,"age":0},{"stageNum":1,"age":3}]`. `stageIconUrl` filled automatically from uploaded sprites.
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

- **DELETE** `/game-data/plants/:id`: Delete a plant by MongoDB `_id` (admin only).
  - Headers: `Authorization: Bearer <token>` OR Cookie: `access_token`
  - Path param: `id` - MongoDB ObjectId string
  - Response: Deleted plant document
  - Note: Returns `404` if plant not found

#### Plant Fields

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `plantId` | string | ✅ | — | Unique game-side ID (e.g., `"plant_corn"`) |
| `plantName` | string | ✅ | — | Display name |
| `growthStages` | JSON string | ✅ | — | Stringified array of `{ stageNum, age }` objects (at least 1 entry). `stageIconUrl` set automatically from uploaded sprites. |
| `harvestedItemId` | string | ✅ | — | `itemID` of crop/item dropped on harvest (from ItemCatalog) |
| `canProducePollen` | bool | — | `false` | Whether pollen can be collected |
| `pollenStage` | int | — | `3` | Stage index at which pollen becomes collectible |
| `pollenItemId` | string | — | — | `itemID` of pollen item given on collection |
| `maxPollenHarvestsPerStage` | int | — | `1` | `0` = unlimited |
| `growingSeason` | int | — | `0` | `0` = Sunny, `1` = Rainy |
| `isHybrid` | bool | — | `false` | `true` for cross-breeding result plants |
| `receiverPlantId` | string | — | — | `plantId` of plant that received pollen *(hybrid only)* |
| `pollenPlantId` | string | — | — | `plantId` of plant whose pollen was applied *(hybrid only)* |
| `hybridFlowerIconUrl` | string | — | — | **Auto-filled** from `hybridFlowerSprite` file upload *(hybrid only)* |
| `hybridMatureIconUrl` | string | — | — | **Auto-filled** from `hybridMatureSprite` file upload *(hybrid only)* |
| `dropSeeds` | bool | — | `false` | When `false`, harvest never generates seeds *(hybrid only)* |

#### `growthStages` Entry Fields

| Field | Type | Notes |
|---|---|---|
| `stageNum` | int | Stage index (0-based) |
| `age` | int | Total in-game days to reach this stage |
| `stageIconUrl` | string | **Auto-filled** by gateway — parsed from trailing index in sprite filename (e.g., `cabbage_2.png` → stage 2). Do not send manually. |

---


