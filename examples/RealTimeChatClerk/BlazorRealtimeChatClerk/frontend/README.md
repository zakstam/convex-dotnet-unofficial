# Realtime Chat — Blazor Frontend

This directory contains a Blazor WebAssembly frontend for the Realtime Chat example.

## Features

- **Real-time messaging**: Live updates using Convex subscriptions
- **User authentication**: Simple username-based login
- **Responsive design**: Works on desktop and mobile
- **Modern UI**: Clean, chat-app style interface

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Node.js (for the backend)

### Running the Application

1. **Start the backend** (from the `backend` directory):

   ```powershell
   cd ../../backend
   npm install
   npx convex dev
   ```

2. **Start the frontend** (from this directory):

   ```powershell
   dotnet run
   ```

3. Open your browser to `https://localhost:5001` (or the URL shown in the console)

## Architecture

### Components

- **Home.razor**: Main chat interface with login form and message display
- **App.razor**: Application layout and routing
- **MainLayout.razor**: Layout component

### Convex Integration

The app uses the Convex .NET client for:

- **Queries**: Loading initial messages with `getMessages`
- **Mutations**: Sending messages with `sendMessage`
- **Subscriptions**: Real-time updates using `Observe("getMessages")`

### Configuration

Convex deployment URL is configured in `wwwroot/appsettings.json`:

```json
{
  "Convex": {
    "DeploymentUrl": "https://handsome-perch-90.convex.cloud"
  }
}
```

## Development

### Building

```powershell
dotnet build
```

### Running Tests

```powershell
dotnet test
```

### Publishing

```powershell
dotnet publish -c Release
```

## Notes

- This is a client-side Blazor WebAssembly app that runs entirely in the browser
- Convex client handles real-time WebSocket connections automatically
- Messages are validated on both client and server side
- The UI automatically scrolls to show new messages
