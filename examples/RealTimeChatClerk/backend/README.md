# Realtime Chat — Convex backend

This directory contains a minimal Convex TypeScript backend for the Realtime Chat example.

## Available Functions

### Queries

- `getMessages` - Retrieve paginated messages (most recent first)
  - Args: `{ paginationOpts?: any, limit?: number }`
  - Returns: `{ messages: Message[], isDone: boolean, continueCursor: string }`

- `getMessagesByUser` - Get messages by a specific user
  - Args: `{ username: string, limit?: number }`
  - Returns: `Message[]`

### Mutations

- `sendMessage` - Send a new chat message
  - Args: `{ username: string, text: string }`
  - Returns: `{ id: string, username: string, text: string, timestamp: number }`
  - Validation: Username and text required, max 50 chars username, max 1000 chars text

- `editMessage` - Edit an existing message
  - Args: `{ id: string, text: string }`
  - Returns: Updated message object

- `deleteMessage` - Delete a message
  - Args: `{ id: string }`
  - Returns: `{ success: true }`

## Schema

```typescript
type Message = {
  username: string;
  text: string;
  timestamp: number;
};
```

## Quick start

```powershell
cd examples/RealTimeChatClerk/backend
npm install
npx convex dev
```

## Notes

- These are scaffolding files intended to be refined. They contain placeholder APIs meant to be adapted to the project's Convex version and coding conventions.
- Schema includes an index on `username` for efficient user-specific queries.
- All mutations include basic validation and error handling.
