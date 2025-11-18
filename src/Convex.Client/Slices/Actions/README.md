# Actions Slice

**Owner:** TBD
**Status:** ✅ Migrated (Third vertical slice!)

## Purpose

Provides HTTP action execution for Convex functions with fluent builder API. Handles action execution, retry logic, error handling, and callbacks for non-transactional operations.

## Responsibilities

- Execute HTTP actions on Convex functions
- Provide fluent action builder API
- Handle action retries and timeouts
- Support success and error callbacks
- Enable long-running operations and external API calls

## Public API

### Entry Point

```csharp
public class ActionsSlice
{
    // Create an action builder
    public IActionBuilder<TResult> Action<TResult>(string functionName)
}
```

### Action Builder

```csharp
public interface IActionBuilder<TResult>
{
    // Configuration
    IActionBuilder<TResult> WithArgs<TArgs>(TArgs args)
    IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure)
    IActionBuilder<TResult> WithTimeout(TimeSpan timeout)
    IActionBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    IActionBuilder<TResult> WithRetry(RetryPolicy policy)
    IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess)
    IActionBuilder<TResult> OnError(Action<Exception> onError)

    // Execution
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
}
```

## Dependencies

### Shared Infrastructure

- ⚠️ `CoreOperations/Implementation/ConvexHttpClient` - For HTTP communication (needs refactoring to use Shared)
- ✅ `Shared/ErrorHandling/ConvexException` - For error handling
- ✅ `Shared/ErrorHandling/ConvexResult<T>` - For result types
- ✅ `Shared/Resilience/RetryPolicy` - For retry policies

### External Dependencies

None

## Cross-Slice Coordination

### With Mutations Slice (via Actions calling Mutations)

```csharp
// Action might trigger mutations internally on the server
await _actions.Action<EmailResult>("emails/send")
    .WithArgs(new { to = "user@example.com", subject = "Hello" })
    .OnSuccess(result => LogEmailSent(result))
    .ExecuteAsync();

// Server-side action might call mutations
```

### With External APIs

```csharp
// Actions can call external APIs on the server
await _actions.Action<PaymentResult>("payments/process")
    .WithArgs(new { amount = 100, currency = "USD" })
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithRetry(retry => retry.MaxAttempts(3))
    .ExecuteAsync();
```

## Testing

### Unit Tests

**Location:** `tests/Slices/ActionsSliceTests.cs`

```csharp
[Test]
public async Task Action_WithValidFunction_ReturnsResult()
{
    // Arrange
    var mockHttp = new MockConvexHttpClient();
    var slice = new ActionsSlice(mockHttp);

    // Act
    var result = await slice.Action<EmailResult>("emails/send")
        .WithArgs(new { to = "test@example.com" })
        .ExecuteAsync();

    // Assert
    Assert.IsNotNull(result);
}
```

### Integration Tests

Test with real Convex backend to validate:
- Action execution
- Long-running operations
- External API calls
- Error handling
- Retry logic
- Timeout handling
- Success/error callbacks

## Migration Notes

### Changes from Original

1. **Namespace:** `Convex.Client.Actions` → `Convex.Client.Slices.Actions`
2. **Entry Point:** Added `ActionsSlice` class as facade
3. **Constructor:** Now internal, instantiated only by ConvexClient
4. **Dependencies:** Still using CoreOperations (needs refactoring to Shared)

### Known Issues

⚠️ **Architecture Violation:** Currently depends on `ConvexHttpClient` (CoreOperations) instead of Shared infrastructure. This needs to be refactored to follow the pattern established by QueriesSlice:
- Replace `ConvexHttpClient` with `IHttpClientProvider`
- Use shared serialization interfaces

### Breaking Changes

**None** - Public API remains identical. All changes are internal.

### Migration Checklist

- [x] Create `Slices/Actions/` folder
- [x] Copy interface files (IActionBuilder)
- [x] Copy implementation files (ActionBuilder)
- [x] Create ActionsSlice entry point
- [x] Update namespaces
- [x] Make constructor internal
- [ ] Refactor to use Shared infrastructure (ConvexHttpClient → IHttpClientProvider)
- [ ] Write unit tests
- [ ] Update ConvexClient to use ActionsSlice
- [ ] Run architecture tests (verify no violations)
- [ ] Delete old Actions/ folder

## Future Enhancements

- [ ] Refactor to use Shared infrastructure instead of CoreOperations
- [ ] Implement `.IncludeMetadata()` for action execution metadata
- [ ] Add progress reporting for long-running actions
- [ ] Add cancellation support for in-flight actions
- [ ] Add streaming support for large responses
- [ ] Implement action batching optimization
- [ ] Add webhook support for async actions

## References

- **Architecture Rules:** [CLAUDE.md](../../../../CLAUDE.md)
- **Shared Infrastructure:** [Shared/README.md](../../Shared/README.md)
- **Architecture Tests:** `tests/Convex.Client.ArchitectureTests/`
- **Queries Slice Example:** [Slices/Queries/README.md](../Queries/README.md)
- **Mutations Slice Example:** [Slices/Mutations/README.md](../Mutations/README.md)
