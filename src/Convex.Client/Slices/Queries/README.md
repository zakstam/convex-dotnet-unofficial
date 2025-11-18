# Queries Slice

**Owner:** TBD
**Status:** ✅ Migrated (First vertical slice!)

## Purpose

Provides read-only Convex function execution with fluent builder API. Handles query execution, retry logic, error handling, and caching for read operations.

## Responsibilities

- Execute read-only Convex functions
- Provide fluent query builder API
- Handle query retries and timeouts
- Support batch query execution
- Integrate with caching layer (via facade)

## Public API

### Entry Point

```csharp
public class QueriesSlice
{
    // Create a query builder
    public IQueryBuilder<TResult> Query<TResult>(string functionName)

    // Create a batch query builder
    public IBatchQueryBuilder Batch()
}
```

### Query Builder

```csharp
public interface IQueryBuilder<TResult>
{
    // Configuration
    IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args)
    IQueryBuilder<TResult> WithTimeout(TimeSpan timeout)
    IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    IQueryBuilder<TResult> OnError(Action<Exception> onError)

    // Execution
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
}
```

### Batch Query Builder

```csharp
public interface IBatchQueryBuilder
{
    IBatchQueryBuilder Add<TResult>(string functionName)
    IBatchQueryBuilder Add<TResult, TArgs>(string functionName, TArgs args)
    Task<Dictionary<string, object?>> ExecuteAsync(CancellationToken cancellationToken = default)
}
```

## Dependencies

### Shared Infrastructure

- ✅ `Shared/Http/IHttpClientProvider` - For HTTP communication
- ✅ `Shared/Serialization/IConvexSerializer` - For JSON serialization
- ✅ `Shared/ErrorHandling/ConvexException` - For error handling
- ✅ `Shared/ErrorHandling/ConvexResult<T>` - For result types

### External Dependencies

- `Convex.Client.Resilience` - For retry policies (will be moved to Shared)
- `Convex.Client.ErrorHandling` - Error types (will be moved to Shared)

## Cross-Slice Coordination

### With Caching Slice (via ConvexClient facade)

```csharp
// ConvexClient coordinates caching
var result = await _queries.Query<Message[]>("messages/list")
    .Cached(TimeSpan.FromMinutes(5))  // Signal to facade
    .ExecuteAsync();

// Facade checks cache before calling slice
if (_cache.TryGet(cacheKey, out var cached))
    return cached;

var result = await _queries.Query<Message[]>("messages/list").ExecuteAsync();
_cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
```

### With Middleware (via ConvexClient)

```csharp
// Middleware executor injected via constructor
var queriesSlice = new QueriesSlice(
    httpProvider,
    serializer,
    middlewareExecutor: ExecuteThroughMiddleware);
```

## Testing

### Unit Tests

**Location:** `tests/Slices/QueriesSliceTests.cs`

```csharp
[Test]
public async Task Query_WithValidFunction_ReturnsResult()
{
    // Arrange
    var mockHttp = new MockHttpClientProvider();
    var serializer = new DefaultConvexSerializer();
    var slice = new QueriesSlice(mockHttp, serializer);

    // Act
    var result = await slice.Query<string[]>("test/function")
        .ExecuteAsync();

    // Assert
    Assert.IsNotNull(result);
}
```

### Integration Tests

Test with real Convex backend to validate:
- Query execution
- Error handling
- Retry logic
- Timeout handling

## Migration Notes

### Changes from Original

1. **Dependency:** `ConvexHttpClient` → `IHttpClientProvider`
2. **Namespace:** `Convex.Client.Queries` → `Convex.Client.Slices.Queries`
3. **Entry Point:** Added `QueriesSlice` class as facade
4. **Isolation:** Removed direct dependencies on other features

### Breaking Changes

**None** - Public API remains identical. All changes are internal.

### Migration Checklist

- [x] Create `Slices/Queries/` folder
- [x] Copy interface files (IQueryBuilder, IBatchQueryBuilder)
- [x] Create QueriesSlice entry point
- [x] Update namespaces
- [x] Refactor to use Shared infrastructure
- [ ] Create QueryBuilder implementation (stub for now)
- [ ] Create BatchQueryBuilder implementation (stub for now)
- [ ] Write unit tests
- [ ] Update ConvexClient to use QueriesSlice
- [ ] Run architecture tests (verify no violations)
- [ ] Delete old Queries/ folder

## Future Enhancements

- [ ] Implement `.IncludeMetadata()` for query execution metadata
- [ ] Implement `.UseConsistency(timestamp)` for consistent reads
- [ ] Implement `.Cached(duration)` for automatic client-side caching
- [ ] Add query result streaming for large datasets
- [ ] Add query cancellation support
- [ ] Add query batching optimization

## References

- **Architecture Rules:** [CLAUDE.md](../../../../CLAUDE.md)
- **Shared Infrastructure:** [Shared/README.md](../../Shared/README.md)
- **Architecture Tests:** `tests/Convex.Client.ArchitectureTests/`
- **Original Implementation:** `Queries/QueryBuilder.cs` (to be deleted)
