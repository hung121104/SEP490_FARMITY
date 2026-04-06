# FARMITY — Copilot Master Instructions

> The full architectural rulebook is in **[`.agent.md`](../.agent.md)** at the workspace root.
> When context allows, attach it via `#file:.agent.md`.
> The critical rules below are always enforced.

---

## Project Stack
- **Client:** Unity 6 (URP) + Photon PUN 2 + C#
- **Server:** NestJS + Mongoose (MongoDB Atlas) — 4 microservices: `gateway-service`, `auth-service`, `player-data-service`, `admin-service`
- **Architecture:** MVP pattern (client), Host-authoritative multiplayer, delta-batch saving

---

## Non-Negotiable Rules

### Multiplayer
- The **Room Host (MasterClient) is the Single Source of Truth**. Clients NEVER call the backend directly.
- Clients send **PUN2 RPCs** to the Host. The Host mutates RAM, sets `isDirty` flags, and syncs state back to clients.

### Saving
- **Never send HTTP per action.** All tile/inventory writes are batched by `WorldSaveManager` every ~17 seconds.
- Only dirty chunks are sent. Payload shape: `{ worldId, deltas: ChunkDeltaDto[] }` where `ChunkDeltaDto.tiles` is `Record<string, TileDataDto>` keyed on local tile index.
- Server `saveWorld()` MUST use a **MongoDB Transaction** (atomic update of world + characters + tiles + inventory).

### MongoDB Tile Storage
- Tiles in a chunk are stored as `Record<string, TileDataDto>` (Map), key = `"0"`–`"899"`.
- **Never use arrays** for tile data. Use targeted `$set` operators per tile index.

### Input
- **Never use `Input.GetKeyDown()`**. Always use `InputManager.Instance.Actions.<ActionMap>.<Action>`.
- All input goes through `FarmittyInputActions` (New Input System).

### Visuals
- **No hardcoded sprite logic.** Use `SkinCatalogManager` + `configId` for all equipment/tool sprites.
- `DynamicSpriteSwapper` runs in `LateUpdate` to sync layered renderers to the master body frame index.
- Spritesheets are fetched from the server, cached to `persistentDataPath`, sliced at runtime (uniform 64×64 grid).

### Animation
- Animation lock durations and trigger names come from the **server AnimationConfig catalog**.
- **Never hardcode `WaitForSeconds(n)` for animation timing.**

### Code Structure
- All gameplay systems live in `Assets/Scripts/MVP/<SystemName>/` with subfolders: `Model/`, `Presenter/`, `Service/`, `SO/`, `View/`.
- All enums live in `Assets/Scripts/Enum/GameEnums.cs` — single file, no new enum files.
- Views are MonoBehaviours that only call Presenter methods. Zero business logic in Views.
- Presenters are plain C# classes (non-MonoBehaviour).

### Server
- Controllers are thin — all logic in Services.
- All DTOs use `class-validator` decorators.
- Never expose a microservice port directly — all traffic goes through `gateway-service`.
