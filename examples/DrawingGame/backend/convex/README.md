# Drawing Game Backend (Convex)

Real-time multiplayer drawing and guessing game backend powered by Convex.

## Setup

1. Install dependencies:
```bash
npm install convex
```

2. Initialize Convex (if not already done):
```bash
npx convex dev
```

3. The backend will automatically deploy the schema and functions.

## API Reference

### Queries

- `getRooms()` - Get list of active game rooms
- `getRoom(roomId)` - Get room state with players
- `getStrokes(roomId, round)` - Get all drawing strokes for a round
- `getGuesses(roomId, round)` - Get all guesses for a round

### Mutations

- `createRoom(name, hostUsername, maxPlayers, roundDuration, difficulty)` - Create new game room
- `joinRoom(code, username)` - Join existing room by code
- `startGame(roomId, username)` - Start the game (host only)
- `selectWord(roomId, username, word)` - Drawer selects word to begin round
- `addStroke(roomId, points, color, thickness, tool)` - Add drawing stroke
- `submitGuess(roomId, username, guess)` - Submit word guess
- `endRound(roomId)` - End current round
- `clearCanvas(roomId, username)` - Clear canvas (drawer only)

### Actions

- `validateWord(word)` - Validate word against dictionary API
- `saveDrawingImage(drawingId, imageData)` - Save drawing as PNG to storage

### Cron Jobs

- `checkRoundTimeout` - Runs every 5 seconds to end timed-out rounds

## Database Schema

- **rooms** - Game room state
- **players** - Player information and scores
- **strokes** - Drawing stroke data
- **guesses** - Player guesses
- **drawings** - Completed drawings with metadata
