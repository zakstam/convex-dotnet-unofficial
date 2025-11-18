# Caching Slice

## Purpose
Provides in-memory caching for query results with support for optimistic updates, pattern-based invalidation, and thread-safe operations.

## Responsibilities
- In-memory query result caching
- Optimistic cache updates
- Pattern-based cache invalidation
- Thread-safe concurrent access
- Cache metadata tracking (timestamps, types)

## Public API Surface

### Main Interface
```csharp
public interface IConvexCache
{
    // Cache operations
    bool TryGet<T>(string queryName, out T? value);
    void Set<T>(string queryName, T value);
    bool TryUpdate<T>(string queryName, Func<T, T> updateFn);

    // Invalidation
    bool Remove(string queryName);
    int RemovePattern(string pattern);
    void Clear();

    // Metadata
    int Count { get; }
    IEnumerable<string> Keys { get; }
}
```

### Exception Types
```csharp
public class ConvexCacheException : Exception
{
    public string? QueryName { get; }
}
```

## Shared Dependencies
None - this is a pure in-memory slice with no external dependencies.

## Architecture
- **CachingSlice**: Public facade implementing IConvexCache
- **CacheImplementation**: Internal implementation using ConcurrentDictionary
- **CacheEntry**: Internal type tracking value, type, and timestamp
- **No HTTP calls**: Pure client-side state management

## Usage Examples

### Basic Caching
```csharp
// Set a value in cache
client.CachingSlice.Set("todos:list", todosList);

// Try to get from cache
if (client.CachingSlice.TryGet<List<Todo>>("todos:list", out var cachedTodos))
{
    Console.WriteLine($"Cache hit: {cachedTodos.Count} todos");
}
else
{
    Console.WriteLine("Cache miss");
}
```

### Optimistic Updates
```csharp
// Optimistically update a todo in the cached list
var updated = client.CachingSlice.TryUpdate<List<Todo>>("todos:list", todos =>
{
    var todo = todos.FirstOrDefault(t => t.Id == todoId);
    if (todo != null)
    {
        todo.Completed = true;
    }
    return todos;
});

if (updated)
{
    Console.WriteLine("Cache updated optimistically");
}
```

### Pattern-Based Invalidation
```csharp
// Invalidate all todo-related queries
var removedCount = client.CachingSlice.RemovePattern("todos:*");
Console.WriteLine($"Invalidated {removedCount} todo queries");

// Specific patterns
client.CachingSlice.RemovePattern("users:*");      // All user queries
client.CachingSlice.RemovePattern("messages:*");   // All message queries
client.CachingSlice.RemovePattern("*");            // All queries (equivalent to Clear)
```

### Cache Management
```csharp
// Check cache size
Console.WriteLine($"Cache contains {client.CachingSlice.Count} entries");

// List all cached queries
foreach (var queryName in client.CachingSlice.Keys)
{
    Console.WriteLine($"Cached: {queryName}");
}

// Remove specific entry
client.CachingSlice.Remove("todos:list");

// Clear entire cache
client.CachingSlice.Clear();
```

### Integration with Mutations
```csharp
// Example: Optimistic update pattern
async Task<Todo> UpdateTodoAsync(string todoId, bool completed)
{
    // 1. Optimistically update cache
    client.CachingSlice.TryUpdate<List<Todo>>("todos:list", todos =>
    {
        var todo = todos.FirstOrDefault(t => t.Id == todoId);
        if (todo != null)
        {
            todo.Completed = completed;
        }
        return todos;
    });

    // 2. Send mutation to server
    var result = await client.MutateAsync<Todo>(
        "todos:update",
        new { id = todoId, completed }
    );

    // 3. Update cache with server result
    client.CachingSlice.TryUpdate<List<Todo>>("todos:list", todos =>
    {
        var index = todos.FindIndex(t => t.Id == todoId);
        if (index >= 0)
        {
            todos[index] = result;
        }
        return todos;
    });

    return result;
}
```

## Implementation Details
- Uses `ConcurrentDictionary<string, CacheEntry>` for thread-safe storage
- Each entry stores: value, type info, and timestamp
- Type safety enforced at runtime via generics
- Pattern matching uses regex conversion from glob patterns
- Supports wildcards: `*` (any characters), `?` (single character)
- Optimistic updates use compare-and-swap for thread safety

## Thread Safety
All operations are thread-safe:
- **TryGet**: Lock-free reads from ConcurrentDictionary
- **Set**: Thread-safe writes
- **TryUpdate**: Atomic compare-and-swap operation
- **Remove**: Thread-safe removal
- **RemovePattern**: Safe iteration with concurrent removals
- **Clear**: Thread-safe dictionary clear
- **Count/Keys**: Consistent snapshot at time of access

## Pattern Matching
Glob patterns are converted to regex:
- `todos:*` → Matches `todos:list`, `todos:count`, `todos:get`, etc.
- `*:list` → Matches `todos:list`, `users:list`, etc.
- `user:*:profile` → Matches `user:123:profile`, `user:456:profile`, etc.
- `*` → Matches all entries

## Cache Entry Metadata
Each cached value includes:
- **Value**: The cached object
- **ValueType**: Runtime type information for type safety
- **Timestamp**: When the value was cached (for potential TTL features)

## Performance Characteristics
- **Get**: O(1) average case
- **Set**: O(1) average case
- **Update**: O(1) average case (compare-and-swap)
- **Remove**: O(1) average case
- **RemovePattern**: O(n) where n = number of keys
- **Clear**: O(n) where n = number of entries

## Limitations
- No TTL (time-to-live) expiration
- No size limits or LRU eviction
- No persistence across application restarts
- Pattern matching is case-insensitive
- Memory usage grows with number of cached entries

## Use Cases
- Query result caching to reduce API calls
- Optimistic UI updates before server confirmation
- Bulk invalidation after mutations
- Temporary client-side state management
- Performance optimization for frequently accessed data

## Owner
TBD
