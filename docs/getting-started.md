# Getting Started Guide

This guide walks you through building your first real-time app with Convex + .NET.

## Prerequisites

- .NET 8.0+ SDK installed
- Node.js 18+ and npm
- A Convex account ([sign up here](https://convex.dev))
- Basic knowledge of C# and TypeScript

## Step-by-Step Guide

### Step 1: Create Your Convex Backend

Write your backend functions in TypeScript (Convex's native language). Each function should be in its own file:

```typescript
// convex/functions/list.ts
import { query } from "./_generated/server";

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

**Important:** Function names match file paths. `convex/functions/createTodo.ts` becomes `"functions/createTodo"` in C#.

### Step 2: Deploy Your Backend

```bash
# Navigate to your backend directory
cd backend

# Install dependencies (if needed)
npm install

# Deploy to Convex
npx convex dev

# This automatically generates convex/_generated/api.d.ts
```

Convex automatically generates `convex/_generated/api.d.ts` with your function definitions. This file contains the API schema that the C# client uses.

### Step 3: Install Convex.Client

```bash
dotnet add package Convex.Client
```

That's it - one package includes everything: real-time client, analyzers, and all features.

### Step 4: (Optional) Generate Type-Safe Constants

For type-safe function names, point the source generator to your `api.d.ts` file:

```xml
<!-- In your .csproj -->
<ItemGroup>
  <AdditionalFiles Include="../backend/convex/_generated/api.d.ts" />
</ItemGroup>
```

Build your project - the generator creates C# constants from your TypeScript functions:

```csharp
// Auto-generated in obj/Debug/generated/Convex.SourceGenerator/ConvexFunctions.g.cs
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

**Note:** This is optional. You can also use string literals directly. Function names match file paths: `convex/functions/createTodo.ts` → `"functions/createTodo"`.

### Step 5: Define Your C# Data Models

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

**Important:** Your C# models should match the structure returned by your Convex functions. Use `string` for Convex IDs, `double` for numbers (JSON numbers are doubles), and nullable types for optional fields.

### Step 6: Use Real-Time Client in Your App

Connect and subscribe to live data that updates automatically:

```csharp
using Convex.Client;

// Create Convex client (simple)
var client = new ConvexClient("https://your-deployment.convex.cloud");

// Or use builder for advanced configuration:
// var client = new ConvexClientBuilder()
//     .UseDeployment("https://your-deployment.convex.cloud")
//     .WithAutoReconnect(maxAttempts: 5)
//     .WithTimeout(TimeSpan.FromSeconds(30))
//     .Build();

// Subscribe to live todos - updates automatically when data changes!
client.Observe<List<Todo>>("functions/list")
    // Or: ConvexFunctions.Queries.List (if using generator)
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

// Create a new todo
await client.Mutate<Todo>("functions/createTodo")
    // Or: ConvexFunctions.Mutations.CreateTodo (if using generator)
    .WithArgs(new { text = "Learn Convex .NET" })
    .ExecuteAsync();

Console.WriteLine("Watching for changes... Press any key to exit");
Console.ReadKey();
client.Dispose(); // Clean up when done - disposes WebSocket connection and resources
```

**That's it!** You now have a real-time reactive app. Any changes from other clients appear instantly.

## Complete Example

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

## Understanding the Workflow

1. **Write TypeScript functions** in Convex backend (standard Convex workflow)
2. **Convex generates `api.d.ts`** automatically when you run `npx convex dev`
3. **Source generator (optional)** reads `api.d.ts` and generates C# constants for type-safe function names
4. **Use C# client** to call functions by string name or generated constants
5. **Real-time subscriptions** automatically sync data changes

## Next Steps

- Learn about [Core Features](../README.md#core-features) - Real-time subscriptions, queries, mutations
- Explore [Advanced Features](../README.md#advanced-features) - File storage, vector search, scheduling
- Check out [UI Integrations](../README.md#ui-integrations) - Blazor, WPF/MAUI examples
- See [Complete Examples](../README.md#examples) - Full working applications
- Learn about [Source Generator](../README.md#source-generator) - Type-safe function constants

## Troubleshooting

Having issues? Check the [Troubleshooting Guide](troubleshooting.md) for common problems and solutions.
