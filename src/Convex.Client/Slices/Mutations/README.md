# Mutations Slice

**Owner:** TBD
**Status:** ✅ Migrated (Second vertical slice!)

## Purpose

Provides write operations for Convex functions with fluent builder API. Handles mutation execution, optimistic updates, retry logic, error handling, and cache invalidation for write operations.

## Responsibilities

- Execute write operations on Convex functions
- Provide fluent mutation builder API
- Handle mutation retries and timeouts
- Support optimistic updates with automatic rollback
- Integrate with caching layer for invalidation (via facade)
- Support batch mutation execution

## Public API

### Entry Point

```csharp
public class MutationsSlice
{
    // Create a mutation builder
    public IMutationBuilder<TResult> Mutate<TResult>(string functionName)
}
```

### Mutation Builder

```csharp
public interface IMutationBuilder<TResult>
{
    // Configuration
    IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args)
    IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure)
    IMutationBuilder<TResult> WithTimeout(TimeSpan timeout)
    IMutationBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    IMutationBuilder<TResult> WithRetry(RetryPolicy policy)
    IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess)
    IMutationBuilder<TResult> OnError(Action<Exception> onError)

    // Optimistic updates
    IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate)
    IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(...)

    // Execution
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
}
```

## Dependencies

### Shared Infrastructure

- ⚠️ `CoreOperations/Implementation/ConvexHttpClient` - For HTTP communication (needs refactoring to use Shared)
- ⚠️ `Caching/IQueryCache` - For cache integration (needs refactoring to use Shared)
- ✅ `Shared/ErrorHandling/ConvexException` - For error handling
- ✅ `Shared/ErrorHandling/ConvexResult<T>` - For result types
- ✅ `Shared/Resilience/RetryPolicy` - For retry policies

### External Dependencies

- `System.Reactive` - For observable patterns in optimistic updates

## Cross-Slice Coordination

### With Caching Slice (via ConvexClient facade)

```csharp
// ConvexClient coordinates cache invalidation
await _mutations.Mutate<Message>("messages/create")
    .WithArgs(new { text = "Hello" })
    .ExecuteAsync();

// Facade invalidates dependent queries
await _cache.InvalidateAsync("messages/list");
```

### With Queries Slice (via Optimistic Updates)

```csharp
// Optimistic update affects cached query results
await _mutations.Mutate<Message>("messages/create")
    .WithArgs(new { text = "Hello" })
    .Optimistic(result => UpdateUIImmediately(result))
    .ExecuteAsync();
```

## Testing

### Unit Tests

**Location:** `tests/Slices/MutationsSliceTests.cs`

```csharp
[Test]
public async Task Mutate_WithValidFunction_ReturnsResult()
{
    // Arrange
    var mockHttp = new MockConvexHttpClient();
    var slice = new MutationsSlice(mockHttp);

    // Act
    var result = await slice.Mutate<Message>("messages/create")
        .WithArgs(new { text = "Test" })
        .ExecuteAsync();

    // Assert
    Assert.IsNotNull(result);
}
```

### Integration Tests

Test with real Convex backend to validate:
- Mutation execution
- Optimistic updates and rollback
- Error handling
- Retry logic
- Cache invalidation
- Timeout handling

## Migration Notes

### Changes from Original

1. **Namespace:** `Convex.Client.Mutations` → `Convex.Client.Slices.Mutations`
2. **Entry Point:** Added `MutationsSlice` class as facade
3. **Constructor:** Now internal, instantiated only by ConvexClient
4. **Dependencies:** Still using CoreOperations (needs refactoring to Shared)

### Known Issues

⚠️ **Architecture Violation:** Currently depends on `ConvexHttpClient` (CoreOperations) and `IQueryCache` (Caching) instead of Shared infrastructure. This needs to be refactored to follow the pattern established by QueriesSlice:
- Replace `ConvexHttpClient` with `IHttpClientProvider`
- Replace `IQueryCache` with a Shared caching abstraction

### Breaking Changes

**None** - Public API remains identical. All changes are internal.

### Migration Checklist

- [x] Create `Slices/Mutations/` folder
- [x] Copy interface files (IMutationBuilder, IBatchMutationBuilder)
- [x] Copy implementation files (MutationBuilder, OptimisticCollection)
- [x] Create MutationsSlice entry point
- [x] Update namespaces
- [x] Make constructor internal
- [ ] Refactor to use Shared infrastructure (ConvexHttpClient → IHttpClientProvider)
- [ ] Write unit tests
- [ ] Update ConvexClient to use MutationsSlice
- [ ] Run architecture tests (verify no violations)
- [ ] Delete old Mutations/ folder

## Future Enhancements

- [ ] Refactor to use Shared infrastructure instead of CoreOperations
- [ ] Implement `.IncludeMetadata()` for mutation execution metadata
- [ ] Implement transaction support for multi-mutation atomicity
- [ ] Add mutation batching optimization
- [ ] Improve optimistic update API with typed state management
- [ ] Add conflict resolution strategies for optimistic updates

## References

- **Architecture Rules:** [CLAUDE.md](../../../../CLAUDE.md)
- **Shared Infrastructure:** [Shared/README.md](../../Shared/README.md)
- **Architecture Tests:** `tests/Convex.Client.ArchitectureTests/`
- **Queries Slice Example:** [Slices/Queries/README.md](../Queries/README.md)
