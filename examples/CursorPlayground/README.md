# 🎨 Cursor Playground - Real-Time Cursor Tracking

An interactive, real-time cursor tracking example showcasing the **TimeBasedBatching** system with a creative "Cursor Constellations" concept.

## ✨ Features

### Phase 1: Core Cursor Tracking (Implemented)
- **Real-time cursor synchronization** across multiple users
- **Smooth interpolation** using linear interpolation (lerp) for 60fps movement
- **Constellation visualization** - dynamic lines connecting nearby cursors (within 200px)
- **User presence system** with heartbeat tracking (10-second timeout)
- **Batched cursor updates** using TimeBasedBatching (16ms sampling, 200ms batches)
- **Custom user profiles** with name, emoji, and color selection
- **Performance stats** showing FPS and batch rates

### Coming Soon
- **Phase 2**: Visual effects (cursor trails, particle effects, ripple animations)
- **Phase 3**: Interactive elements (emoji reactions, click effects, customization)
- **Phase 4**: Mini-game (Cursor Tag)
- **Phase 5**: Polish and documentation

## 🛠️ Tech Stack

- **Frontend**: Blazor WebAssembly (NET 9.0)
- **Backend**: Convex (TypeScript)
- **Real-time**: Convex subscriptions with TimeBasedBatching
- **Animation**: RequestAnimationFrame with smooth interpolation
- **Styling**: Custom CSS with gradient backgrounds and animations

## 🏗️ Project Structure

```
CursorPlayground/
├── backend/
│   └── convex/
│       ├── schema.ts              # Database schema (users, cursorBatches, reactions)
│       └── functions/
│           ├── cursorBatches.ts   # Cursor batch storage (REPLACE mode)
│           ├── users.ts           # User management and presence
│           └── reactions.ts       # Emoji reactions
└── client/
    ├── CursorPlayground.Shared/   # Shared models and business logic
    │   ├── Models/                # CursorPosition, User, CursorBatch, Reaction
    │   ├── Helpers/               # CursorBatcher (batching wrapper)
    │   └── Services/              # CursorService (Convex client wrapper)
    └── CursorPlayground.Blazor/   # Blazor WebAssembly frontend
        ├── Components/            # CursorCanvas, UserSetup
        ├── Pages/                 # Index page
        └── wwwroot/
            ├── js/cursor.js       # Mouse tracking and rendering
            └── css/cursor.css     # Styling and animations
```

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+ and npm
- Convex CLI (`npm install -g convex`)

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Set up Convex:
   ```bash
   npx convex dev
   ```

4. Copy your deployment URL (you'll need this for the frontend)

### Frontend Setup

1. Navigate to the Blazor project:
   ```bash
   cd client/CursorPlayground.Blazor
   ```

2. Update `wwwroot/appsettings.json` with your Convex deployment URL:
   ```json
   {
     "Convex": {
       "DeploymentUrl": "https://your-deployment-url.convex.cloud"
     }
   }
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open multiple browser windows/tabs to http://localhost:5050 to see the real-time cursor synchronization!

## 🎯 How It Works

### Cursor Batching
The application uses **TimeBasedBatching** with the `ForCursorTracking()` preset:
- **Sampling interval**: 16ms (~60fps)
- **Batch interval**: 200ms (5 batches/second)
- **Min distance**: 5px (deduplicate small movements)
- **Mode**: REPLACE (each user has one current batch)

### Constellation Algorithm
The constellation visualization connects cursors that are within 200px of each other:
1. Calculate distance between all cursor pairs
2. Draw gradient lines for cursors within threshold
3. Fade line opacity based on distance
4. Update at 60fps using `requestAnimationFrame`

### User Presence
Each user sends a heartbeat every 3 seconds. The backend tracks `lastSeen` timestamps and filters out users inactive for >10 seconds.

### Smooth Interpolation
Remote cursors use linear interpolation (lerp) to smoothly animate between batched positions, creating fluid 60fps movement from 5 updates/second.

## 📊 Key Components

### CursorBatcher
Wraps `TimeBasedBatcher` with cursor-specific configuration:
```csharp
var options = BatchingOptions.ForCursorTracking();
// Automatically configured for optimal cursor tracking
```

### CursorCanvas Component
Main Blazor component that:
- Tracks local mouse movement via JavaScript interop
- Batches and sends cursor positions to Convex
- Subscribes to remote cursor updates
- Renders constellation canvas
- Manages user heartbeat

### cursor.js
JavaScript module handling:
- Mouse event tracking (move, click, double-click)
- Velocity calculation for trail effects
- Smooth cursor interpolation (lerp)
- Constellation line rendering
- Remote cursor DOM management

## 🎨 Customization

Users can customize their appearance by selecting:
- **Name**: Up to 20 characters
- **Emoji**: 24 preset emojis (faces, animals, symbols)
- **Color**: 10 vibrant colors with visual preview

## 🔧 Technical Details

### Database Schema
```typescript
users: {
  name: string
  emoji: string
  color: string
  lastSeen: number
}

cursorBatches: {
  userId: Id<"users">
  events: Array<{
    eventId: string
    timestamp: number
    eventData: { x: number, y: number, velocity?: number }
  }>
  batchStartTime: number
}

reactions: {
  userId: Id<"users">
  emoji: string
  x: number
  y: number
  timestamp: number
}
```

### Performance
- **Client FPS**: 60fps rendering with smooth interpolation
- **Network batches**: 5 batches/second per user
- **Server queries**: Real-time subscriptions (no polling)
- **Bandwidth**: ~1KB per batch (highly compressed)

## 📝 License

Part of the Convex .NET examples collection. MIT License.

## 🤝 Contributing

This is an example project demonstrating TimeBasedBatching. Feel free to use it as a reference for your own real-time cursor tracking implementations!

## 📚 Learn More

- [TimeBasedBatching Documentation](../../src/Convex.Client.Extensions/Batching/TimeBasedBatching/)
- [Convex .NET Client](../../src/Convex.Client/)
- [Phase Implementation Plan](./PLAN.md)
