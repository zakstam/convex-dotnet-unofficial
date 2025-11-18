# Subscriptions Slice

## Purpose

Provides utility classes and extension methods to simplify working with Convex real-time subscriptions obtained through `IConvexClient.Observe()`.

**Note**: This is a utility slice. The core subscription functionality (creating observables from Convex queries) is directly exposed on `IConvexClient` via the `Observe<T>()` methods. This slice provides optional helper classes to make working with those subscriptions easier.

## Responsibilities

- **ObservableConvexList<T>**: Thread-safe observable collection with UI synchronization support
- **SubscriptionExtensions**: Extension methods for binding observables to collections

## Key Classes

### ObservableConvexList<T>

A thread-safe observable collection designed for UI data binding with Convex subscriptions.

**Features**:
- Implements `IList<T>`, `INotifyCollectionChanged`, `INotifyPropertyChanged`
- Thread-safe operations with locking
- UI thread marshalling via `SynchronizationContext`
- Automatic synchronization with observables via `BindToObservable()`
- Efficient batch operations (`AddRange`, `ReplaceAll`, `RemoveAll`)

**Usage**:
```csharp
// Create and bind to observable
var todoList = new ObservableConvexList<Todo>();
var observable = client.Observe<Todo[]>("todos:list");
todoList.BindToObservable(observable);

// Use with UI framework (WPF, MAUI, etc.)
listView.ItemsSource = todoList;
```

### SubscriptionExtensions

Extension methods for `IObservable<T>` to simplify collection synchronization.

**Methods**:
- `ToObservableList<T>()`: Creates auto-syncing ObservableConvexList
- `BindToCollection<T>()`: Syncs observable to any ICollection<T>
- `BindToList<T>()`: Syncs observable to IList<T>
- `SyncTo<T>()`: Syncs single value to mutable reference

**Usage**:
```csharp
// One-liner to create auto-syncing list
var todoList = client.Observe<Todo[]>("todos:list")
    .ToObservableList();

// Bind to existing collection
var myCollection = new ObservableCollection<Todo>();
using var binding = client.Observe<Todo[]>("todos:list")
    .BindToCollection(myCollection);

// Sync single value
private User? _currentUser;
using var binding = client.Observe<User>("users:current")
    .SyncTo(value => _currentUser = value);
```

## Dependencies

### Shared Infrastructure
- None - this slice only depends on standard .NET libraries and System.Reactive

### Related Slices
- None - utilities are standalone and work with any IObservable source

## Public API

This slice provides utility classes and extension methods, not a service-style API:

### Classes
- `ObservableConvexList<T>`: Thread-safe observable collection
- `SubscriptionExtensions`: Static extension methods

## Architecture Notes

### Why This Is a "Utility Slice"

Unlike operational slices (Queries, Mutations, Actions) that encapsulate business operations, the Subscriptions slice provides utilities that users *optionally* apply to subscriptions they obtain from `IConvexClient.Observe()`.

**Core subscription flow**:
1. User calls `client.Observe<T>("function")` → Returns `IObservable<T>`
2. User can use the observable directly with Rx operators
3. OR user can use utilities from this slice for common scenarios:
   - UI binding with `ObservableConvexList<T>`
   - Collection sync with `BindToCollection()`
   - Value sync with `SyncTo()`

### Design Decisions

**Why static extension methods?**
- Provides fluent API: `observable.ToObservableList()`
- Works with any IObservable source (not just Convex)
- No service instance needed

**Why ObservableConvexList instead of ObservableCollection?**
- Thread-safe by design (critical for subscription callbacks)
- Built-in UI thread marshalling
- Optimized ReplaceAll() for subscription updates
- Subscription binding lifecycle management

**Why not expose on IConvexClient?**
- These are optional utilities, not core functionality
- Users might want custom collection handling
- Keeps IConvexClient interface focused on core operations
- Extension methods provide better composability

## Integration with ConvexClient

The ConvexClient directly implements subscription functionality:

```csharp
// ConvexClient.cs
public IObservable<T> Observe<T>(string functionName)
{
    // Creates observable from WebSocket LiveQuery
    var source = _webSocketClient.Value.LiveQuery<T>(functionName);
    return source.ToObservable();
}
```

Users then optionally use utilities from this slice:

```csharp
// Option 1: Use utilities
var list = client.Observe<Todo[]>("todos:list").ToObservableList();

// Option 2: Use Rx operators directly
var subscription = client.Observe<Todo[]>("todos:list")
    .Where(todos => todos.Any())
    .Subscribe(todos => Console.WriteLine($"Found {todos.Length} todos"));
```

## Owner

TBD

## Related Documentation

- [System.Reactive Documentation](https://github.com/dotnet/reactive)
- [INotifyCollectionChanged](https://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.inotifycollectionchanged)
