# Convex.Client.Extensions

Core extension library for the Convex .NET Client. Compatible with all .NET platforms including Blazor WebAssembly, MAUI, WPF, Console apps, and ASP.NET Core.

## Overview

`Convex.Client.Extensions` provides helper utilities and dependency injection extensions for the Convex .NET Client. This package is **WASM-compatible** and works on all .NET platforms.

**For ASP.NET Core server features** (middleware, health checks), see the companion package: [`Convex.Client.Extensions.AspNetCore`](../Convex.Client.Extensions.AspNetCore/README.md)

## Installation

```bash
dotnet add package Convex.Client.Extensions
```

**Prerequisites**: Requires `Convex.Client` package.

## What's Included

### Simplified API (No More Guessing!)

**Problem Solved**: No more confusing `<TResult, object>` type parameters AND no more guessing what arguments to pass!

The extension methods eliminate verbose type parameters AND work consistently with ANY Convex function signature:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// ✅ Functions with parameters
await client.QueryAsync<List<Todo>>("todos:search", new { userId = 123 });
await client.MutateAsync<Todo>("todos:create", new { text = "Buy milk" });
var observable = client.Observe<Message[]>("messages:list", new { roomId = "abc" });

// ✅ Functions without parameters - just omit args!
// Works for ANY Convex function signature - you don't need to check the backend!
await client.QueryAsync<List<Todo>>("todos:list");
await client.MutateAsync<Result>("cache:clear");
var observable = client.Observe<Message[]>("messages:all");
```

**Key Benefit: No More Guessing!**

You don't need to look at the Convex backend function definition to know whether to pass an empty object or omit arguments. Just omit the `args` parameter and it works for ALL Convex functions:

- Functions defined with `args: { ... }` → Works! ✅
- Functions defined without `args` → Works! ✅
- Functions with optional args → Works! ✅
- Functions with required args → Just pass them! ✅

**Before (verbose and confusing):**
```csharp
// 😢 Old way - type parameter hell + guessing game
await client.QueryAsync<List<Todo>, object>("todos:search", new { userId = 123 });

// 😢 Do I pass null? new { }? Omit it? Need to check backend!
await client.QueryAsync<List<Todo>, object>("todos:list", ???);
```

**After (clean and no guessing):**
```csharp
// 😊 New way - simple and consistent, works for any function!
await client.QueryAsync<List<Todo>>("todos:search", new { userId = 123 });
await client.QueryAsync<List<Todo>>("todos:list");  // Always works!
```

### How Argument Handling Works

The Convex .NET client automatically handles empty arguments for you with **defense in depth**:

**What Gets Sent to Convex:**
- **No args provided:** Sends `[{}]` (empty object in array) ✅
- **Null args:** Automatically converted to `[{}]` ✅
- **Empty object `new {}`:** Sends `[{}]` ✅
- **With args `new { id = 123 }`:** Sends `[{ id: 123 }]` ✅

**Multiple Layers of Safety:**

1. **Extension Methods:** Automatically use `args ?? new {}` to ensure you always pass an object
2. **Core Client Methods:** No-args overloads explicitly pass `new {}` instead of `null`
3. **Serialization Layer:** Converts `null` to `new {}` before wrapping in array (defense in depth)

**Why This Matters:**

Convex backend functions defined with an `args` parameter (even if properties are optional) require receiving an object argument. The .NET client ensures this happens automatically at multiple levels, so you **never need to guess or worry about it**.

**You can use ANY of these approaches and they all work:**
```csharp
// Extension method - omit args parameter
await client.QueryAsync<Result>("function");

// Extension method - pass null explicitly
await client.QueryAsync<Result>("function", null);

// Extension method - pass empty object explicitly
await client.QueryAsync<Result>("function", new { });

// Core client - no-args overload
await client.QueryAsync<Result>("function");

// All of the above send [{}] to Convex ✅
```

### Developer Experience Helpers

The extensions package now includes comprehensive developer experience helpers that dramatically improve productivity by providing ready-to-use patterns for common Convex application scenarios.

#### Reactive Extensions (Rx) Patterns

Simplify complex observable operations with pre-built patterns:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Retry with exponential backoff
var result = client.QueryAsync<List<Todo>>("todos:list")
    .RetryWithBackoff(maxRetries: 3, initialDelay: TimeSpan.FromSeconds(1));

// Smart debouncing (preserves first + last values)
var searchResults = searchText
    .SmartDebounce(TimeSpan.FromMilliseconds(300))
    .SelectMany(term => client.QueryAsync<List<Todo>>("todos:search", new { term }));

// Share subscriptions with automatic cleanup
var sharedData = client.Observe<List<Todo>>("todos:list")
    .ShareReplayLatest();

// Connection-aware operations
var connectedUpdates = client.Observe<Todo>("todos:updates")
    .WhenConnected(client);

// Buffer during poor connection
var batchedUpdates = client.Observe<Todo>("todos:stream")
    .BufferDuringPoorConnection(client, bufferSize: 50);
```

#### UI Framework Integrations

Seamlessly integrate with popular UI frameworks:

**WPF / MAUI:**

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Auto-sync ObservableCollection
var todosCollection = client.Observe<List<Todo>>("todos:list")
    .ToObservableCollection();

// Reactive property binding
client.Observe<User>("users:current")
    .BindToProperty(myViewModel, nameof(myViewModel.CurrentUser));

// Command enablement
var canSave = client.ConnectionStateChanges
    .Select(state => state == ConnectionState.Connected)
    .BindToCanExecute(saveCommand);
```

**Blazor (requires separate package):**

```bash
dotnet add package Convex.Client.Extensions.Blazor
```

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Automatic StateHasChanged
client.Observe<List<Todo>>("todos:list")
    .SubscribeWithStateHasChanged(this, todos =>
    {
        this.todos = todos;
        StateHasChanged();
    });

// Async enumeration for streaming
await foreach (var update in client.Observe<Todo>("todos:stream")
    .ToAsyncEnumerable())
{
    await ProcessUpdate(update);
}

// Two-way form binding
client.Observe<User>("users:current")
    .BindToForm(this, user => this.currentUser = user);
```

See [`Convex.Client.Extensions.Blazor`](../Convex.Client.Extensions.Blazor/README.md) for complete Blazor integration details.

#### Testing Utilities

Comprehensive testing support with mocks and utilities:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Create mock client
var mockClient = ConvexTestingExtensions.CreateMockClient(builder =>
    builder.SetupQuery("todos:list", new List<Todo>())
           .SetupMutation("todos:create", new Todo()));

// Record observable emissions
var recorder = client.Observe<List<Todo>>("todos:list")
    .Record();

// Wait for specific values
await recorder.WaitForValue(todos => todos.Count > 0, timeout: TimeSpan.FromSeconds(5));

// Simulate connection issues
client.SimulateIntermittentConnection(
    connectedDuration: TimeSpan.FromSeconds(5),
    disconnectedDuration: TimeSpan.FromSeconds(2));
```

#### Error Handling Patterns

Robust error recovery and monitoring:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Conditional retry
var result = client.QueryAsync<List<Todo>>("todos:list")
    .RetryWhen(ex => ex is ConvexNetworkException, maxRetries: 5);

// Circuit breaker
var safeClient = client.WithCircuitBreaker(
    failureThreshold: 5,
    recoveryTimeout: TimeSpan.FromMinutes(1));

// Custom timeout messages
var result = client.QueryAsync<List<Todo>>("todos:list")
    .TimeoutWithMessage(TimeSpan.FromSeconds(10), "Query timed out");

// Error reporting with context
client.Observe<List<Todo>>("todos:list")
    .CatchAndReport(
        () => new { UserId = currentUserId, Timestamp = DateTime.UtcNow },
        (ex, context) => logger.LogError(ex, "Failed to load todos for user {UserId}", context.UserId));
```

#### Performance Optimizations

Built-in performance helpers for high-throughput scenarios:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Multi-key change detection
client.Observe<Todo>("todos:updates")
    .DistinctUntilChangedBy(t => t.Id, t => t.LastModified, t => t.Status);

// Rate limiting
var throttledUpdates = client.Observe<Todo>("todos:stream")
    .ThrottleToMaxFrequency(TimeSpan.FromMilliseconds(100));

// Sliding window rate limiting
var rateLimited = updates.ThrottleSlidingWindow(maxEmissions: 10, timeWindow: TimeSpan.FromSeconds(1));

// Intelligent batching
var batched = client.Observe<Todo>("todos:stream")
    .BatchUpdates(TimeSpan.FromMilliseconds(100), maxBatchSize: 50);

// Performance monitoring
client.Observe<List<Todo>>("todos:list")
    .WithPerformanceLogging("TodoList", logInterval: TimeSpan.FromMinutes(1));
```

#### Common Usage Patterns

Ready-to-use implementations for typical application scenarios:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

// Infinite scroll
var items = client.CreateInfiniteScroll<Item>(
    "items:list",
    new { category = "electronics" },
    pageSize: 20,
    loadThreshold: 5);

// Debounced search
var searchHandler = client.CreateDebouncedSearch<SearchResult>(
    "search:query",
    debounceTime: TimeSpan.FromMilliseconds(300));

private void OnSearchTextChanged(string text)
    => searchHandler(text).Subscribe(results => UpdateSearchResults(results));

// Connection status indicator
var statusIndicator = client.CreateConnectionIndicator();
statusIndicator.Subscribe(status => connectionLabel.Text = status);

// Detailed connection status
var detailedStatus = client.CreateDetailedConnectionIndicator();
detailedStatus.Subscribe(status => {
    statusIcon.Source = status.Icon;
    statusLabel.Text = status.Message;
    retryButton.IsVisible = status.CanRetry;
});

// Resilient subscriptions
var messages = client.CreateResilientSubscription<Message>(
    "messages:subscribe",
    new { channelId });
```

### Helper Utilities

#### Timestamp Conversion

```csharp
using Convex.Client.Extensions.Converters;

// Convert DateTime to Convex timestamp
var timestamp = DateTime.UtcNow.ToConvexTimestamp();

// Convert back to DateTime
var dateTime = TimestampConverter.FromConvexTimestamp(timestamp);
```

#### Fluent Argument Building

```csharp
using Convex.Client.Extensions.ArgumentBuilders;

var args = new ArgumentBuilder()
    .Add("title", "Buy groceries")
    .AddDateTime("dueDate", DateTime.Now.AddDays(7))
    .AddArray("tags", new[] { "shopping", "urgent" })
    .Build();

await client.MutationAsync("todos:create", args);
```

#### Result Pattern (Railway-Oriented Programming)

```csharp
using Convex.Client.Extensions.ResultWrappers;

var result = await client.TryQueryAsync<List<Todo>>("todos:list");
result.Match(
    onSuccess: todos => Console.WriteLine($"Found {todos.Count} todos"),
    onFailure: error => Console.WriteLine($"Error: {error.Message}")
);
```

#### Retry Logic

```csharp
using Convex.Client.Extensions.ExtensionMethods;

var todos = await client.QueryWithRetryAsync<List<Todo>>(
    "todos:list",
    maxRetries: 3,
    retryDelay: TimeSpan.FromSeconds(1)
);
```

#### Batch Operations

```csharp
using Convex.Client.Extensions.Batching;

var batch = new BatchOperationBuilder(client)
    .AddQuery<int>("todos:count")
    .AddQuery<int>("users:count")
    .AddMutation<string>("todos:create", new { text = "New task" });

var results = await batch.ExecuteParallelAsync();
```

#### Pagination Helpers

```csharp
using Convex.Client.Extensions.ExtensionMethods;

await foreach (var todo in client.PaginateAsync<Todo>("todos:paginated", pageSize: 10))
{
    Console.WriteLine(todo.Text);
}
```

#### Empty Arguments Helper

```csharp
using Convex.Client.Extensions.EmptyArgs;

// Clean way to call functions with no arguments
var todos = await client.QueryAsync<List<Todo>>("todos:list", NoArgs.Instance);
```

### Dependency Injection

Works with all .NET platforms including Blazor WASM, MAUI, WPF, Console apps, and ASP.NET Core:

```csharp
using Convex.Client.Extensions.DependencyInjection;

// Simple registration
builder.Services.AddConvex("https://your-deployment.convex.cloud");

// Or with configuration
builder.Services.AddConvex(builder.Configuration);

// Or with options
builder.Services.AddConvex(options =>
{
    options.DeploymentUrl = "https://your-deployment.convex.cloud";
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

#### Configuration from appsettings.json

```json
{
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud",
    "AdminKey": "your-admin-key",
    "Timeout": "00:00:30"
  }
}
```

### ASP.NET Core Features (Separate Package)

**For ASP.NET Core middleware and health checks**, install the companion package:

```bash
dotnet add package Convex.Client.Extensions.AspNetCore
```

See [`Convex.Client.Extensions.AspNetCore`](../Convex.Client.Extensions.AspNetCore/README.md) for:
- Authentication middleware
- Health checks
- HttpContext integration

## Features by Namespace

### Developer Experience Helpers

#### `Convex.Client.Extensions.ExtensionMethods`

- **Rx Patterns**: `RetryWithBackoff()`, `SmartDebounce()`, `ShareReplayLatest()`, `WhenConnected()`, `BufferDuringPoorConnection()`
- **UI Framework**: `ObserveOnUI()`, `BindToProperty()`, `BindToCanExecute()`, `ToObservableCollection()`, `SubscribeWithStateHasChanged()`, `ToAsyncEnumerable()`, `BindToForm()`
- **Testing**: `CreateMockClient()`, `Record()`, `WaitForValue()`, `SimulateIntermittentConnection()`
- **Error Handling**: `RetryWhen()`, `WithCircuitBreaker()`, `TimeoutWithMessage()`, `CatchAndReport()`
- **Performance**: `DistinctUntilChangedBy()`, `ThrottleToMaxFrequency()`, `ThrottleSlidingWindow()`, `BatchUpdates()`, `WithPerformanceLogging()`
- **Common Patterns**: `CreateInfiniteScroll()`, `CreateDebouncedSearch()`, `CreateConnectionIndicator()`, `CreateResilientSubscription()`

### Legacy Helper Utilities

#### `Convex.Client.Extensions.Converters`

- `TimestampConverter` - DateTime ↔ Convex timestamp conversion
- Extension methods: `ToConvexTimestamp()`, `FromConvexTimestamp()`

#### `Convex.Client.Extensions.ArgumentBuilders`

- `ArgumentBuilder` - Fluent API for building function arguments
- Type-safe argument construction with validation

#### `Convex.Client.Extensions.ResultWrappers`

- `Result<T>` - Railway-oriented programming pattern
- `Success<T>()`, `Failure<T>()` factory methods
- Pattern matching with `Match()` method

#### `Convex.Client.Extensions.ExtensionMethods`

- `QueryWithRetryAsync()` - Automatic retry logic
- `PaginateAsync()` - Async enumerable pagination
- `BatchQueryAsync()` - Execute multiple queries in parallel

#### `Convex.Client.Extensions.Batching`

- `BatchOperationBuilder` - Fluent batch operation building
- `ExecuteParallelAsync()` - Parallel execution
- `ExecuteSequentialAsync()` - Sequential execution with dependencies

#### `Convex.Client.Extensions.EmptyArgs`

- `NoArgs` - Singleton for functions with no arguments

#### `Convex.Client.Extensions.DependencyInjection`

- `AddConvex()` - Configure Convex in DI container (all platforms)
- `ConvexClientOptions` - Configuration options
- Service lifetime management (Singleton)

## Migration Guides

### From v1.x to v2.0

**Breaking Change**: ASP.NET Core features have been moved to a separate package for WASM compatibility.

#### For Blazor WebAssembly Projects

No changes needed! Just upgrade:

```bash
dotnet add package Convex.Client.Extensions --version 2.0.0
```

#### For ASP.NET Core Projects Using Middleware/Health Checks

1. Install the AspNetCore package:

```bash
dotnet add package Convex.Client.Extensions.AspNetCore --version 2.0.0
```

2. Update namespace imports:

```csharp
// OLD (v1.x)
using Convex.Client.Extensions.Middleware;
using Convex.Client.Extensions.HealthChecks;

// NEW (v2.0+)
using Convex.Client.Extensions.AspNetCore.Middleware;
using Convex.Client.Extensions.AspNetCore.HealthChecks;
```

3. DI extensions remain unchanged:

```csharp
// Still works the same
using Convex.Client.Extensions.DependencyInjection;
builder.Services.AddConvex(configuration);
```

### From Convex.Client.Helpers or Convex.Client.AspNetCore

If you were using the legacy packages:

1. Remove old packages:

```bash
dotnet remove package Convex.Client.Helpers
dotnet remove package Convex.Client.AspNetCore
```

2. Add new packages:

```bash
dotnet add package Convex.Client.Extensions --version 2.0.0
# If using ASP.NET Core features:
dotnet add package Convex.Client.Extensions.AspNetCore --version 2.0.0
```

3. Update namespaces:

- `Convex.Client.Helpers.*` → `Convex.Client.Extensions.*`
- `Convex.Client.AspNetCore.*` → `Convex.Client.Extensions.AspNetCore.*`

## Requirements

- .NET 8.0 or .NET 9.0
- Convex.Client package
- **Platform**: All platforms (Blazor WASM, MAUI, WPF, Console, ASP.NET Core)
- **For ASP.NET Core features**: Install `Convex.Client.Extensions.AspNetCore` separately

## Examples

See the following example projects:

- [HelpersDemo](../../examples/HelpersDemo) - Helper utilities demonstration
- [FeatureShowcaseApi](../../examples/FeatureShowcaseApi) - ASP.NET Core integration

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Support

- 📖 [Documentation](https://github.com/zakstam/convex-dotnet)
- 🐛 [Issue Tracker](https://github.com/zakstam/convex-dotnet/issues)
- 💬 [Discord Community](https://convex.dev/community)

