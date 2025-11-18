# API Reference

Complete API reference for the Convex .NET SDK.

## ConvexClient

The main client class for interacting with Convex.

### Construction

```csharp
// Simple constructor
var client = new ConvexClient(string deploymentUrl);

// Builder pattern for advanced configuration
var client = new ConvexClientBuilder()
    .UseDeployment(string deploymentUrl)
    .WithAutoReconnect(int maxAttempts = 5, int delayMs = 1000)
    .WithTimeout(TimeSpan timeout)
    .Build();
```

### Real-Time Subscriptions

```csharp
// Subscribe to live data updates
IObservable<T> Observe<T>(string functionName);
IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

// Get cached value from active subscription
T? GetCachedValue<T>(string functionName);
bool TryGetCachedValue<T>(string functionName, out T? value);
```

### Queries

```csharp
// Execute a query
IQueryBuilder<TResult> Query<TResult>(string functionName);

// Query builder methods
IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IQueryBuilder<TResult> WithTimeout(TimeSpan timeout);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
```

### Mutations

```csharp
// Execute a mutation
IMutationBuilder<TResult> Mutate<TResult>(string functionName);

// Mutation builder methods
IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IMutationBuilder<TResult> WithTimeout(TimeSpan timeout);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
```

### Actions

```csharp
// Execute an action
IActionBuilder<TResult> Action<TResult>(string functionName);

// Action builder methods
IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IActionBuilder<TResult> WithTimeout(TimeSpan timeout);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
```

### Batch Operations

```csharp
// Execute multiple queries in parallel
IBatchQueryBuilder Batch();

// Batch builder methods
IBatchQueryBuilder Query<TResult>(string functionName);
IBatchQueryBuilder Query<TResult, TArgs>(string functionName, TArgs args) where TArgs : notnull;
Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>();
```

### Authentication

```csharp
// Set authentication token
Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default);

// Set token provider for automatic refresh
Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default);

// Set admin authentication (server-side only)
Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default);
```

### File Storage

```csharp
// Upload a file
Task<string> UploadFileAsync(
    Stream fileContent,
    string contentType,
    string? filename = null,
    CancellationToken cancellationToken = default);

// Get download URL
Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default);

// Download file
Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default);
```

### Vector Search

```csharp
// Search using vector embeddings
Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(
    string indexName,
    float[] vector,
    int limit = 10,
    CancellationToken cancellationToken = default);
```

### Scheduling

```csharp
// Schedule one-time execution
Task<string> ScheduleAsync(
    string functionName,
    TimeSpan delay,
    object? args = null,
    CancellationToken cancellationToken = default);

// Schedule recurring job (cron)
Task<string> ScheduleRecurringAsync(
    string functionName,
    string cronExpression,
    string timezone = "UTC",
    object? args = null,
    CancellationToken cancellationToken = default);

// Schedule interval job
Task<string> ScheduleIntervalAsync(
    string functionName,
    TimeSpan interval,
    object? args = null,
    CancellationToken cancellationToken = default);

// Cancel scheduled job
Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default);
```

### HTTP Actions

```csharp
// GET request
Task<ConvexHttpActionResponse<T>> GetAsync<T>(
    string actionPath,
    Dictionary<string, string>? queryParameters = null,
    Dictionary<string, string>? headers = null,
    CancellationToken cancellationToken = default);

// POST request
Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(
    string actionPath,
    TBody body,
    string contentType = "application/json",
    Dictionary<string, string>? headers = null,
    CancellationToken cancellationToken = default) where TBody : notnull;
```

### Connection Monitoring

```csharp
// Connection state changes
IObservable<ConnectionState> ConnectionStateChanges { get; }

// Connection quality changes
IObservable<ConnectionQuality> ConnectionQualityChanges { get; }
```

### Query Dependencies

```csharp
// Define cache invalidation rules
void DefineQueryDependency(string mutationName, params string[] invalidates);
```

### Disposal

```csharp
// Clean up resources
void Dispose();
```

## Extension Methods

### Reactive Extensions (Rx)

Located in `Convex.Client.Extensions.ExtensionMethods` namespace:

- `RetryWithBackoff<T>()` - Retry with exponential backoff
- `SmartDebounce<T>()` - Debounce preserving first and last values
- `ShareReplayLatest<T>()` - Share subscription with replay
- `WhenConnected<T>()` - Only emit when connected
- `BufferDuringPoorConnection<T>()` - Buffer during poor connection
- `RetryWhen<T>()` - Conditional retry
- `WithCircuitBreaker<T>()` - Circuit breaker pattern
- `TimeoutWithMessage<T>()` - Timeout with custom message
- `CatchAndReport<T>()` - Error reporting with context
- `DistinctUntilChangedBy<T>()` - Multi-key change detection
- `ThrottleToMaxFrequency<T>()` - Rate limiting
- `ThrottleSlidingWindow<T>()` - Sliding window rate limiting
- `BatchUpdates<T>()` - Intelligent batching
- `WithPerformanceLogging<T>()` - Performance monitoring

### UI Framework Integrations

**WPF/MAUI:**
- `ObserveOnUI<T>()` - Marshal to UI thread
- `ToObservableCollection<T>()` - Convert to ObservableCollection
- `BindToProperty<TSource, TTarget, TProp>()` - Reactive property binding
- `BindToCanExecute()` - Command enablement binding

**Blazor:**
- `SubscribeWithStateHasChanged<T>()` - Subscribe with StateHasChanged
- `ToAsyncEnumerable<T>()` - Convert to async enumerable
- `BindToForm<T>()` - Two-way form binding

### Common Patterns

- `CreateInfiniteScroll<T>()` - Infinite scroll helper
- `CreateDebouncedSearch<TResult>()` - Debounced search helper
- `CreateConnectionIndicator()` - Connection status indicator
- `CreateDetailedConnectionIndicator()` - Detailed connection status
- `CreateResilientSubscription<T>()` - Resilient subscription helper

### Testing

- `CreateMockClient()` - Create mock client for testing
- `Record<T>()` - Record observable emissions
- `WaitForValue<T>()` - Wait for specific values
- `SimulateIntermittentConnection()` - Simulate connection issues

## Attributes

### ConvexTableAttribute

Marks a class as a Convex table:

```csharp
[ConvexTable]
public class MyTable { }
```

### ConvexQueryAttribute

Marks a method as a Convex query function:

```csharp
[ConvexQuery("function:name")]
public static ReturnType MyQuery() { }
```

### ConvexMutationAttribute

Marks a method as a Convex mutation function:

```csharp
[ConvexMutation("function:name")]
public static ReturnType MyMutation() { }
```

### ConvexActionAttribute

Marks a method as a Convex action function:

```csharp
[ConvexAction("function:name")]
public static ReturnType MyAction() { }
```

### ConvexIndexAttribute

Marks a property as indexed:

```csharp
[ConvexIndex(IndexName = "myIndex", IsUnique = true)]
public string MyProperty { get; set; }
```

### ConvexSearchIndexAttribute

Marks a property as searchable:

```csharp
[ConvexSearchIndex]
public string SearchableText { get; set; }
```

## Types

### ConnectionState

```csharp
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
```

### ConnectionQuality

```csharp
public enum ConnectionQuality
{
    Excellent,
    Good,
    Poor,
    Offline
}
```

### VectorSearchResult<T>

```csharp
public class VectorSearchResult<T>
{
    public T Item { get; }
    public float Score { get; }
}
```

### ConvexHttpActionResponse<T>

```csharp
public class ConvexHttpActionResponse<T>
{
    public bool IsSuccess { get; }
    public T? Body { get; }
    public int StatusCode { get; }
    public Dictionary<string, string> Headers { get; }
}
```

## See Also

- [Getting Started Guide](getting-started.md)
- [Troubleshooting Guide](troubleshooting.md)
- [Transpiler Limitations](transpiler-limitations.md)

