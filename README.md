<div align="center">

<img src="icon.png" alt="Convex .NET Client Logo" width="200" />

# Convex .NET SDK

The .NET SDK for [Convex](https://convex.dev) - build **real-time reactive applications** with live data subscriptions, automatic synchronization, and type-safe backend queries. No WebSocket plumbing, no polling, no complexity.

[![NuGet](https://img.shields.io/nuget/v/Convex.Client.svg)](https://www.nuget.org/packages/Convex.Client/)
[![Build Status](https://github.com/zakstam/convex-dotnet-unofficial/actions/workflows/ci.yml/badge.svg)](https://github.com/zakstam/convex-dotnet-unofficial/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![Status](https://img.shields.io/badge/Status-ALPHA-orange)
![Community](https://img.shields.io/badge/Project-Community-blue)

### 🎯 Supported Platforms

| Platform           | Version | Status                                                |
| ------------------ | ------- | ----------------------------------------------------- |
| **.NET Standard**  | 2.1+    | ✅ Full support                                       |
| **.NET**           | 8.0+    | ✅ Full support                                       |
| **.NET**           | 9.0+    | ✅ Full support                                       |
| **Unity**          | 2021.3+ | ✅ Compatible (via .NET Standard 2.1)                 |
| **Godot**          | 4.0+    | ✅ Compatible (via .NET Standard 2.1)                 |
| **Xamarin / MAUI** | All     | ✅ Compatible via .NET Standard 2.1                   |
| **WPF / WinForms** | All     | ✅ Full support                                       |
| **ASP.NET Core**   | 6.0+    | ✅ Full support (Extensions.AspNetCore requires 8.0+) |
| **Blazor**         | All     | ✅ Compatible                                         |
| **Console Apps**   | All     | ✅ Full support                                       |

**Target Frameworks:** `netstandard2.1`, `net8.0`, `net9.0`

---

### ⚠️ ALPHA - Community Project

**This is an ALPHA release of a community-driven Convex client for .NET.**
This project is actively maintained by the community and provides protocol compatibility with Convex.
**NOT recommended for production use yet.** API may change, breaking changes expected.

[Report Issues](https://github.com/zakstam/convex-dotnet-unofficial/issues) • [Request Features](https://github.com/zakstam/convex-dotnet-unofficial/issues/new?labels=enhancement) • [Join Discord](https://convex.dev/community)

</div>

### Demo (Second phase is running the seed script)

![Demo](demo.gif)

---

## 📑 Quick Navigation

**[Quick Start](#-quick-start)** • [Core Features](#-core-features) • [Advanced Features](#-advanced-features) • [UI Integrations](#-ui-integrations) • [Authentication](#-authentication) • [Developer Tools](#-developer-tools) • [Reference](#-reference)

---

## 🚀 Quick Start

Follow these steps to build your first real-time app with Convex + .NET. See the [detailed getting started guide](docs/getting-started.md) for more information.

### Step 1: Create Your Convex Backend

Write your backend functions in TypeScript (Convex's native language):

```typescript
// convex/functions/todos.ts
import { query, mutation } from "./_generated/server";
import { v } from "convex/values";

export default query({
  handler: async (ctx) => {
    return await ctx.db.query("todos").collect();
  },
});
```

```typescript
// convex/functions/createTodo.ts
import { mutation } from "./_generated/server";
import { v } from "convex/values";

export default mutation({
  args: { text: v.string() },
  handler: async (ctx, args) => {
    const id = await ctx.db.insert("todos", {
      text: args.text,
      isCompleted: false,
      createdAt: Date.now(),
    });
    return await ctx.db.get(id);
  },
});
```

### Step 2: Deploy Your Backend

```bash
# Initialize Convex project (if needed)
cd backend
npm install
npx convex dev

# This automatically generates convex/_generated/api.d.ts
```

Convex automatically generates `convex/_generated/api.d.ts` with your function definitions. Function names match file paths: `convex/functions/list.ts` → `"functions/list"`.

### Step 3: Install Convex.Client

```bash
dotnet add package Convex.Client
```

That's it - one package includes everything: real-time client, analyzers, and all features.

### Step 4: (Optional) Generate Type-Safe Constants

Point the source generator to your `api.d.ts` file for type-safe function names:

```xml
<!-- In your .csproj -->
<ItemGroup>
  <AdditionalFiles Include="../backend/convex/_generated/api.d.ts" />
</ItemGroup>
```

Build your project - the generator creates C# constants from your TypeScript functions:

```csharp
// Auto-generated in obj/Debug/generated/Convex.FunctionGenerator/ConvexFunctions.g.cs
namespace Convex.Generated
{
    public static class ConvexFunctions
    {
        public static class Queries
        {
            public const string List = "functions/list";
        }
        public static class Mutations
        {
            public const string CreateTodo = "functions/createTodo";
        }
    }
}
```

**Note:** Function names match file paths. `convex/functions/createTodo.ts` becomes `"functions/createTodo"`.

### Step 5: Define Your C# Data Models

**Option A: Auto-generate from schema.ts (Recommended)**

Point the schema generator to your `schema.ts` file for type-safe models:

```xml
<!-- In your .csproj -->
<ItemGroup>
  <AdditionalFiles Include="../backend/convex/schema.ts" />
</ItemGroup>
```

Build your project - the generator creates C# classes from your schema:

```csharp
// Auto-generated in obj/Debug/generated/Convex.SchemaGenerator/Todos.g.cs
namespace Convex.Generated.Models
{
    public class Todos
    {
        [JsonPropertyName("_id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("_creationTime")]
        public double CreationTime { get; init; }

        [JsonPropertyName("text")]
        public string Text { get; init; } = default!;

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; init; }
    }
}
```

See [Schema Generator documentation](src/Convex.SchemaGenerator/README.md) for complete type mapping.

**Option B: Manual model definition**

Create C# models that match your Convex schema:

```csharp
public class Todo
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 6: Use Real-Time Client in Your App

Connect and subscribe to live data that updates automatically:

```csharp
using Convex.Client;
using Convex.Generated; // Optional: for type-safe constants

// Simple: Direct instantiation
var client = new ConvexClient("https://your-deployment.convex.cloud");

// Or: Use builder for advanced configuration
var client = new ConvexClientBuilder()
    .UseDeployment("https://your-deployment.convex.cloud")
    .WithAutoReconnect(maxAttempts: 5)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .Build();

// Subscribe to live todos - updates automatically when data changes!
client.Observe<List<Todo>>("functions/list")
    // Or: ConvexFunctions.Queries.List (if using generator)
    .Subscribe(todos =>
    {
        Console.WriteLine($"📝 {todos.Count} todos (live update!)");
        foreach (var todo in todos)
        {
            var status = todo.IsCompleted ? "✓" : " ";
            Console.WriteLine($"  [{status}] {todo.Text}");
        }
    });

// Create a new todo
await client.Mutate<Todo>("functions/createTodo")
    // Or: ConvexFunctions.Mutations.CreateTodo (if using generator)
    .WithArgs(new { text = "Learn Convex .NET" })
    .ExecuteAsync();

Console.WriteLine("Watching for changes... Press any key to exit");
Console.ReadKey();
client.Dispose();
```

**That's it!** You now have a real-time reactive app. Any changes from other clients appear instantly.

### Complete Example

Here's a complete working example:

```csharp
using Convex.Client;
using Convex.Generated; // Optional: for type-safe constants

class Program
{
    static async Task Main(string[] args)
    {
        var client = new ConvexClient("https://your-deployment.convex.cloud");

        // Subscribe to live updates
        var subscription = client.Observe<List<Todo>>("functions/list")
            // Or: ConvexFunctions.Queries.List (if using generator)
            .Subscribe(todos =>
            {
                Console.WriteLine($"\n📝 {todos.Count} todos:");
                foreach (var todo in todos)
                {
                    var status = todo.IsCompleted ? "✓" : " ";
                    Console.WriteLine($"  [{status}] {todo.Text}");
                }
            });

        // Create a new todo (using string or generated constant)
        await client.Mutate<Todo>("functions/createTodo")
            // Or: ConvexFunctions.Mutations.CreateTodo (if using generator)
            .WithArgs(new { text = "Learn Convex .NET" })
            .ExecuteAsync();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();

        subscription.Dispose();
        client.Dispose();
    }
}

public class Todo
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Next Steps:** Explore [Core Features](#-core-features) below or check out [complete examples](#-examples).

---

## 🎯 Core Features

### Real-Time Subscriptions

**Problem:** You want live updates when data changes, without polling or manual refresh.

**Solution:** Use `Observe()` for automatic synchronization via WebSocket.

**Complete Example:** Live todo list that updates automatically

```csharp
using Convex.Client;

class TodoApp
{
    private ConvexClient _client;
    private IDisposable? _subscription;

    public TodoApp(string deploymentUrl)
    {
        _client = new ConvexClient(deploymentUrl);
    }

    public void Start()
    {
        // Subscribe to live todos - automatically updates when data changes!
        _subscription = _client.Observe<List<Todo>>("todos:list")
            .Subscribe(todos =>
            {
                Console.Clear();
                Console.WriteLine($"📝 {todos.Count} todos (live update!)");
                foreach (var todo in todos)
                {
                    var status = todo.IsCompleted ? "✓" : " ";
                    Console.WriteLine($"  [{status}] {todo.Text}");
                }
            });

        Console.WriteLine("Watching for changes... Press any key to exit");
    }

    public void Stop()
    {
        _subscription?.Dispose();
        _client.Dispose();
    }
}
```

**Key Points:**

- `Observe()` creates a WebSocket subscription
- Updates arrive automatically when data changes
- Always dispose subscriptions and client when done

**Learn more:** [API Reference - Real-Time Subscriptions](docs/api-reference.md#real-time-subscriptions)

### Queries & Mutations

**Problem:** You need to read and write data with type safety and error handling.

**Solution:** Use `Query()` for reads and `Mutate()` for writes with fluent API.

**Complete Example:** Todo CRUD app

```csharp
using Convex.Client;

class TodoService
{
    private readonly ConvexClient _client;

    public TodoService(ConvexClient client)
    {
        _client = client;
    }

    // Read todos
    public async Task<List<Todo>> GetAllTodosAsync()
    {
        try
        {
            return await _client.Query<List<Todo>>("todos:list")
                .WithTimeout(TimeSpan.FromSeconds(5))
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading todos: {ex.Message}");
            return new List<Todo>();
        }
    }

    // Create todo
    public async Task<Todo> CreateTodoAsync(string text)
    {
        return await _client.Mutate<Todo>("todos:create")
            .WithArgs(new { text })
            .ExecuteAsync();
    }

    // Update todo
    public async Task<Todo> UpdateTodoAsync(string id, bool completed)
    {
        return await _client.Mutate<Todo>("todos:update")
            .WithArgs(new { id, completed })
            .ExecuteAsync();
    }

    // Delete todo
    public async Task DeleteTodoAsync(string id)
    {
        await _client.Mutate<object>("todos:delete")
            .WithArgs(new { id })
            .ExecuteAsync();
    }

    // Batch multiple queries
    public async Task<(List<Todo>, User, Stats)> GetDashboardDataAsync()
    {
        return await _client.Batch()
            .Query<List<Todo>>("todos:list")
            .Query<User>("users:current")
            .Query<Stats>("dashboard:stats")
            .ExecuteAsync<List<Todo>, User, Stats>();
    }
}
```

**Key Points:**

- Fluent API: `Query().WithArgs().ExecuteAsync()`
- Type-safe with generics
- Batch multiple queries for efficiency
- Built-in timeout and error handling

**Learn more:** [API Reference - Queries & Mutations](docs/api-reference.md#queries)

### Actions

**Problem:** You need to call external APIs or perform server-side operations.

**Solution:** Use `Action()` for side effects and external integrations.

**Complete Example:** Sending email action

```csharp
using Convex.Client;

class EmailService
{
    private readonly ConvexClient _client;

    public EmailService(ConvexClient client)
    {
        _client = client;
    }

    public async Task<string> SendWelcomeEmailAsync(string userId)
    {
        try
        {
            var result = await _client.Action<string>("emails:sendWelcome")
                .WithArgs(new
                {
                    userId,
                    template = "welcome",
                    subject = "Welcome to our app!"
                })
                .WithTimeout(TimeSpan.FromSeconds(30))
                .ExecuteAsync();

            Console.WriteLine($"Email sent: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
            throw;
        }
    }
}
```

**Key Points:**

- Actions can access external APIs
- Use for operations that aren't pure database operations
- Supports longer timeouts for external calls

**Learn more:** [API Reference - Actions](docs/api-reference.md#actions)

---

## 🚀 Advanced Features

### File Storage

**Problem:** You need to upload and serve files (images, documents, etc.).

**Solution:** Use `FileStorageSlice` for file operations.

**Complete Example:** Upload profile picture and display it

```csharp
using Convex.Client;

class ProfileService
{
    private readonly ConvexClient _client;

    public ProfileService(ConvexClient client)
    {
        _client = client;
    }

    public async Task<string> UploadProfilePictureAsync(Stream imageStream, string filename)
    {
        // Upload file
        var storageId = await _client.FileStorageSlice.UploadFileAsync(
            imageStream,
            contentType: "image/jpeg",
            filename: filename
        );

        Console.WriteLine($"Uploaded: {storageId}");
        return storageId;
    }

    public async Task<string> GetProfilePictureUrlAsync(string storageId)
    {
        // Get download URL
        var url = await _client.FileStorageSlice.GetDownloadUrlAsync(storageId);
        return url;
    }

    public async Task<Stream> DownloadProfilePictureAsync(string storageId)
    {
        // Download file directly
        return await _client.FileStorageSlice.DownloadFileAsync(storageId);
    }
}
```

**Key Points:**

- Upload returns a storage ID
- Get download URL for browser display
- Download directly for server processing

**Learn more:** [API Reference - File Storage](docs/api-reference.md#file-storage)

### Vector Search

**Problem:** You need AI-powered semantic search over your data.

**Solution:** Use `VectorSearchSlice` for similarity search.

**Complete Example:** Product search with embeddings

```csharp
using Convex.Client;

class ProductSearchService
{
    private readonly ConvexClient _client;

    public ProductSearchService(ConvexClient client)
    {
        _client = client;
    }

    public async Task<List<Product>> SearchSimilarProductsAsync(float[] queryEmbedding)
    {
        try
        {
            var results = await _client.VectorSearchSlice.SearchAsync<Product>(
                indexName: "product_embeddings",
                vector: queryEmbedding,
                limit: 10
            );

            var products = new List<Product>();
            foreach (var result in results)
            {
                Console.WriteLine($"{result.Item.Name} (similarity: {result.Score:F3})");
                products.Add(result.Item);
            }

            return products;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search failed: {ex.Message}");
            return new List<Product>();
        }
    }
}
```

**Key Points:**

- Requires pre-computed embeddings
- Returns results with similarity scores
- Use for semantic search, recommendations, etc.

**Learn more:** [API Reference - Vector Search](docs/api-reference.md#vector-search)

### Scheduling

**Problem:** You need to run functions at specific times or intervals.

**Solution:** Use `SchedulingSlice` for delayed and recurring jobs.

**Complete Example:** Reminder system

```csharp
using Convex.Client;

class ReminderService
{
    private readonly ConvexClient _client;

    public ReminderService(ConvexClient client)
    {
        _client = client;
    }

    public async Task<string> ScheduleReminderAsync(string userId, DateTime reminderTime)
    {
        var delay = reminderTime - DateTime.UtcNow;

        // Schedule one-time reminder
        var jobId = await _client.SchedulingSlice.ScheduleAsync(
            functionName: "reminders:send",
            delay: delay,
            args: new { userId }
        );

        Console.WriteLine($"Reminder scheduled: {jobId}");
        return jobId;
    }

    public async Task<string> ScheduleDailyDigestAsync(string userId)
    {
        // Schedule recurring daily digest at 9 AM
        var jobId = await _client.SchedulingSlice.ScheduleRecurringAsync(
            functionName: "emails:sendDailyDigest",
            cronExpression: "0 9 * * *", // Daily at 9 AM
            timezone: "America/New_York",
            args: new { userId }
        );

        return jobId;
    }

    public async Task CancelReminderAsync(string jobId)
    {
        var cancelled = await _client.SchedulingSlice.CancelAsync(jobId);
        if (cancelled)
        {
            Console.WriteLine($"Reminder cancelled: {jobId}");
        }
    }
}
```

**Key Points:**

- Schedule one-time, recurring (cron), or interval jobs
- Returns job ID for cancellation
- Supports timezone-aware scheduling

**Learn more:** [API Reference - Scheduling](docs/api-reference.md#scheduling)

### HTTP Actions

**Problem:** You need to call REST endpoints built with Convex HTTP Actions.

**Solution:** Use `HttpActionsSlice` for HTTP requests.

**Complete Example:** REST API integration

```csharp
using Convex.Client;
using System.Collections.Generic;

class ApiClient
{
    private readonly ConvexClient _client;

    public ApiClient(ConvexClient client)
    {
        _client = client;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        // GET request
        var response = await _client.HttpActionsSlice.GetAsync<User>(
            actionPath: $"users/{userId}",
            queryParameters: new Dictionary<string, string> { ["include"] = "profile" }
        );

        if (response.IsSuccess)
        {
            return response.Body;
        }

        Console.WriteLine($"Failed: {response.StatusCode}");
        return null;
    }

    public async Task<User> CreateUserAsync(User newUser)
    {
        // POST request with body
        var response = await _client.HttpActionsSlice.PostAsync<User, User>(
            actionPath: "users",
            body: newUser
        );

        if (response.IsSuccess)
        {
            return response.Body!;
        }

        throw new Exception($"Failed to create user: {response.StatusCode}");
    }
}
```

**Key Points:**

- GET and POST requests supported
- Type-safe request/response handling
- Check `IsSuccess` before using response body

**Learn more:** [API Reference - HTTP Actions](docs/api-reference.md#http-actions)

---

## 🎨 UI Integrations

### Blazor

**Problem:** You want real-time data in Blazor components with automatic UI updates.

**Solution:** Use Blazor extensions for StateHasChanged integration and form binding.

**Complete Example:** Blazor component with real-time data

```csharp
@page "/todos"
@using Convex.Client
@using Convex.Client.Extensions.ExtensionMethods
@inject IConvexClient Client
@implements IDisposable

<h3>Todo List (Live Updates)</h3>

@if (todos == null)
{
    <p>Loading...</p>
}
else
{
    <ul>
        @foreach (var todo in todos)
        {
            <li>
                <input type="checkbox" checked="@todo.IsCompleted" />
                @todo.Text
            </li>
        }
    </ul>
}

@code {
    private List<Todo>? todos;
    private IDisposable? subscription;

    protected override void OnInitialized()
    {
        // Subscribe with automatic StateHasChanged
        subscription = Client.Observe<List<Todo>>("todos:list")
            .SubscribeWithStateHasChanged(this, newTodos =>
            {
                todos = newTodos;
                StateHasChanged(); // Trigger UI update
            });
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}
```

**Key Points:**

- `SubscribeWithStateHasChanged()` handles component lifecycle
- `BindToForm()` for two-way form binding
- `ToAsyncEnumerable()` for async streaming

**Learn more:** Install `Convex.Client.Extensions.Blazor` package

### WPF / MAUI

**Problem:** You want reactive data binding in MVVM applications.

**Solution:** Use WPF/MAUI extensions for ObservableCollection and property binding.

**Complete Example:** MVVM app with ObservableCollection

```csharp
using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using System.Collections.ObjectModel;

class TodoViewModel : INotifyPropertyChanged
{
    private readonly ConvexClient _client;
    private IDisposable? _subscription;

    public TodoViewModel(ConvexClient client)
    {
        _client = client;
        Todos = new ObservableCollection<Todo>();
    }

    public ObservableCollection<Todo> Todos { get; }

    public void Start()
    {
        // Auto-sync ObservableCollection
        _subscription = _client.Observe<List<Todo>>("todos:list")
            .ObserveOnUI() // Marshal to UI thread
            .ToObservableCollection()
            .Subscribe(collection =>
            {
                Todos.Clear();
                foreach (var todo in collection)
                {
                    Todos.Add(todo);
                }
            });

        // Bind connection state to command
        _client.ConnectionStateChanges
            .Select(state => state == ConnectionState.Connected)
            .ObserveOnUI()
            .BindToCanExecute(SaveCommand); // Enable/disable command
    }

    public void Stop()
    {
        _subscription?.Dispose();
    }
}
```

**Key Points:**

- `ObserveOnUI()` ensures thread safety
- `ToObservableCollection()` for automatic updates
- `BindToProperty()` and `BindToCanExecute()` for reactive binding

**Learn more:** Install `Convex.Client.Extensions` package

### ASP.NET Core

**Problem:** You need server-side Convex client with dependency injection and health checks.

**Solution:** Use ASP.NET Core extensions for middleware and DI.

**Complete Example:** Server-side API with middleware

```csharp
// Program.cs
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Convex client with DI
builder.Services.AddConvex(options =>
{
    options.DeploymentUrl = builder.Configuration["Convex:DeploymentUrl"];
});

// Add ASP.NET Core middleware
builder.Services.AddConvexMiddleware();

// Add health checks
builder.Services.AddHealthChecks()
    .AddConvexHealthCheck();

var app = builder.Build();

// Use middleware
app.UseConvexMiddleware();

// Use health checks
app.MapHealthChecks("/health");

// Use in controllers
app.MapGet("/todos", async (IConvexClient client) =>
{
    var todos = await client.Query<List<Todo>>("todos:list")
        .ExecuteAsync();
    return Results.Ok(todos);
});

app.Run();
```

**Key Points:**

- Dependency injection for `IConvexClient`
- Middleware for authentication token handling
- Health checks for monitoring

**Learn more:** Install `Convex.Client.Extensions.AspNetCore` package

---

## 🔐 Authentication

### Token Management

**Problem:** You need secure authentication with token refresh.

**Solution:** Use token providers for automatic token management.

**Complete Example:** Auth flow with token refresh

```csharp
using Convex.Client;
using Convex.Client.Slices.Authentication;

class AuthService
{
    private readonly ConvexClient _client;
    private readonly IAuthenticationService _authService;

    public AuthService(ConvexClient client, IAuthenticationService authService)
    {
        _client = client;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        // Use token provider for automatic refresh
        var provider = new AuthTokenProvider(_authService);
        await _client.AuthenticationSlice.SetAuthTokenProviderAsync(provider);
    }

    // For simple apps or testing
    public async Task SetStaticTokenAsync(string token)
    {
        await _client.AuthenticationSlice.SetAuthTokenAsync(token);
    }
}

class AuthTokenProvider : IAuthTokenProvider
{
    private readonly IAuthenticationService _authService;

    public AuthTokenProvider(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Refresh token if needed
        return await _authService.GetValidTokenAsync();
    }
}
```

**Key Points:**

- Token provider pattern for automatic refresh
- Static token for simple apps/testing
- Admin auth for server-side only

**Learn more:** [API Reference - Authentication](docs/api-reference.md#authentication)

### Clerk Integration

**Problem:** You're using Clerk for authentication and want seamless integration.

**Solution:** Use Clerk extension packages for your platform.

**Complete Example:** Blazor app with Clerk

```bash
# Install Clerk Blazor package
dotnet add package Convex.Client.Extensions.Clerk.Blazor
```

```csharp
// Program.cs
using Convex.Client.Extensions.Clerk.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Add Convex with Clerk integration
builder.Services.AddConvexWithClerkForBlazor(
    builder.Configuration.GetSection("Clerk"),
    builder.Configuration.GetSection("Convex")
);

var app = builder.Build();
app.Run();
```

**Key Points:**

- Zero-configuration setup for Blazor WebAssembly
- Automatic JavaScript injection
- Token management handled automatically

**Learn more:** See [Clerk Extensions documentation](src/Convex.Client.Extensions.Clerk/README.md)

---

## 🛠️ Developer Tools

### Reactive Extensions (Rx)

**Problem:** You need advanced observable patterns like debouncing, retry, and circuit breakers.

**Solution:** Use Rx extension methods for common patterns.

**Complete Example:** Debounced search with retry

```csharp
using Convex.Client.Extensions.ExtensionMethods;
using System.Reactive.Subjects;

class SearchService
{
    private readonly ConvexClient _client;
    private readonly Subject<string> _searchText = new();

    public SearchService(ConvexClient client)
    {
        _client = client;
    }

    public void SetupSearch()
    {
        // Debounced search with retry
        _searchText
            .SmartDebounce(TimeSpan.FromMilliseconds(300))
            .SelectMany(term => _client.Observe<List<Product>, object>("products:search", new { term }))
            .RetryWithBackoff(maxRetries: 3, initialDelay: TimeSpan.FromSeconds(1))
            .Subscribe(results =>
            {
                UpdateSearchResults(results);
            });
    }

    public void OnSearchTextChanged(string text)
    {
        _searchText.OnNext(text);
    }
}
```

**Key Points:**

- `SmartDebounce()` preserves first and last values
- `RetryWithBackoff()` for automatic retry
- `ShareReplayLatest()` for multiple subscribers

**Learn more:** Install `Convex.Client.Extensions` package

### Error Handling

**Problem:** You need resilient subscriptions that handle network issues gracefully.

**Solution:** Use error handling patterns like circuit breakers and conditional retry.

**Complete Example:** Resilient subscription with circuit breaker

```csharp
using Convex.Client.Extensions.ExtensionMethods;

class ResilientDataService
{
    private readonly ConvexClient _client;

    public ResilientDataService(ConvexClient client)
    {
        _client = client;
    }

    public void SetupResilientSubscription()
    {
        // Resilient subscription with circuit breaker
        _client.Observe<List<Todo>>("todos:list")
            .RetryWhen(ex => ex is ConvexNetworkException, maxRetries: 5)
            .WithCircuitBreaker(
                failureThreshold: 5,
                recoveryTimeout: TimeSpan.FromMinutes(1))
            .CatchAndReport(
                () => new { Timestamp = DateTime.UtcNow },
                (ex, context) => Console.WriteLine($"Error at {context.Timestamp}: {ex.Message}"))
            .Subscribe(todos => UpdateUI(todos));
    }
}
```

**Key Points:**

- `RetryWhen()` for conditional retry
- `WithCircuitBreaker()` prevents cascading failures
- `CatchAndReport()` for error logging

**Learn more:** [API Reference - Extension Methods](docs/api-reference.md#extension-methods)

### Testing

**Problem:** You need to test code that uses Convex client without hitting the real backend.

**Solution:** Use testing utilities to create mock clients.

**Complete Example:** Unit test with mock client

```csharp
using Convex.Client.Extensions.ExtensionMethods;
using Xunit;

public class TodoServiceTests
{
    [Fact]
    public async Task GetAllTodos_ReturnsTodos()
    {
        // Create mock client
        var mockClient = ConvexTestingExtensions.CreateMockClient(builder =>
            builder.SetupQuery("todos:list", new List<Todo>
            {
                new Todo { Id = "1", Text = "Test todo" }
            }));

        var service = new TodoService(mockClient);
        var todos = await service.GetAllTodosAsync();

        Assert.Single(todos);
        Assert.Equal("Test todo", todos[0].Text);
    }

    [Fact]
    public async Task ObserveTodos_EmitsValues()
    {
        var mockClient = ConvexTestingExtensions.CreateMockClient(builder =>
            builder.SetupQuery("todos:list", new List<Todo>()));

        var recorder = mockClient.Observe<List<Todo>>("todos:list")
            .Record();

        // Wait for value
        await recorder.WaitForValue(todos => todos.Count > 0, timeout: TimeSpan.FromSeconds(5));

        Assert.True(recorder.Values.Count > 0);
    }
}
```

**Key Points:**

- `CreateMockClient()` for testing
- `Record()` to capture observable emissions
- `WaitForValue()` for async testing

**Learn more:** Install `Convex.Client.Extensions` package

---

## 📦 Available Packages

The Convex .NET SDK consists of several packages:

| Package                                          | Purpose                            | When to Use                                                                     |
| ------------------------------------------------ | ---------------------------------- | ------------------------------------------------------------------------------- |
| **Convex.Client**                                | Core client library                | Always required - includes real-time client, generators, and analyzers          |
| **Convex.Client.Extensions**                     | Helper utilities and Rx extensions | For Rx patterns, testing utilities, and UI framework integrations               |
| **Convex.Client.Extensions.DependencyInjection** | DI extensions                      | For dependency injection configuration in all .NET platforms                    |
| **Convex.Client.Extensions.Blazor**              | Blazor-specific extensions         | For Blazor WebAssembly/Server apps (StateHasChanged integration, form binding)  |
| **Convex.Client.Extensions.AspNetCore**          | ASP.NET Core middleware            | For ASP.NET Core server apps (middleware, health checks)                        |
| **Convex.Client.Extensions.Clerk**               | Clerk authentication (core)        | For Clerk auth in ASP.NET Core or console apps                                  |
| **Convex.Client.Extensions.Clerk.Blazor**        | Clerk auth for Blazor              | For Clerk auth in Blazor WebAssembly apps                                       |
| **Convex.Client.Extensions.Clerk.Godot**         | Clerk auth for Godot               | For Clerk auth in Godot desktop apps                                            |
| **Convex.Client.Analyzer**                       | Roslyn analyzers                   | Bundled in Convex.Client (also available separately for analyzer-only projects) |
| **Convex.Client.Analyzer.CodeFixes**             | Code fixes for analyzers           | Bundled in Convex.Client (also available separately for analyzer-only projects) |
| **Convex.Client.Attributes**                     | Attributes for code generation     | Included directly in Convex.Client (no separate package needed)                 |
| **Convex.FunctionGenerator**                     | Source generator                   | Bundled into Convex.Client (no separate package needed)                         |
| **Convex.SchemaGenerator**                       | Schema type generator              | Generate C# models from `schema.ts` (no separate package needed)                |

**Quick Start:** Most apps only need `Convex.Client`. Add extension packages as needed for your platform.

---

## 📚 Reference

### Quick API Reference

| Operation     | Method                           | Example                                                                           |
| ------------- | -------------------------------- | --------------------------------------------------------------------------------- |
| **Subscribe** | `Observe<T>()`                   | `client.Observe<List<Todo>>("todos:list")`                                        |
| **Query**     | `Query<T>().ExecuteAsync()`      | `await client.Query<List<Todo>>("todos:list").ExecuteAsync()`                     |
| **Mutate**    | `Mutate<T>().ExecuteAsync()`     | `await client.Mutate<Todo>("todos:create").WithArgs(new { text }).ExecuteAsync()` |
| **Action**    | `Action<T>().ExecuteAsync()`     | `await client.Action<string>("sendEmail").WithArgs(new { to }).ExecuteAsync()`    |
| **Batch**     | `Batch().Query().ExecuteAsync()` | `await client.Batch().Query<List<Todo>>("todos:list").ExecuteAsync<List<Todo>>()` |

### Common Patterns

**Connection Status:**

```csharp
client.ConnectionStateChanges.Subscribe(state => Console.WriteLine(state));
```

**Cached Values:**

```csharp
if (client.TryGetCachedValue<List<Todo>>("todos:list", out var todos))
{
    // Use cached data
}
```

**Query Dependencies:**

```csharp
client.DefineQueryDependency("todos:create", "todos:list", "todos:count");
```

### Documentation

- 📖 [Getting Started Guide](docs/getting-started.md) - Detailed walkthrough
- 📘 [API Reference](docs/api-reference.md) - Complete API documentation
- 🔧 [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions
- ⚙️ [Function Generator](src/Convex.FunctionGenerator/README.md) - Type-safe function constants from `api.d.ts`
- 🗂️ [Schema Generator](src/Convex.SchemaGenerator/README.md) - Type-safe C# models from `schema.ts`

### Troubleshooting

**Connection Failed?**

- Check deployment URL format: `https://your-app.convex.cloud`
- Verify deployment is running: `npx convex deployments`

**Function Not Found?**

- Use function path format matching your file structure: `"functions/list"` (from `convex/functions/list.ts`)
- Function names match file paths: `convex/functions/createTodo.ts` → `"functions/createTodo"`

**Source Generator Issues?**

- See [Function Generator documentation](src/Convex.FunctionGenerator/README.md)
- Constants not generating? Check `api.d.ts` path in `.csproj`
- Wrong function type? Constant value is still correct, just categorized differently

**Schema Generator Issues?**

- See [Schema Generator documentation](src/Convex.SchemaGenerator/README.md)
- Models not generating? Check `schema.ts` path in `.csproj`
- Parse error? Check for syntax errors in `schema.ts` or unsupported TypeScript features

**More help:** See [Troubleshooting Guide](docs/troubleshooting.md)

---

## 📚 Examples

Complete working examples are available in the `examples/` directory:

- **RealTimeChat** - Full-featured real-time chat application demonstrating:

  - **Backend**: TypeScript Convex functions in `backend/convex/functions/`
  - **Blazor WebAssembly**: Frontend with DI setup (`AddConvex()`) and source generator integration
  - **WPF**: Desktop application with `ChatConfiguration` and `CreateClient()`
  - **Godot**: Game engine integration with `ConvexManager` singleton and `CreateClientBuilder()`
  - **Shared**: Common models and services (`RealtimeChat.Shared`) used across platforms
  - Real-time subscriptions, file uploads, and presence

- **RealTimeChatClerk** - Same chat application with Clerk authentication integration:
  - **Backend**: Same TypeScript functions with Clerk auth configuration
  - **Blazor WebAssembly**: Clerk integration with `AddConvexWithClerkForBlazor()`
  - **Godot**: Desktop app with browser-based OAuth flow
  - Token management and caching

**Project Structure:**

```
examples/RealTimeChat/
├── backend/
│   └── convex/
│       ├── _generated/
│       │   └── api.d.ts          ← Generated by Convex
│       └── functions/
│           ├── getMessages.ts   ← TypeScript functions
│           └── sendMessage.ts
├── BlazorRealtimeChat/
│   └── frontend/
│       └── RealtimeChat.Frontend.csproj  ← References api.d.ts
├── RealtimeChat.Shared/          ← Shared models and services
└── appsettings.json               ← Configuration
```

Both examples demonstrate the complete workflow: TypeScript backend → `api.d.ts` → C# source generator → type-safe constants.

---

## 🤝 Contributing

We welcome contributions!

### Getting Started

```bash
git clone https://github.com/zakstam/convex-dotnet-unofficial.git
cd convex-dotnet-unofficial
dotnet build
dotnet test
```

### Ways to Contribute

- 🐛 [Report bugs](https://github.com/zakstam/convex-dotnet-unofficial/issues/new?labels=bug)
- 💡 [Request features](https://github.com/zakstam/convex-dotnet-unofficial/issues/new?labels=enhancement)
- 📝 Improve documentation
- 🔧 Submit pull requests
- ✅ Write tests

### Guidelines

- Follow existing code style
- Add tests for new features
- Update documentation
- Use [Conventional Commits](https://www.conventionalcommits.org/)
- Ensure compatibility tests pass

## 💬 Support

Need help?

| Channel              | Link                                                                        |
| -------------------- | --------------------------------------------------------------------------- |
| 📖 Documentation     | [docs.convex.dev](https://docs.convex.dev)                                  |
| 💬 Discord Community | [convex.dev/community](https://convex.dev/community)                        |
| 🐛 Issue Tracker     | [GitHub Issues](https://github.com/zakstam/convex-dotnet-unofficial/issues) |

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**✅ ALPHA Community Project**

This is a **community-driven Convex client** for .NET developers.

Built with ❤️ by the community • Maintained by contributors • Not affiliated with Convex, Inc.

[⭐ Star us on GitHub](https://github.com/zakstam/convex-dotnet-unofficial) | [📦 View on NuGet](https://www.nuget.org/packages/Convex.Client/)

</div>
