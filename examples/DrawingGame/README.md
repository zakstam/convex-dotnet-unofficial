# Drawing Game (Pictionary-lite)

A real-time multiplayer drawing and guessing game built with **Blazor WebAssembly** and **Convex** backend, demonstrating high-frequency real-time updates, collaborative gameplay, and responsive canvas drawing.

## Features

- 🎨 **Real-time Drawing** - Smooth canvas drawing with HTML5 Canvas and stroke batching
- 👥 **Multiplayer** - 3-8 players per game room
- ⚡ **High-frequency Updates** - Efficient stroke batching (100ms intervals)
- 🎯 **Word Guessing** - Timed rounds with point scoring based on guess speed
- 🏆 **Scoring System** - Dynamic points based on guess timing
- 📱 **Responsive Design** - Works on desktop and mobile devices
- 🔄 **Real-time Sync** - Convex subscriptions for instant updates
- 🎮 **Game Phases** - Lobby → Waiting → Playing → Results

## Tech Stack

### Backend
- **Convex** - Real-time backend with TypeScript functions
- Database tables: rooms, players, strokes, guesses, drawings
- Scheduled functions for round timeouts
- Actions for external API integration

### Frontend
- **Blazor WebAssembly (.NET 9.0)**
- **Convex .NET Client** - Real-time subscriptions
- **HTML5 Canvas** - Drawing interface
- **JavaScript Interop** - Canvas drawing optimization

## Project Structure

```
DrawingGame/
├── backend/
│   └── convex/
│       ├── schema.ts                  # Database schema
│       ├── crons.ts                   # Scheduled functions
│       ├── lib/
│       │   └── words.ts              # Word selection helpers
│       └── functions/
│           ├── getRooms.ts           # Query: List rooms
│           ├── getRoom.ts            # Query: Room details
│           ├── getStrokes.ts         # Query: Drawing strokes
│           ├── getGuesses.ts         # Query: Player guesses
│           ├── createRoom.ts         # Mutation: Create room
│           ├── joinRoom.ts           # Mutation: Join room
│           ├── startGame.ts          # Mutation: Start game
│           ├── selectWord.ts         # Mutation: Choose word
│           ├── addStroke.ts          # Mutation: Add drawing
│           ├── submitGuess.ts        # Mutation: Submit guess
│           ├── endRound.ts           # Mutation: End round
│           ├── clearCanvas.ts        # Mutation: Clear drawing
│           └── checkRoundTimeout.ts  # Internal: Auto-end rounds
│
└── client/
    ├── DrawingGame.Shared/
    │   ├── Models/                   # Data models
    │   │   ├── Room.cs
    │   │   ├── Player.cs
    │   │   ├── Stroke.cs
    │   │   └── Guess.cs
    │   ├── Services/
    │   │   └── DrawingGameService.cs # Convex client wrapper
    │   └── Helpers/
    │       └── StrokeBatcher.cs      # Stroke batching
    │
    └── DrawingGame.Blazor/
        ├── Components/               # Razor components
        │   ├── RoomLobby.razor      # Room list & join/create
        │   ├── WaitingRoom.razor    # Pre-game lobby
        │   ├── GameView.razor       # Main game view
        │   ├── DrawingCanvas.razor  # Canvas component
        │   ├── GuessInput.razor     # Chat & guessing
        │   ├── PlayerList.razor     # Scoreboard
        │   └── GameResults.razor    # Final scores
        ├── Pages/
        │   └── Index.razor          # Main page
        └── wwwroot/
            ├── js/
            │   └── canvas.js        # Canvas interop
            └── css/
                └── app.css          # Styling
```

## Setup Instructions

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Convex CLI](https://docs.convex.dev/quickstart)

### 1. Backend Setup

```bash
# Navigate to backend directory
cd examples/DrawingGame/backend

# Install Convex
npm install convex

# Initialize Convex development server
npx convex dev
```

This will:
- Deploy the schema and functions
- Start the Convex development server
- Give you a deployment URL (save this for frontend configuration)

### 2. Frontend Setup

```bash
# Navigate to client directory
cd examples/DrawingGame/client/DrawingGame.Blazor

# Update appsettings.json with your Convex URL
# "ConvexUrl": "https://your-deployment.convex.cloud"

# Run the Blazor app
dotnet run
```

The app will be available at `https://localhost:5001` (or the port specified).

### 3. Play the Game

1. Open the app in multiple browser tabs/windows
2. Create a room (first player is the host)
3. Share the room code with other players
4. Wait for at least 2 players
5. Host starts the game
6. Take turns drawing and guessing!

## Game Rules

1. **Room Creation**: 3-8 players, configurable round duration (60-90s)
2. **Turn Order**: Each player gets one turn to draw
3. **Word Selection**: Drawer picks from 3 words (easy/medium/hard based on room settings)
4. **Drawing Phase**: Drawer has 60-90 seconds to illustrate the word
5. **Guessing**: Other players type guesses in the chat
6. **Scoring**:
   - Correct guesses earn points based on timing
   - Faster guesses = more points (100-1000 points)
   - Drawer gets points if people guess correctly
7. **Winner**: Player with highest score after all rounds

## Technical Highlights

### Stroke Batching
Drawing generates 60-120 points per second. The `StrokeBatcher` helper batches points for 100ms before sending to reduce mutation calls by 70-90%.

### Real-time Subscriptions
- **Room Updates**: Player joins/leaves, game state changes
- **Strokes**: New drawings appear instantly
- **Guesses**: Chat updates in real-time

### Canvas Performance
- Optimistic rendering on drawer's client
- Server as source of truth for stroke replay
- Timestamp-based ordering for consistency

### Round Management
- Scheduled function checks for timeouts every 5 seconds
- Automatic round advancement
- Score calculations based on elapsed time

## Development Tips

### Adding New Words
Edit `backend/convex/lib/words.ts` to add custom word lists per difficulty.

### Adjusting Stroke Batching
Modify `flushIntervalMs` in `StrokeBatcher` constructor (default: 100ms).

### Customizing Canvas
- Canvas size: 800x600 (change in `DrawingCanvas.razor`)
- Drawing tools: Pencil, eraser (add more in `canvas.js`)
- Color palette: 16 colors (customize in `DrawingCanvas.razor`)

### Testing Locally
1. Open multiple browser tabs
2. Use different usernames
3. Test round timeouts, disconnections, and rejoining

## Troubleshooting

**Issue**: Canvas not drawing
- Check browser console for JS errors
- Verify canvas.js is loaded
- Ensure Convex connection is established

**Issue**: Strokes not syncing
- Check Convex dashboard for mutation errors
- Verify stroke batching is flushing
- Check network tab for failed requests

**Issue**: Round not ending
- Check Convex logs for cron errors
- Verify `checkRoundTimeout` is running
- Ensure round duration is set correctly

## Future Enhancements

- [ ] Multiple drawing tools (line, circle, fill)
- [ ] Hint system (reveal letters over time)
- [ ] Team mode (2v2, 3v3)
- [ ] Custom word lists per room
- [ ] Drawing replay mode
- [ ] Mobile app (MAUI)
- [ ] Accessibility features (colorblind modes, screen reader support)

## License

MIT License - see LICENSE file for details

## Learn More

- [Convex Documentation](https://docs.convex.dev/)
- [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- [Convex .NET Client](https://github.com/Convex-Dev/convex-dotnet)

---

Built with ❤️ using Blazor and Convex
