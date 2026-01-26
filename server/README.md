# Game Server Microservices

A NestJS-based microservices architecture for a game server, handling authentication, player data, and API gateway functionality.

## Architecture

- **auth-service**: Handles user registration, login (Photon PUN 2 compatible), password reset, and OTP verification (TCP on port 8877).
- **player-data-service**: Manages player positions and game data (TCP on port 8878).
- **gateway-service**: HTTPS API gateway routing requests to microservices (HTTPS on port 3000, accessible from network via 0.0.0.0).
- **world-service**: Placeholder for future world-related services.

Services communicate via TCP, with the gateway exposing REST endpoints.

## Prerequisites

Before running, ensure you have:

- **Node.js** (v18 or higher): [Download here](https://nodejs.org/)
- **pnpm** (Package manager): Install globally with `npm install -g pnpm`
- **MongoDB**: Running locally on `mongodb://localhost:27017/game` (or update connection string in services)
- **SSL Certificates**: Place `localhost-key.pem` and `localhost.pem` in the `certs/` folder (for HTTPS). Generate as follows:
  1. Install `mkcert` globally: `npm install -g mkcert`
  2. Install the local CA: `mkcert -install`
  3. Generate certificates: `mkcert -key-file certs/localhost-key.pem -cert-file certs/localhost.pem localhost`
- **VS Code** (recommended): For integrated terminals and tasks.

## Environment Configuration

Each service uses environment variables for configuration. Before running, check the `.env.md` file in each service directory for details on required variables:

- [auth-service/.env.md](microservices/auth-service/.env.md)
- [player-data-service/.env.md](microservices/player-data-service/.env.md)  
- [gateway-service/.env.md](microservices/gateway-service/.env.md)

Copy the example values to `.env` files in each service folder if they don't exist.

## Setup Instructions

1. **Clone or navigate to the project**:
   ```
   cd "d:\tailieu\SEP490\New folder\server"
   ```

2. **Install dependencies**:
   ```
   pnpm install
   ```
   This installs all deps for the workspace (auth-service, player-data-service, gateway-service).

3. **Build all services** (optional, as `pnpm start` builds automatically):
   ```
   pnpm -r build
   ```

4. **Seed the database** (if needed):
   ```
   npm run seed
   ```
   This runs the seeding script for initial data.

## Running the Project

### Option 1: VS Code Tasks (Recommended for Debugging)
1. Open the project in VS Code.
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac).
3. Type "Tasks: Run Task" and select it.
4. Choose "Start All Services".
5. VS Code will open 3 integrated terminals, each running one service.

### Option 2: Command Line
Run all services in parallel:
```
npm run start:all
```
This opens separate PowerShell windows for each service.

### Individual Services
- Auth Service: `cd microservices/auth-service && pnpm start`
- Player Data Service: `cd microservices/player-data-service && pnpm start`
- Gateway Service: `cd microservices/gateway-service && pnpm start`

Services will log their status (e.g., "Auth TCP Microservice listening on port 8877").

## API Endpoints

See [ENDPOINTS.md](ENDPOINTS.md) for detailed API documentation.

## Development Notes

- **Database**: Uses MongoDB. Schemas are defined in each service.
- **Communication**: TCP for inter-service calls; HTTPS for external.
- **Linting/Testing**: Run `pnpm -r lint` or `pnpm -r test`.
- **Adding Services**: Update `pnpm-workspace.yaml`, create folder, and add to tasks/gateway.
- **Troubleshooting**:
  - Port conflicts: Change ports in `main.ts` files.
  - MongoDB issues: Ensure it's running on localhost:27017.
  - SSL errors: Regenerate certs with `mkcert localhost`.

## Contributing

- Follow NestJS best practices.
- Use pnpm for package management.
- Test endpoints with tools like Postman or curl.

For questions, check the code or ask! 🎮