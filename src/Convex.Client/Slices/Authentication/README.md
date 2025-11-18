# Authentication Slice

## Purpose
Provides authentication state management and token handling for Convex clients. Manages three authentication modes: static tokens, admin keys, and token providers. Includes event-driven state change notifications and thread-safe token management.

## Responsibilities
- Authentication state management (Unauthenticated, Authenticated, Failed, Expired)
- Static JWT token management
- Admin authentication key handling
- Token provider integration for dynamic tokens
- Authentication state change events
- Thread-safe authentication operations
- HTTP header generation with Bearer tokens

## Public API Surface

### Main Interface
```csharp
public interface IConvexAuthentication
{
    AuthenticationState AuthenticationState { get; }
    string? CurrentAuthToken { get; }

    event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default);
    Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default);
    Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default);
    Task ClearAuthAsync(CancellationToken cancellationToken = default);
    Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default);
}
```

### Authentication Types
```csharp
public enum AuthenticationState
{
    Unauthenticated,
    Authenticated,
    AuthenticationFailed,
    TokenExpired
}

public class AuthenticationStateChangedEventArgs : EventArgs
{
    public AuthenticationState State { get; }
    public string? ErrorMessage { get; }
}
```

## Shared Dependencies
- **IAuthTokenProvider**: For dynamic token retrieval (defined in Shared/Common)

## Architecture
- **AuthenticationSlice**: Public facade implementing IConvexAuthentication
- **AuthenticationManager**: Internal implementation with SemaphoreSlim-based locking
- **State Management**: Thread-safe with async lock for all operations
- **Event System**: Fires state change events when authentication state transitions

## Usage Examples

### Static Token Authentication
```csharp
// Set a static JWT token
await client.AuthenticationSlice.SetAuthTokenAsync("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");

// Check authentication state
if (client.AuthenticationSlice.AuthenticationState == AuthenticationState.Authenticated)
{
    Console.WriteLine("Authenticated successfully");
}

// Get current token
var token = client.AuthenticationSlice.CurrentAuthToken;
```

### Admin Authentication
```csharp
// Set admin key for backend operations
await client.AuthenticationSlice.SetAdminAuthAsync("admin-key-12345");

// Admin auth takes precedence over regular tokens
var adminToken = await client.AuthenticationSlice.GetAuthTokenAsync();
```

### Token Provider (Dynamic Tokens)
```csharp
// Implement token provider
public class MyTokenProvider : IAuthTokenProvider
{
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        // Fetch fresh token from your auth service
        return await _authService.GetCurrentTokenAsync();
    }
}

// Register token provider
var provider = new MyTokenProvider();
await client.AuthenticationSlice.SetAuthTokenProviderAsync(provider);

// Token will be fetched automatically when needed
var token = await client.AuthenticationSlice.GetAuthTokenAsync();
```

### Authentication State Events
```csharp
// Subscribe to state changes
client.AuthenticationSlice.AuthenticationStateChanged += (sender, args) =>
{
    Console.WriteLine($"Auth state changed to: {args.State}");
    if (args.ErrorMessage != null)
    {
        Console.WriteLine($"Error: {args.ErrorMessage}");
    }
};

// Set authentication (will trigger event)
await client.AuthenticationSlice.SetAuthTokenAsync("token-12345");
```

### Getting Authentication Headers
```csharp
// Get headers for custom HTTP requests
var headers = await client.AuthenticationSlice.GetAuthHeadersAsync();
// Returns: { "Authorization": "Bearer eyJhbGci..." }

foreach (var (key, value) in headers)
{
    httpRequest.Headers.Add(key, value);
}
```

### Clearing Authentication
```csharp
// Clear all authentication (logout)
await client.AuthenticationSlice.ClearAuthAsync();

// State will change to Unauthenticated
Console.WriteLine(client.AuthenticationSlice.AuthenticationState); // Unauthenticated
```

## Implementation Details
- Uses SemaphoreSlim for async-safe locking
- Supports three mutually exclusive authentication modes (token, admin, provider)
- Token provider is called on-demand if no cached token available
- State transitions trigger events only when state actually changes
- Thread-safe for concurrent authentication operations
- No HTTP calls - pure state management

## State Management
- **_authToken**: Static JWT token set by user
- **_adminAuth**: Admin authentication key
- **_authTokenProvider**: Dynamic token provider interface
- **_authenticationState**: Current authentication state
- All state changes are thread-safe using SemaphoreSlim

## Authentication Priority
When GetAuthTokenAsync is called, tokens are returned in this order:
1. Static token (_authToken)
2. Admin key (_adminAuth)
3. Token from provider (_authTokenProvider.GetTokenAsync)
4. null (unauthenticated)

## Error Handling
- Invalid token/admin key → ArgumentNullException
- Token provider fetch failure → Exception propagated + state set to AuthenticationFailed
- All operations are async and support cancellation tokens

## Thread Safety
All authentication operations are protected by `_authLock` (SemaphoreSlim):
- AuthenticationState, CurrentAuthToken are safe to read
- Set operations safely clear other auth modes
- GetAuthTokenAsync safely fetches from provider if needed
- Multiple concurrent calls are serialized

## Limitations
- No automatic token refresh (must be implemented in IAuthTokenProvider)
- No token expiration detection (except via provider)
- No token validation (assumes tokens are valid when set)
- Events fire on same thread as state change (no async event handlers)

## Owner
TBD
