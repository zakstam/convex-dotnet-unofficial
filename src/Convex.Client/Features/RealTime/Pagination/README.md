# Pagination Slice

## Purpose
Provides cursor-based pagination for Convex queries. Enables loading large datasets in manageable pages with automatic cursor management and support for async iteration.

## Responsibilities
- Cursor-based pagination management
- Page loading with configurable page sizes
- State management (loaded items, cursors, page count)
- Async enumerable support for automatic page loading
- Thread-safe pagination state
- Custom argument merging with pagination options

## Public API Surface

### Main Interface
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

    Task<IReadOnlyList<T>> LoadNextAsync(...);
    void Reset();
    IAsyncEnumerable<T> AsAsyncEnumerable(...);
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

### Exception Types
```csharp
public class ConvexPaginationException : Exception
{
    public string? FunctionName { get; }
}
```

## Shared Dependencies
- **IHttpClientProvider**: For HTTP request execution
- **IConvexSerializer**: For JSON serialization/deserialization

## Architecture
- **PaginationSlice**: Public facade implementing IConvexPagination
- **PaginationBuilder**: Fluent builder for creating paginators
- **Paginator**: Implementation with direct HTTP calls and state management
- **Uses**: `/api/query` endpoint with pagination options

## Usage Examples

### Basic Pagination
```csharp
// Create a paginator with default page size (20)
var paginator = client.PaginationSlice
    .Query<Message>("messages:list")
    .Build();

// Load first page
var firstPage = await paginator.LoadNextAsync();
Console.WriteLine($"Loaded {firstPage.Count} messages");

// Load next page
if (paginator.HasMore)
{
    var secondPage = await paginator.LoadNextAsync();
}

// Check total loaded
Console.WriteLine($"Total loaded: {paginator.LoadedItems.Count} items");
```

### Custom Page Size and Arguments
```csharp
// Paginator with custom page size and filter arguments
var paginator = client.PaginationSlice
    .Query<Product>("products:search")
    .WithPageSize(50)
    .WithArgs(new { category = "electronics", minPrice = 100 })
    .Build();

var page = await paginator.LoadNextAsync();
```

### Async Enumerable (Auto-Loading)
```csharp
// Automatically load all pages
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

### With Builder Pattern for Args
```csharp
var paginator = client.PaginationSlice
    .Query<User>("users:search")
    .WithPageSize(30)
    .WithArgs<UserSearchArgs>(args =>
    {
        args.SearchTerm = "john";
        args.IncludeInactive = false;
        args.Role = "admin";
    })
    .Build();
```

### Reset and Reload
```csharp
var paginator = client.PaginationSlice
    .Query<Order>("orders:list")
    .WithPageSize(20)
    .Build();

// Load some pages
await paginator.LoadNextAsync();
await paginator.LoadNextAsync();

// Reset to start over
paginator.Reset();

// Start loading from beginning again
var firstPage = await paginator.LoadNextAsync();
```

## Implementation Details
- Uses direct HTTP POST to `/api/query` endpoint
- Merges user arguments with pagination options automatically
- Maintains thread-safe state with lock-based synchronization
- Cursor is managed automatically between page loads
- Returns empty list when no more pages available
- Supports async enumeration for convenient iteration
- Preserves all user arguments while adding `paginationOpts`

## State Management
- **_loadedItems**: List of all items loaded across pages
- **_continueCursor**: Cursor for next page
- **_hasMore**: Whether more pages are available
- **_loadedPageCount**: Total number of pages loaded
- All state changes are thread-safe using lock

## Error Handling
- Invalid page size → ArgumentOutOfRangeException
- Query failure → ConvexPaginationException with function name
- Deserialization failure → ConvexPaginationException
- Network errors → ConvexPaginationException with inner exception

## Pagination Flow
1. User creates paginator with builder
2. First LoadNextAsync creates PaginationOptions with cursor=null
3. Merges user args with paginationOpts
4. Makes HTTP POST to /api/query
5. Receives PaginationResult with page + continueCursor
6. Updates state (cursor, hasMore, loadedItems)
7. Subsequent LoadNextAsync uses continueCursor
8. Continues until isDone=true

## Thread Safety
All pagination state is protected by `_paginationLock`:
- HasMore, LoadedPageCount, LoadedItems are safe to read
- LoadNextAsync safely updates state
- Reset safely clears all state
- Multiple concurrent calls to LoadNextAsync are serialized

## Limitations
- Live subscriptions (LivePaginatedSubscription) not yet migrated
- No support for split cursors (splitCursor in response)
- No automatic page size adjustment based on PageStatus

## Owner
TBD
