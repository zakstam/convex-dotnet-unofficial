# Shared Error Handling

This directory will contain shared error handling infrastructure used across all slices.

## Migration Plan

The following existing error handling classes should be moved here during the migration:

### From `Common/ConvexExceptions.cs`
- ✅ `ConvexException` - Base exception for all Convex-related errors
- ✅ `ConvexFunctionException` - Function execution failures
- ✅ `ConvexArgumentException` - Invalid function arguments
- ✅ `ConvexNetworkException` - Network-related errors
- ✅ `ConvexAuthenticationException` - Authentication failures
- ✅ `ConvexRateLimitException` - Rate limit exceeded
- ✅ `ConvexCircuitBreakerException` - Circuit breaker open
- ✅ `NetworkErrorType` enum
- ✅ `CircuitBreakerState` enum
- ✅ `RequestContext` class

### From `ErrorHandling/`
- ✅ `ConvexError` - Error representation with pattern matching
- ✅ `ConvexFunctionError` - Convex function error
- ✅ `NetworkError` - Network-related error
- ✅ `TimeoutError` - Timeout error
- ✅ `CancellationError` - Cancellation error
- ✅ `UnexpectedError` - Unexpected error
- ✅ `ConvexResult<T>` - Result type for error handling

## Target Structure

```
Shared/ErrorHandling/
├── Exceptions/
│   ├── ConvexException.cs
│   ├── ConvexFunctionException.cs
│   ├── ConvexArgumentException.cs
│   ├── ConvexNetworkException.cs
│   ├── ConvexAuthenticationException.cs
│   ├── ConvexRateLimitException.cs
│   └── ConvexCircuitBreakerException.cs
├── Results/
│   ├── ConvexResult.cs
│   └── ConvexError.cs
├── Enums/
│   ├── NetworkErrorType.cs
│   └── CircuitBreakerState.cs
└── RequestContext.cs
```

## Migration Status

**Status:** ⏳ Pending

The classes listed above are currently in `Common/` and `ErrorHandling/` folders. They will be moved to `Shared/ErrorHandling/` during Phase 1 of the vertical slice migration.

## Usage in Slices

Slices should use these error types for consistent error handling:

```csharp
using Convex.Client.Shared.ErrorHandling;

// In slice code
public async Task<ConvexResult<TResult>> ExecuteAsync()
{
    try
    {
        var result = await _httpProvider.SendAsync(request, ct);
        return ConvexResult<TResult>.Success(result);
    }
    catch (ConvexException ex)
    {
        return ConvexResult<TResult>.Failure(ConvexError.FromException(ex));
    }
}
```

## References

- **Current Exceptions:** [Common/ConvexExceptions.cs](../../Common/ConvexExceptions.cs)
- **Current Error Types:** [ErrorHandling/ConvexError.cs](../../ErrorHandling/ConvexError.cs)
- **Current Result Type:** [ErrorHandling/ConvexResult.cs](../../ErrorHandling/ConvexResult.cs)
