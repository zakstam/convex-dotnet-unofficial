# Shared Configuration

This directory will contain shared configuration infrastructure used across all slices.

## Migration Plan

The following existing configuration classes should be moved here during the migration:

### From Root
- ✅ `ConvexClientOptions` - Client configuration and options
- ✅ `ConvexClientBuilder` - Fluent builder for client configuration

## Target Structure

```
Shared/Configuration/
├── ConvexClientOptions.cs
├── IConvexOptions.cs
└── README.md
```

## Current Configuration

Currently, configuration lives at the root level of the project:
- `ConvexClientOptions.cs` - Contains deployment URL, timeout, retry policy, etc.
- `ConvexClientBuilder.cs` - Fluent API for building client configuration

## Migration Status

**Status:** ⏳ Pending

Configuration classes will remain at the root level for now, but slices should prepare to consume them via dependency injection in the future.

## Usage in Slices

Slices will receive configuration through constructor injection:

```csharp
public class QueriesSlice
{
    private readonly IHttpClientProvider _httpProvider;
    private readonly ConvexClientOptions _options;

    public QueriesSlice(
        IHttpClientProvider httpProvider,
        ConvexClientOptions options)
    {
        _httpProvider = httpProvider;
        _options = options;
    }

    public async Task<TResult> QueryAsync<TResult>(string functionName)
    {
        // Use _options.DefaultTimeout, etc.
        var timeout = _options.DefaultTimeout;
        // ...
    }
}
```

## Configuration Options

### Deployment Configuration
- `DeploymentUrl` - The Convex backend URL
- `AdminAuth` - Admin authentication key (optional)

### Timeout Configuration
- `DefaultTimeout` - Default timeout for all operations
- `QueryTimeout` - Specific timeout for queries
- `MutationTimeout` - Specific timeout for mutations
- `ActionTimeout` - Specific timeout for actions

### Retry Configuration
- `RetryPolicy` - Custom retry policy for transient failures
- `MaxRetries` - Maximum number of retry attempts
- `RetryDelay` - Delay between retry attempts

### Caching Configuration
- `CacheEnabled` - Enable/disable query result caching
- `CacheMaxSize` - Maximum cache size
- `CacheTTL` - Time-to-live for cache entries

### WebSocket Configuration
- `WebSocketReconnectDelay` - Delay before reconnecting
- `WebSocketMaxReconnectAttempts` - Maximum reconnection attempts

## References

- **Current Options:** [ConvexClientOptions.cs](../../ConvexClientOptions.cs)
- **Current Builder:** [ConvexClientBuilder.cs](../../ConvexClientBuilder.cs)
