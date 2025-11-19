# Tic-Tac-Toe Game Example

A real-time multiplayer Tic-Tac-Toe game demonstrating core Convex .NET client capabilities including real-time subscriptions, mutations, and turn-based game logic.

## Overview

This example demonstrates:
- **Real-time subscriptions** - Live game board updates via `Observe<T>()`
- **Mutations** - Player moves with server-side validation
- **Queries** - Fetching game state and active games list
- **Presence tracking** - Online players and matchmaking
- **Turn-based logic** - Server-enforced game rules
- **State management** - Game lifecycle (waiting, playing, finished)

## Prerequisites

- Node.js (v18 or later) and npm
- .NET SDK 8.0 or later
- A Convex account (sign up at [convex.dev](https://www.convex.dev))

## Backend Setup

1. **Navigate to the backend directory:**
   ```bash
   cd examples/TicTacToe/backend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Initialize Convex project:**
   ```bash
   npx convex dev
   ```
   
   This will:
   - Create a new Convex project (if not already initialized)
   - Start the Convex development server
   - Deploy your functions to Convex
   - Provide you with a deployment URL (e.g., `https://your-deployment.convex.cloud`)

4. **Copy the deployment URL** - You'll need this for the client configuration.

## Client Setup

1. **Navigate to the client directory:**
   ```bash
   cd examples/TicTacToe/client
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

## Configuration

The game loads configuration from `examples/TicTacToe/appsettings.json`. Edit this file to set your Convex deployment URL:

```json
{
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud",
    "EnableDebugLogging": false
  }
}
```

**Note:** The `appsettings.json` file is included in the repository with a placeholder URL. Make sure to update it with your actual deployment URL before running the game.

### Configuration Priority

The configuration is loaded in the following order (first match wins):

1. **Environment variable** - `CONVEX_DEPLOYMENT_URL` or `CONVEX_URL`
2. **appsettings.json** - Located at `examples/TicTacToe/appsettings.json`
3. **Default** - Shows an error if not configured

## Running the Game

### Option 1: Using appsettings.json (Recommended)

1. Edit `examples/TicTacToe/appsettings.json` and set your deployment URL
2. Run the Blazor application:

```bash
cd examples/TicTacToe/client/TicTacToe.Blazor
dotnet run
```

### Option 2: Environment Variable

Set the `CONVEX_DEPLOYMENT_URL` environment variable:

**Windows (PowerShell):**
```powershell
$env:CONVEX_DEPLOYMENT_URL="https://your-deployment.convex.cloud"
cd examples/TicTacToe/client/TicTacToe.Blazor
dotnet run
```

**Windows (CMD):**
```cmd
set CONVEX_DEPLOYMENT_URL=https://your-deployment.convex.cloud
cd examples/TicTacToe/client/TicTacToe.Blazor
dotnet run
```

**Linux/macOS:**
```bash
export CONVEX_DEPLOYMENT_URL=https://your-deployment.convex.cloud
cd examples/TicTacToe/client/TicTacToe.Blazor
dotnet run
```

## How to Play

1. **Start the game:**
   - Run the Blazor application (see "Running the Game" above)
   - Enter your username when prompted

2. **Create or join a game:**
   - **Create new game**: Choose option 1 to create a new game and wait for an opponent
   - **Join existing game**: Choose option 2 and enter a game ID to join an existing game

3. **Make moves:**
   - When it's your turn, enter a number from 1-9 corresponding to the board position:
     ```
      1 | 2 | 3
     ---+---+---
      4 | 5 | 6
     ---+---+---
      7 | 8 | 9
     ```
   - The board updates in real-time as both players make moves

4. **Game ends:**
   - The game ends when a player gets 3 in a row (horizontal, vertical, or diagonal)
   - Or when all 9 squares are filled (draw)
   - Press any key to return to the main menu

## Project Structure

```
examples/TicTacToe/
├── backend/                          # Convex backend
│   └── convex/
│       ├── schema.ts                 # Database schema
│       └── functions/
│           ├── getGames.ts           # Query: List active games
│           ├── getGame.ts            # Query: Get specific game
│           ├── createGame.ts         # Mutation: Create new game
│           ├── joinGame.ts          # Mutation: Join existing game
│           ├── makeMove.ts           # Mutation: Make a move
│           ├── forfeitGame.ts        # Mutation: Forfeit game
│           └── updatePresence.ts      # Mutation: Update presence
│
├── client/                           # C# Client
│   ├── TicTacToe.Shared/            # Shared models & services
│   │   ├── Models/
│   │   │   ├── Game.cs               # Game model
│   │   │   └── Presence.cs           # Presence model
│   │   └── Services/
│   │       └── TicTacToeService.cs   # Game service
│   │
│   └── TicTacToe.Blazor/            # Blazor UI
│
├── PLAN.md                           # Implementation plan
└── README.md                         # This file
```

## Key Features Demonstrated

### Real-time Subscriptions

The game uses Convex subscriptions to receive real-time updates:

```csharp
_service.SubscribeToGame(gameId);
```

The `GameUpdated` event fires automatically when the game state changes, allowing the UI to update in real-time.

### Server-side Validation

All moves are validated on the server:
- Turn order enforcement
- Position validation (0-8)
- Occupied square checks
- Win detection

### Turn-based Logic

The server enforces turn order - players can only move when it's their turn. Invalid moves are rejected with clear error messages.

## Troubleshooting

### "Deployment URL not provided"
- Make sure you've initialized the Convex backend with `npx convex dev`
- Copy the deployment URL from the Convex dashboard or terminal output
- Provide it as a command-line argument or environment variable

### "Game not found"
- Make sure the backend is running (`npx convex dev`)
- Verify the game ID is correct
- Check that the game hasn't already finished

### "Not your turn"
- Wait for your opponent to make their move
- The board will update automatically when it's your turn

### Connection Issues
- Ensure the Convex backend is running
- Check your internet connection
- Verify the deployment URL is correct

## Next Steps

- Try running two instances of the Blazor app to play against yourself
- Explore the backend functions in the Convex dashboard
- Modify the UI to add features like move history or game statistics
- Extend the game with features like AI opponents or tournaments

## Learn More

- [Convex Documentation](https://docs.convex.dev/)
- [Convex .NET Client](https://github.com/zakstam/convex-dotnet)
- [Tic-Tac-Toe Rules](https://en.wikipedia.org/wiki/Tic-tac-toe)

