# API Reference

Complete API reference for the Convex .NET SDK.

## ConvexClient

The main client class for interacting with Convex.

### Construction

```csharp
// Simple constructor
var client = new ConvexClient(string deploymentUrl);

// Constructor with options
var client = new ConvexClient(string deploymentUrl, ConvexClientOptions options);

// Builder pattern for advanced configuration
var client = new ConvexClientBuilder()
    .UseDeployment(string deploymentUrl)
    .WithAutoReconnect(int maxAttempts = 5, int delayMs = 1000)
    .WithTimeout(TimeSpan timeout)
    .WithHttpClient(HttpClient httpClient)
    .WithReconnectionPolicy(ReconnectionPolicy policy)
    .WithSyncContext(SynchronizationContext synchronizationContext)
    .WithLogging(ILogger logger)
    .EnableDebugLogging(bool enabled = true)
    .PreConnect()
    .UseMiddleware(IConvexMiddleware middleware)
    .UseMiddleware<TMiddleware>()
    .Use(Func<ConvexRequest, ConvexRequestDelegate, Task<ConvexResponse>> middleware)
    .WithSchemaValidation(Action<SchemaValidationOptions> configure)
    .WithStrictSchemaValidation()
    .WithRequestLogging(bool enabled = true)
    .WithDevelopmentDefaults()
    .WithProductionDefaults()
    .Build();

// Async builder
var client = await new ConvexClientBuilder()
    .UseDeployment(deploymentUrl)
    .BuildAsync(CancellationToken cancellationToken = default);
```

### Properties

```csharp
// Deployment URL
string DeploymentUrl { get; }

// Default timeout for HTTP operations
TimeSpan Timeout { get; set; }

// Current WebSocket connection state
ConnectionState ConnectionState { get; }

// PreConnect error if connection failed during initialization
Exception? PreConnectError { get; }
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
// Create a query builder
IQueryBuilder<TResult> Query<TResult>(string functionName);

// Query builder methods
IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();
IQueryBuilder<TResult> WithTimeout(TimeSpan timeout);
IQueryBuilder<TResult> IncludeMetadata();
IQueryBuilder<TResult> Cached(TimeSpan cacheDuration);
IQueryBuilder<TResult> OnError(Action<Exception> onError);
IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);
IQueryBuilder<TResult> WithRetry(RetryPolicy policy);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);

// Experimental: Consistent reads
[Obsolete("Experimental API")]
IQueryBuilder<TResult> UseConsistency(long timestamp);
```

### Mutations

```csharp
// Create a mutation builder
IMutationBuilder<TResult> Mutate<TResult>(string functionName);

// Mutation builder methods
IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();
IMutationBuilder<TResult> WithTimeout(TimeSpan timeout);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);

// Optimistic updates
IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate);
IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply);
IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(
    Func<TState> getter,
    Action<TState> setter,
    Func<TState, TState> update);
IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<IOptimisticLocalStore, TArgs> updateFn) where TArgs : notnull;
IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn);
IMutationBuilder<TResult> WithRollback(Action rollback);
IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception;

// Callbacks
IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess);
IMutationBuilder<TResult> OnError(Action<Exception> onError);
IMutationBuilder<TResult> WithCleanup(Action cleanup);

// Queue and retry
IMutationBuilder<TResult> SkipQueue();
IMutationBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);
IMutationBuilder<TResult> WithRetry(RetryPolicy policy);

// Pending mutation tracking
IMutationBuilder<TResult> TrackPending(ISet<string> tracker, string key);
```

### Actions

```csharp
// Create an action builder
IActionBuilder<TResult> Action<TResult>(string functionName);

// Action builder methods
IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;
IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();
IActionBuilder<TResult> WithTimeout(TimeSpan timeout);
IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess);
IActionBuilder<TResult> OnError(Action<Exception> onError);
IActionBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);
IActionBuilder<TResult> WithRetry(RetryPolicy policy);
Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);
```

### Batch Operations

```csharp
// Create a batch query builder
IBatchQueryBuilder Batch();

// Batch builder methods
IBatchQueryBuilder Query<T>(string functionName);
IBatchQueryBuilder Query<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

// Execute batch queries
Task<Dictionary<string, object>> ExecuteAsDictionaryAsync(CancellationToken cancellationToken = default);
Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default);

// Tuple overloads (2-8 results)
Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3, T4, T5, T6)> ExecuteAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3, T4, T5, T6, T7)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(CancellationToken cancellationToken = default);
Task<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7, T8>(CancellationToken cancellationToken = default);
```

### Pagination

```csharp
// Convention interfaces for automatic ID/sort key extraction
public interface IHasId { string Id { get; } }
public interface IHasSortKey { IComparable SortKey { get; } }

// Extension methods (recommended entry points)
PaginatedQueryHelperBuilder<T> Paginate<T>(string functionName, int pageSize = 25);
PaginatedQueryHelperBuilder<T> Paginate<T, TArgs>(string functionName, TArgs args, int pageSize = 25);
Task<PaginatedQueryHelper<T>> PaginateAsync<T>(string functionName, int pageSize = 25, ...);
Task<PaginatedQueryHelper<T>> PaginateAsync<T, TArgs>(string functionName, TArgs args, int pageSize = 25, ...);

// Builder methods
PaginatedQueryHelperBuilder<T> WithPageSize(int pageSize);
PaginatedQueryHelperBuilder<T> WithArgs<TArgs>(TArgs args);
PaginatedQueryHelperBuilder<T> WithIdExtractor(Func<T, string> getId);      // Optional with conventions
PaginatedQueryHelperBuilder<T> WithSortKey(Func<T, IComparable> getSortKey); // Optional with conventions
PaginatedQueryHelperBuilder<T> WithSubscriptionExtractor<TResponse>(Func<TResponse, IEnumerable<T>> extract);
PaginatedQueryHelperBuilder<T> WithUIThreadMarshalling();
PaginatedQueryHelperBuilder<T> OnItemsUpdated(Action<IReadOnlyList<T>, IReadOnlyList<int>> callback);
PaginatedQueryHelperBuilder<T> OnError(Action<string> callback);
Task<PaginatedQueryHelper<T>> InitializeAsync(bool enableSubscription = true, ...);

// Helper properties and methods
IReadOnlyList<T> CurrentItems { get; }
IReadOnlyList<int> PageBoundaries { get; }
bool HasMore { get; }
Task<IReadOnlyList<T>> LoadNextAsync(CancellationToken cancellationToken = default);
void Reset();

// Low-level access (optional)
IConvexPagination Pagination { get; }
IPaginationBuilder<T> Query<T>(string functionName);
IPaginator<T> Build();
IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken cancellationToken = default);
```

### Authentication

```csharp
// Access authentication service
IConvexAuthentication Auth { get; }

// Authentication methods (via IConvexAuthentication)
AuthenticationState AuthenticationState { get; }
string? CurrentAuthToken { get; }
event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;
Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default);
Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default);
Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default);
Task ClearAuthAsync(CancellationToken cancellationToken = default);
Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default);
Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default);

// Observable authentication state
IObservable<AuthenticationState> AuthenticationStateChanges { get; }
```

### File Storage

```csharp
// Access file storage service
IConvexFileStorage Files { get; }

// File storage methods (via IConvexFileStorage)
Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default);
Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default);
Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default);
Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default);
Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default);
Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default);
Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default);
```

### Vector Search

```csharp
// Access vector search service
IConvexVectorSearch VectorSearch { get; }

// Vector search methods (via IConvexVectorSearch)
Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default);
Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;
Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default);
Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;
Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);
Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);
Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default);
Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default);
```

### Scheduling

```csharp
// Access scheduling service
IConvexScheduler Scheduler { get; }

// Scheduling methods (via IConvexScheduler)
Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default);
Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;
Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default);
Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;
Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default);
Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull;
Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default);
Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull;
Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default);
Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default);
Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default);
```

### HTTP Actions

```csharp
// Access HTTP actions service
IConvexHttpActions Http { get; }

// HTTP action methods (via IConvexHttpActions)
Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;
Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;
Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;
Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;
Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull;
```

### Connection Monitoring

```csharp
// Observable streams
IObservable<ConnectionState> ConnectionStateChanges { get; }
IObservable<ConnectionQuality> ConnectionQualityChanges { get; }

// Connection management
Task EnsureConnectedAsync(CancellationToken cancellationToken = default);
Task<ConnectionQualityInfo> GetConnectionQualityAsync();
```

### Health Monitoring

```csharp
// Access health service
IConvexHealth Health { get; }

// Get health status
Task<ConvexHealthCheck> GetHealthAsync();
```

### Caching

```csharp
// Access caching service
IConvexCache Cache { get; }

// Cache invalidation
void DefineQueryDependency(string mutationName, params string[] invalidates);
Task InvalidateQueryAsync(string queryName);
Task InvalidateQueriesAsync(string pattern);
```

### Disposal

```csharp
void Dispose();
```

## Retry Policies

### RetryPolicy

```csharp
// Predefined policies
RetryPolicy.Default()       // 3 retries, exponential backoff
RetryPolicy.Aggressive()    // 5 retries, faster backoff
RetryPolicy.Conservative()  // 2 retries, longer delays
RetryPolicy.None()          // No retries

// Custom policy via builder
new RetryPolicyBuilder()
    .MaxRetries(int maxRetries)
    .ExponentialBackoff(TimeSpan initialDelay, double multiplier = 2.0, bool useJitter = true)
    .LinearBackoff(TimeSpan initialDelay)
    .ConstantBackoff(TimeSpan delay)
    .WithMaxDelay(TimeSpan maxDelay)
    .RetryOn<TException>()
    .OnRetry(Action<int, Exception, TimeSpan> callback)
    .Build();
```

### BackoffStrategy

```csharp
public enum BackoffStrategy
{
    Constant,
    Linear,
    Exponential
}
```

## Result Types

### ConvexResult<T>

Functional error handling type for operations that can fail.

```csharp
public sealed record ConvexResult<T>
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    T Value { get; }
    ConvexError Error { get; }

    static ConvexResult<T> Success(T value);
    static ConvexResult<T> Failure(ConvexError error);
    static ConvexResult<T> Failure(Exception exception);

    TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ConvexError, TResult> onFailure);
    void Match(Action<T> onSuccess, Action<ConvexError> onFailure);
    ConvexResult<T> OnSuccess(Action<T> action);
    ConvexResult<T> OnFailure(Action<ConvexError> action);
    ConvexResult<TNew> Map<TNew>(Func<T, TNew> mapper);
    ConvexResult<TNew> Bind<TNew>(Func<T, ConvexResult<TNew>> binder);
    T GetValueOrDefault(T defaultValue = default);
    T GetValueOrDefault(Func<ConvexError, T> defaultValueFactory);
}
```

### ConvexError

```csharp
public abstract class ConvexError
{
    Exception Exception { get; }
    string Message { get; }

    TResult Match<TResult>(
        Func<ConvexFunctionError, TResult> onConvexError,
        Func<NetworkError, TResult> onNetworkError,
        Func<TimeoutError, TResult> onTimeoutError,
        Func<CancellationError, TResult> onCancellationError,
        Func<UnexpectedError, TResult> onUnexpectedError);
}

// Error subtypes
public sealed class ConvexFunctionError : ConvexError { }
public sealed class NetworkError : ConvexError { }
public sealed class TimeoutError : ConvexError { }
public sealed class CancellationError : ConvexError { }
public sealed class UnexpectedError : ConvexError { }
```

## Attributes

### ConvexFunctionAttribute

Base attribute for Convex functions:

```csharp
[ConvexFunction("function:name", FunctionType.Query)]
public class MyFunction { }
```

### ConvexQueryAttribute

```csharp
[ConvexQuery("function:name")]
public class MyQuery { }
```

### ConvexMutationAttribute

```csharp
[ConvexMutation("function:name")]
public class MyMutation { }
```

### ConvexActionAttribute

```csharp
[ConvexAction("function:name")]
public class MyAction { }
```

### ConvexTableAttribute

```csharp
[ConvexTable]
public class MyTable { }

// With custom table name
[ConvexTable]
public class MyModel
{
    // TableName property can override class name
}
```

### ConvexIndexAttribute

```csharp
[ConvexIndex(IndexName = "myIndex", IsUnique = true)]
public string MyProperty { get; set; }
```

### ConvexSearchIndexAttribute

```csharp
[ConvexSearchIndex(IndexName = "searchIndex")]
public string SearchableText { get; set; }
```

### ConvexForeignKeyAttribute

```csharp
[ConvexForeignKey("users")]
public string UserId { get; set; }
```

### ConvexValidationAttribute

```csharp
[ConvexValidation(Min = 0, Max = 100, MinLength = 1, MaxLength = 255)]
public string MyProperty { get; set; }
```

### ConvexIgnoreAttribute

```csharp
[ConvexIgnore]
public string IgnoredProperty { get; set; }
```

## Enums

### ConnectionState

```csharp
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}
```

### ConnectionQuality

```csharp
public enum ConnectionQuality
{
    Unknown = 0,
    Excellent = 1,
    Good = 2,
    Fair = 3,
    Poor = 4,
    Terrible = 5
}
```

### AuthenticationState

```csharp
public enum AuthenticationState
{
    Unauthenticated,
    Authenticated,
    AuthenticationFailed,
    TokenExpired
}
```

### FunctionType

```csharp
public enum FunctionType
{
    Query,
    Mutation,
    Action
}
```

### ConvexJobStatus

```csharp
public enum ConvexJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Active,
    Paused
}
```

### ConvexScheduleType

```csharp
public enum ConvexScheduleType
{
    OneTime,
    Cron,
    Interval
}
```

## Types

### VectorSearchResult<T>

```csharp
public class VectorSearchResult<T>
{
    string Id { get; init; }
    float Score { get; init; }
    T Data { get; init; }
    float[]? Vector { get; init; }
    Dictionary<string, JsonElement>? Metadata { get; init; }
}
```

### VectorIndexInfo

```csharp
public class VectorIndexInfo
{
    string Name { get; init; }
    int Dimension { get; init; }
    VectorDistanceMetric Metric { get; init; }
    long VectorCount { get; init; }
    string Table { get; init; }
    string VectorField { get; init; }
    string? FilterField { get; init; }
    DateTimeOffset CreatedAt { get; init; }
    DateTimeOffset UpdatedAt { get; init; }
}
```

### ConvexHttpActionResponse<T>

```csharp
public class ConvexHttpActionResponse<T>
{
    HttpStatusCode StatusCode { get; init; }
    bool IsSuccess { get; }
    T? Body { get; init; }
    string? RawBody { get; init; }
    Dictionary<string, string> Headers { get; init; }
    string? ContentType { get; init; }
    double ResponseTimeMs { get; init; }
    ConvexHttpActionError? Error { get; init; }
}
```

### ConvexUploadUrlResponse

```csharp
public class ConvexUploadUrlResponse
{
    string UploadUrl { get; init; }
    string StorageId { get; init; }
}
```

### ConvexFileMetadata

```csharp
public class ConvexFileMetadata
{
    string StorageId { get; init; }
    string? Filename { get; init; }
    string? ContentType { get; init; }
    long Size { get; init; }
    DateTimeOffset UploadedAt { get; init; }
    string? Sha256 { get; init; }
}
```

### ConvexScheduledJob

```csharp
public class ConvexScheduledJob
{
    string Id { get; init; }
    string FunctionName { get; init; }
    ConvexJobStatus Status { get; init; }
    JsonElement? Arguments { get; init; }
    ConvexScheduleConfig Schedule { get; init; }
    DateTimeOffset CreatedAt { get; init; }
    DateTimeOffset UpdatedAt { get; init; }
    DateTimeOffset? NextExecutionTime { get; init; }
    DateTimeOffset? LastExecutionTime { get; init; }
    int ExecutionCount { get; init; }
    ConvexJobError? LastError { get; init; }
    JsonElement? LastResult { get; init; }
}
```

### ConvexScheduleConfig

```csharp
public class ConvexScheduleConfig
{
    ConvexScheduleType Type { get; init; }
    DateTimeOffset? ScheduledTime { get; init; }
    string? CronExpression { get; init; }
    TimeSpan? Interval { get; init; }
    string? Timezone { get; init; }
    DateTimeOffset? StartTime { get; init; }
    DateTimeOffset? EndTime { get; init; }
    int? MaxExecutions { get; init; }

    static ConvexScheduleConfig OneTime(DateTimeOffset scheduledTime);
    static ConvexScheduleConfig Cron(string cronExpression, string timezone = "UTC");
    static ConvexScheduleConfig CreateInterval(TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
}
```

### PaginationResult<T>

```csharp
public class PaginationResult<T>
{
    List<T> Page { get; set; }
    bool IsDone { get; set; }
    string? ContinueCursor { get; set; }
    string? SplitCursor { get; set; }
    PageStatus? PageStatus { get; set; }
}
```

### ConnectionQualityInfo

```csharp
public sealed class ConnectionQualityInfo
{
    ConnectionQuality Quality { get; }
    string Description { get; }
    DateTimeOffset Timestamp { get; }
    double? AverageLatencyMs { get; }
    double? LatencyVarianceMs { get; }
    double? PacketLossRate { get; }
    int ReconnectionCount { get; }
    int ErrorCount { get; }
    TimeSpan? TimeSinceLastMessage { get; }
    double UptimePercentage { get; }
    int QualityScore { get; }
}
```

## Exceptions

### ConvexException

Base exception for all Convex-related errors:

```csharp
public class ConvexException : Exception
{
    string? ErrorCode { get; set; }
    JsonElement? ErrorData { get; set; }
    RequestContext? RequestContext { get; set; }
    ConvexErrorDetails? ErrorDetails { get; set; }
    string GetDetailedMessage();
}
```

### Specialized Exceptions

```csharp
public class ConvexFunctionException : ConvexException
{
    string FunctionName { get; }
}

public class ConvexArgumentException : ConvexException
{
    string ArgumentName { get; }
}

public class ConvexNetworkException : ConvexException
{
    NetworkErrorType ErrorType { get; }
    HttpStatusCode? StatusCode { get; set; }
}

public class ConvexAuthenticationException : ConvexException { }

public class ConvexRateLimitException : ConvexException
{
    TimeSpan RetryAfter { get; }
    int CurrentLimit { get; }
}

public class ConvexCircuitBreakerException : ConvexException
{
    CircuitBreakerState CircuitState { get; }
}

public class ConvexFileStorageException : Exception
{
    FileStorageErrorType ErrorType { get; }
    string? StorageId { get; }
}

public class ConvexVectorSearchException : Exception
{
    VectorSearchErrorType ErrorType { get; }
    string? IndexName { get; }
}

public class ConvexSchedulingException : Exception
{
    SchedulingErrorType ErrorType { get; }
    string? JobId { get; }
}

public class ConvexHttpActionException : Exception
{
    HttpStatusCode? StatusCode { get; }
    string? ActionPath { get; }
    HttpActionErrorType ErrorType { get; }
}

public class ConvexPaginationException : Exception
{
    string? FunctionName { get; }
}
```

## Interfaces

### IAuthTokenProvider

```csharp
public interface IAuthTokenProvider
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
```

### IConvexMiddleware

```csharp
public interface IConvexMiddleware
{
    Task<ConvexResponse> InvokeAsync(ConvexRequest request, ConvexRequestDelegate next);
}
```

## See Also

- [Getting Started Guide](getting-started.md)
- [Troubleshooting Guide](troubleshooting.md)
- [Transpiler Limitations](transpiler-limitations.md)
