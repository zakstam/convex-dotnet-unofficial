# Convex.Client

Core client library for the Convex .NET SDK. This package provides everything you need to connect to Convex from .NET applications.

## Installation

```bash
dotnet add package Convex.Client
```

## What's Included

This package contains all Convex feature implementations organized by business capability:

### Core Operations

- **Queries** - Read-only data fetching
- **Mutations** - Data modification
- **Actions** - Server-side operations

### Authentication

- JWT token management
- Token providers for automatic refresh
- Auth state tracking

### File Storage

- File upload/download
- Storage ID management
- URL generation

### Vector Search

- Semantic search capabilities
- AI-powered similarity matching
- Embedding support

### HTTP Actions

- Action execution
- Retry policies
- Error handling

### Scheduling

- Delayed function execution
- Scheduled operations

### Real-time Communication

- WebSocket connections
- Live query subscriptions
- Real-time updates

## Architecture

Each feature is organized as a vertical slice containing:

- **Contracts** - Public interfaces and types
- **Implementation** - Concrete implementations
- **Supporting code** - Feature-specific utilities

This architecture provides:

- ✅ Clear separation of concerns
- ✅ Easy to test and maintain
- ✅ Minimal coupling between features
- ✅ Self-contained feature logic

## Learn More

- [Main Package](https://www.nuget.org/packages/Convex.Client/)
- [Documentation](https://github.com/zakstam/convex-dotnet-unofficial)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
