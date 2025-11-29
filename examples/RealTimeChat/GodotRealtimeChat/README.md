# Godot Realtime Chat with Convex .NET Client

A fully functional real-time chat application built with **Godot 4.x** and the **Convex .NET client**, demonstrating real-time synchronization, WebSocket subscriptions, and proper thread-safe UI updates.

## Overview

This example showcases:

✅ **Real-time messaging** via WebSocket subscriptions using `Observe<T>()`
✅ **Sending messages** with `MutateAsync()` mutations
✅ **Thread-safe UI updates** with `CallDeferred()` marshalling
✅ **Connection state monitoring** and automatic reconnection
✅ **Error handling** for network failures
✅ **Multi-user synchronization** across connected clients

## Architecture

### Directory Structure

```
GodotRealtimeChat/
├── ConvexManager.cs          # Autoload singleton for Convex client
├── ChatScene.cs              # Main chat UI logic
├── ChatScene.tscn            # Scene file with UI layout
├── Models/
│   ├── MessageDto.cs         # Data model for messages
│   └── GetMessagesResponse.cs # Response format from backend
├── GodotRealtimeChat.csproj  # Project configuration
├── project.godot             # Godot engine configuration
└── README.md                 # This file
```

### Backend Functions (from RealtimeChat example)

Located in `../RealtimeChat/backend/convex/functions/`:

- **`getMessages.ts`** - Query to fetch paginated messages
- **`sendMessage.ts`** - Mutation to send new messages
- **`getOnlineUsers.ts`** - Query to get active users (optional)
- **`setTyping.ts`** - Mutation for typing indicators (optional)
- **`toggleReaction.ts`** - Mutation for emoji reactions (optional)

## Setup Instructions

### Prerequisites

- **Godot 4.2+** with .NET support enabled
- **Node.js 18+** and npm for Convex backend
- **.NET 8.0** SDK
- A **Convex account** (free tier available at https://convex.dev)

### Step 1: Deploy Convex Backend

The backend is already implemented in `../RealtimeChat/backend/`. Deploy it to your Convex space:

```bash
cd ../RealtimeChat/backend

# Install dependencies
npm install

# Deploy to Convex (creates or uses existing project)
npx convex deploy
```

This will output your deployment URL like: `https://your-deployment.convex.cloud`

### Step 2: Update Deployment URL

In `ConvexManager.cs`, line 67, replace with your actual deployment URL:

```csharp
const string deploymentUrl = "https://your-deployment.convex.cloud";
```

### Step 3: Open in Godot

1. Open Godot 4.2+
2. Click "Open Project"
3. Navigate to `examples/GodotRealtimeChat/` and select `project.godot`
4. Godot will build the C# project automatically

### Step 4: Run the Chat

Press `F5` or click "Run" to start the chat application.

## Usage

### Starting the Chat

1. **Select a username** from the dropdown (Anonymous, Alice, Bob, Charlie, Diana)
2. **Click "Reconnect"** to ensure connected to Convex
3. **Watch for "Connected" status** in the status label
4. **Type a message** and press Enter or click Send
5. **See real-time updates** as other clients send messages

### Features

#### Sending Messages
- Type message in the input field
- Press Enter or click "Send"
- Message appears immediately in your UI (optimistic update)
- All connected clients receive the message in real-time via WebSocket

#### Connection Monitoring
- Status label shows: "Connected", "Connecting...", "Disconnected", "Reconnecting..."
- Click "Reconnect" button to manually reload messages
- Client auto-reconnects on connection loss

#### Message Display
- Shows username, timestamp (relative), and message text
- Timestamps update automatically (e.g., "5m ago", "1h ago")
- Messages displayed in chronological order (oldest first)
- Auto-scrolls to newest messages

#### Error Handling
- Failed messages show error in status bar
- Network errors handled gracefully
- Input preserved on send failure
- Clear error messages for user guidance

## Code Architecture

### 1. ConvexManager (Autoload Singleton)

**Purpose**: Global access to Convex client with lifecycle management

```csharp
// Access from any scene
var client = ConvexManager.Instance.Client;

// Monitor connection state
ConvexManager.Instance.ConnectionStateChanged += (state) => {
    GD.Print($"Connection: {state}");
};
```

**Responsibilities**:
- Initialize `IConvexClient` with deployment URL
- Manage authentication tokens
- Monitor connection state changes
- Emit signals for connection/auth state changes
- Proper cleanup on exit

### 2. ChatScene (Main UI Logic)

**Purpose**: Handle message display, input, and Convex integration

**Key Methods**:

#### `LoadInitialMessages()`
Fetches initial messages from Convex using `QueryAsync<T>()`:
```csharp
var response = await _client.QueryAsync<GetMessagesResponse>(
    "functions/getMessages",
    new { limit = 50 }
);
```

#### `SubscribeToMessages()`
Subscribes to real-time updates using `Observe<T>()`:
```csharp
_client.Observe<GetMessagesResponse>("functions/getMessages", new { limit = 50 })
    .Subscribe(
        response => CallDeferred(nameof(OnNewMessagesReceived), response.Messages),
        error => GD.PrintErr($"Error: {error}")
    );
```

#### `SendMessage(string text)`
Sends a message using `MutateAsync<T>()`:
```csharp
var result = await _client.MutateAsync<MessageDto>(
    "functions/sendMessage",
    new { username = _username, text = text }
);
```

### 3. Thread Safety Pattern

**Critical**: Subscription callbacks run on background threads. Never update UI directly.

**Pattern Used**:
```csharp
// In subscription callback (background thread)
_client.Observe<T>("function").Subscribe(data => {
    // ❌ WRONG - UI access from background thread
    _label.Text = data.Text;

    // ✅ CORRECT - Marshal to main thread
    CallDeferred(nameof(UpdateUI), data);
});

// On main thread
private void UpdateUI(SomeData data) {
    // ✅ Safe to update UI here
    _label.Text = data.Text;
}
```

### 4. Data Models

**MessageDto**: Represents a single message
- `Id`: Unique message ID
- `Username`: Sender username
- `Text`: Message content
- `Timestamp`: When sent (ms since epoch)
- `Attachments`: Optional file attachments
- `GetFormattedTime()`: Returns relative time ("5m ago")

**GetMessagesResponse**: Response from `getMessages` query
- `Messages`: List of `MessageDto` objects
- `IsDone`: Whether more pages available
- `ContinueCursor`: For pagination (not used in basic version)

## Advanced Features (Optional Enhancements)

### Message Pagination

To load message history in pages:

```csharp
var paginator = client.Pagination
    .Query<MessageDto>("functions/getMessages")
    .WithPageSize(20)
    .Build();

var firstPage = await paginator.LoadNextAsync();
// Load more pages as needed
```

### Typing Indicators

Subscribe to and update typing status:

```csharp
// Set typing indicator
await client.MutateAsync("functions/setTyping", new { username = _username });

// Query active typers
var typers = await client.QueryAsync<string[]>("functions/getTypingUsers");
```

### Reactions

Add emoji reactions to messages:

```csharp
await client.MutateAsync("functions/toggleReaction", new {
    messageId = messageId,
    username = _username,
    emoji = "👍"
});
```

### Authentication

For production, use proper JWT tokens:

```csharp
var token = await GetJWTFromAuthService();
await ConvexManager.Instance.SetAuthTokenAsync(token);
```

## Troubleshooting

### "ConvexManager not initialized"
- Ensure ConvexManager is added as an autoload in project settings
- Check `project.godot` has `[autoload]` section

### "Connection refused" or "Deployment URL not found"
- Verify deployment URL in `ConvexManager.cs`
- Ensure Convex backend is deployed: `npx convex deploy`
- Check internet connectivity

### Messages not appearing
- Check Convex backend logs: `npx convex logs`
- Verify message schema matches `getMessages` function
- Check browser console for JavaScript errors in backend

### UI freezing
- This indicates blocking on main thread
- Verify all `await` calls use async methods
- Check `CallDeferred` is used for all subscription callbacks

### Memory leaks
- Ensure `_messagesSubscription?.Dispose()` called in `_ExitTree()`
- Unsubscribe from `ConvexManager.Instance.ConnectionStateChanged`
- Check no circular references in event subscriptions

## Performance Tips

1. **Pagination**: For large message histories, load in pages rather than all at once
2. **Subscription filtering**: Only subscribe to data you need (use function arguments)
3. **UI rendering**: VBoxContainer auto-layouts; for 1000+ messages, use custom container
4. **Message limit**: Keep `limit` parameter reasonable (50-100 recommended)

## Testing Multi-Client

To test synchronization:

1. **Run two Godot instances**:
   ```bash
   godot --instance-uuid abc123
   godot --instance-uuid def456
   ```

2. **Select different usernames** in each instance

3. **Send messages** from one and verify they appear in the other in real-time

4. **Test reconnection**: Pause/resume one client and verify recovery

## API Reference

### IConvexClient

```csharp
// Query (read-only)
Task<T> QueryAsync<T>(string functionName);
Task<T> QueryAsync<T, TArgs>(string functionName, TArgs args);

// Mutation (write)
Task<T> MutateAsync<T>(string functionName);
Task<T> MutateAsync<T, TArgs>(string functionName, TArgs args);

// Subscription (real-time)
IObservable<T> Observe<T>(string functionName);
IObservable<T> Observe<T, TArgs>(string functionName, TArgs args);

// Connection
ConnectionState ConnectionState { get; }
IObservable<ConnectionState> ConnectionStateChanges { get; }

// Authentication
IConvexAuthentication Auth { get; }
```

### ConnectionState Enum

```csharp
public enum ConnectionState {
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
```

## Project Structure & Vertical Slice Architecture

This example follows the Convex .NET client's **vertical slice architecture**:

- **ConvexManager**: Infrastructure layer (client lifecycle)
- **ChatScene**: Feature slice (chat functionality)
- **Models**: Shared data transfer objects
- **Backend**: Convex functions organized by capability

All slices depend on shared infrastructure (Models) but never directly on each other.

## Next Steps

After understanding this basic chat:

1. **Add persistence**: Save user preference for selected username
2. **Add file uploads**: Use Convex file storage for attachments
3. **Add presence**: Track online users with `updatePresence` function
4. **Add typing indicators**: Show who's currently typing
5. **Add reactions**: Add emoji reactions to messages
6. **Add threads**: Implement message replies with parent references
7. **Multi-room chat**: Add room/channel support
8. **User authentication**: Integrate OAuth or JWT auth

## Resources

- **Godot .NET Docs**: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/
- **Convex Docs**: https://docs.convex.dev
- **Convex .NET Client**: https://github.com/get-convex/convex-dotnet
- **System.Reactive (Rx.NET)**: https://reactive.how/

## License

This example is part of the Convex .NET client repository.

## Support

For issues:
- **Convex backend**: Check logs with `npx convex logs`
- **Godot/C#**: Check Godot debugger output
- **Network**: Check browser DevTools network tab in connected Godot editor

---

**Built with** ❤️ using Godot 4.x and Convex .NET Client
