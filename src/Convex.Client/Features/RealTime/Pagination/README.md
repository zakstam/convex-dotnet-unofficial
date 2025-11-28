# Pagination Slice

## Purpose
Provides cursor-based pagination for Convex queries. Enables loading large datasets in manageable pages with automatic cursor management, real-time subscription merging, and convention-based configuration.

## Responsibilities
- Cursor-based pagination management
- Page loading with configurable page sizes
- State management (loaded items, cursors, page count)
- Async enumerable support for automatic page loading
- Thread-safe pagination state
- Custom argument merging with pagination options
- Convention-based ID and sort key extraction
- Real-time subscription merging with deduplication

## Public API Surface

### Convention Interfaces
```csharp
// Implement for automatic ID extraction (eliminates WithIdExtractor())
public interface IHasId
{
    string Id { get; }
}

// Implement for automatic sort key extraction (eliminates WithSortKey())
public interface IHasSortKey
{
    IComparable SortKey { get; }
}
```

### Extension Methods (Recommended Entry Points)
```csharp
// Simple paginated query with conventions
PaginatedQueryHelperBuilder<T> Paginate<T>(string functionName, int pageSize = 25);

// With typed arguments
PaginatedQueryHelperBuilder<T> Paginate<T, TArgs>(string functionName, TArgs args, int pageSize = 25);

// One-liner initialization
Task<PaginatedQueryHelper<T>> PaginateAsync<T>(string functionName, int pageSize = 25, ...);

// One-liner with arguments
Task<PaginatedQueryHelper<T>> PaginateAsync<T, TArgs>(string functionName, TArgs args, int pageSize = 25, ...);
```

### Low-Level Interfaces
```csharp
public interface IConvexPagination
{
    IPaginationBuilder<T> Query<T>(string functionName);
}

public interface IPaginationBuilder<T>
{
    IPaginationBuilder<T> WithPageSize(int pageSize);
    IPaginationBuilder<T> WithArgs<TArgs>(TArgs args);
    IPaginationBuilder<T> WithArgs<TArgs>(Action<TArgs> configure);
    IPaginator<T> Build();
}

public interface IPaginator<T>
{
    bool HasMore { get; }
    int LoadedPageCount { get; }
    IReadOnlyList<T> LoadedItems { get; }
    IReadOnlyList<int> PageBoundaries { get; }
    event Action<int>? PageBoundaryAdded;

    Task<IReadOnlyList<T>> LoadNextAsync(...);
    void Reset();
    IAsyncEnumerable<T> AsAsyncEnumerable(...);
    MergedPaginationResult<T> MergeWithSubscription(...);
}
```

### High-Level Helper
```csharp
public class PaginatedQueryHelper<T> : IDisposable
{
    IReadOnlyList<T> CurrentItems { get; }
    IReadOnlyList<int> PageBoundaries { get; }
    bool HasMore { get; }

    event Action<IReadOnlyList<T>, IReadOnlyList<int>>? ItemsUpdated;
    event Action<int>? PageBoundaryAdded;
    event Action<string>? SubscriptionStatusChanged;
    event Action<string>? ErrorOccurred;

    Task InitializeAsync(bool enableSubscription = true, ...);
    Task<IReadOnlyList<T>> LoadNextAsync(...);
    void MergeSubscriptionItems(IEnumerable<T> items);
    void Reset();
}
```

### Pagination Types
```csharp
public class PaginationOptions
{
    public int NumItems { get; set; }
    public string? Cursor { get; set; }
    public string? EndCursor { get; set; }
    public int? MaximumRowsRead { get; set; }
    public long? MaximumBytesRead { get; set; }
}

public class PaginationResult<T>
{
    public List<T> Page { get; set; }
    public bool IsDone { get; set; }
    public string ContinueCursor { get; set; }
    public string? SplitCursor { get; set; }
    public PageStatus? PageStatus { get; set; }
}

public enum PageStatus
{
    Normal,
    SplitRecommended,
    SplitRequired
}
```

## Usage Examples

### Simplest Usage (Convention-Based DTOs)
```csharp
// Define DTO with convention interfaces
public class MessageDto : IHasId, IHasSortKey
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonIgnore]
    public IComparable SortKey => Timestamp;
}

// One-liner pagination
var paginator = await client.PaginateAsync<MessageDto>("messages:list");

// Access items
foreach (var msg in paginator.CurrentItems)
{
    Console.WriteLine(msg.Text);
}

// Load more if available
if (paginator.HasMore)
{
    await paginator.LoadNextAsync();
}
```

### With Builder Pattern
```csharp
var paginator = await client.Paginate<MessageDto>("messages:list", pageSize: 25)
    .WithArgs(new { channel = "general" })
    .WithUIThreadMarshalling()
    .OnItemsUpdated((items, boundaries) => UpdateUI(items))
    .OnError(error => ShowError(error))
    .InitializeAsync();
```

### With Subscription Wrapper Type
```csharp
// When subscription returns a wrapper type instead of items directly
var paginator = await client.Paginate<MessageDto>("messages:list")
    .WithArgs(new GetMessagesArgs { Limit = 50 })
    .WithSubscriptionExtractor<GetMessagesResponse>(r => r.Messages ?? [])
    .WithUIThreadMarshalling()
    .OnItemsUpdated((items, _) => Messages = items.ToList())
    .InitializeAsync(enableSubscription: true);
```

### Custom ID/Sort Key (When Not Following Conventions)
```csharp
var paginator = await client.Paginate<CustomDto>("items:list")
    .WithIdExtractor(item => item.UniqueKey)
    .WithSortKey(item => item.CreatedDate)
    .InitializeAsync();
```

### Low-Level Pagination (Without Helper)
```csharp
// Create a paginator directly
var paginator = client.PaginationSlice
    .Query<Message>("messages:list")
    .WithPageSize(20)
    .Build();

// Load first page
var firstPage = await paginator.LoadNextAsync();

// Load more pages
while (paginator.HasMore)
{
    var nextPage = await paginator.LoadNextAsync();
}
```

### Async Enumerable (Auto-Loading)
```csharp
var paginator = client.PaginationSlice
    .Query<Article>("articles:list")
    .WithPageSize(25)
    .Build();

// Automatically loads pages as needed
await foreach (var article in paginator.AsAsyncEnumerable())
{
    Console.WriteLine(article.Title);
}
```

## Convention-Based Extraction

The pagination system automatically extracts IDs and sort keys using conventions:

### ID Extraction Priority
1. `IHasId.Id` - If type implements `IHasId`
2. `Id` property - Public string property named "Id"
3. `_id` property - Public string property named "_id"
4. `id` property - Public string property named "id"

### Sort Key Extraction Priority
1. `IHasSortKey.SortKey` - If type implements `IHasSortKey`
2. `Timestamp` property - Public `IComparable` property named "Timestamp"
3. `CreatedAt` property - Public `IComparable` property named "CreatedAt"
4. `timestamp` property - Public `IComparable` property named "timestamp"
5. `createdAt` property - Public `IComparable` property named "createdAt"

## Architecture
- **PaginationSlice**: Public facade implementing IConvexPagination
- **PaginationBuilder**: Fluent builder for creating paginators
- **Paginator**: Implementation with direct HTTP calls and state management
- **PaginatedQueryHelper**: High-level helper with subscription integration
- **PaginatedQueryHelperBuilder**: Fluent builder for the helper
- **PaginationConventions**: Static helper for convention-based extraction
- **Uses**: `/api/query` endpoint with pagination options

## Implementation Details
- Uses direct HTTP POST to `/api/query` endpoint
- Merges user arguments with pagination options automatically
- Maintains thread-safe state with lock-based synchronization
- Cursor is managed automatically between page loads
- Convention-based extractors are cached for performance
- Supports UI thread marshalling for WPF/WinForms/Blazor

## Error Handling
- Invalid page size → ArgumentOutOfRangeException
- No ID extractor found → InvalidOperationException with helpful message
- Query failure → ConvexPaginationException with function name
- Deserialization failure → ConvexPaginationException
- Network errors → ConvexPaginationException with inner exception

## Thread Safety
All pagination state is protected by locks:
- HasMore, LoadedPageCount, LoadedItems are safe to read
- LoadNextAsync safely updates state
- Reset safely clears all state
- Multiple concurrent calls to LoadNextAsync are serialized

## Limitations
- No support for split cursors (splitCursor in response)
- No automatic page size adjustment based on PageStatus

## Owner
TBD
