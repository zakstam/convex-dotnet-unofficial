# Source Generator

The Convex .NET SDK includes a source generator that creates type-safe C# constants from your Convex backend's TypeScript functions.

## Overview

The `Convex.SourceGenerator` source generator reads your Convex backend's TypeScript function files and creates C# constants for function names. This provides:

- ✅ **Compile-time type safety** - Typos caught at build time
- ✅ **IntelliSense support** - Autocomplete for all function names
- ✅ **Refactoring safety** - Rename support across codebase
- ✅ **Zero runtime cost** - Constants are compile-time only

## How It Works

1. **Write TypeScript functions** in your Convex backend
2. **Source generator reads `.ts` files** during C# build
3. **C# constants are generated** automatically
4. **Use constants instead of strings** for function names

## Setup

### Step 1: Ensure Convex Backend is Set Up

Make sure your Convex backend has TypeScript function files:

```bash
cd your-backend-directory
npx convex dev
```

This starts the Convex development server and syncs your functions.

### Step 2: Point Generator to TypeScript Files

In your C# project file, add your TypeScript files as additional files:

```xml
<ItemGroup>
  <!-- Include your Convex function files (NOT api.d.ts - .d.ts files are excluded) -->
  <AdditionalFiles Include="../backend/convex/**/*.ts" Exclude="../backend/convex/_generated/**" />
</ItemGroup>
```

**Note:** Adjust the path to match your project structure. The generator excludes `.d.ts` files automatically.

### Step 3: Build Your Project

```bash
dotnet build
```

The generator runs automatically and creates constants in:
`obj/Debug/generated/Convex.SourceGenerator/ConvexFunctions.g.cs`

## Usage

### Before (Unsafe)

```csharp
// ❌ Magic string - typos = runtime errors
var todos = await client.Query<List<Todo>>("functions/list").ExecuteAsync();
await client.Mutate<Todo>("functions/createTodo").WithArgs(new { text = "..." }).ExecuteAsync();
```

### After (Type-Safe)

```csharp
using Convex.Generated;

// ✅ IntelliSense + compile-time validation
var todos = await client.Query<List<Todo>>(ConvexFunctions.Queries.List).ExecuteAsync();
await client.Mutate<Todo>(ConvexFunctions.Mutations.CreateTodo)
    .WithArgs(new { text = "..." })
    .ExecuteAsync();
```

## Generated Code Structure

The generator creates constants organized by function type:

```csharp
namespace Convex.Generated
{
    public static class ConvexFunctions
    {
        public static class Queries
        {
            /// <summary>Query: functions/list</summary>
            public const string List = "functions/list";

            /// <summary>Query: functions/getMessages</summary>
            public const string GetMessages = "functions/getMessages";
        }

        public static class Mutations
        {
            /// <summary>Mutation: functions/createTodo</summary>
            public const string CreateTodo = "functions/createTodo";

            /// <summary>Mutation: functions/sendMessage</summary>
            public const string SendMessage = "functions/sendMessage";
        }

        public static class Actions
        {
            /// <summary>Action: functions/sendEmail</summary>
            public const string SendEmail = "functions/sendEmail";
        }
    }
}
```

## Function Type Inference

The generator infers function types from naming patterns:

- **Queries**: Functions starting with `get`, `list`, or containing `search`
- **Mutations**: Functions starting with `send`, `create`, `update`, `delete`, `edit`, `toggle`, or `set`
- **Actions**: Everything else

**Important:** Function names match file paths. `convex/functions/getMessages.ts` becomes `"functions/getMessages"` in the constant.

If a function is categorized incorrectly, you can still use it - the constant value is correct regardless of the category.

## Analyzer Integration

The `Convex.Client.Analyzer` package includes analyzer **CVX004** that warns when you use raw strings instead of generated constants:

```csharp
// Warning: CVX004 - Use 'ConvexFunctions.Queries.GetMessages' instead of string literal
var messages = await client.Query<List<Message>>("functions/getMessages").ExecuteAsync();
```

Use Quick Actions (`Ctrl+.`) to automatically replace the string with the constant.

## Schema Model Generation

The generator also creates C# model classes from your `schema.ts` file.

### Setup

Include your schema file in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="../backend/convex/schema.ts" />
</ItemGroup>
```

### Generated Models

For a schema like:
```typescript
// schema.ts
export default defineSchema({
  notes: defineTable({
    title: v.string(),
    content: v.string(),
    tags: v.array(v.string()),
  }),
});
```

The generator creates:
```csharp
namespace Convex.Generated
{
    /// <summary>Document type for the 'notes' table.</summary>
    public class Note  // ← Singular form of table name
    {
        [JsonPropertyName("_id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("_creationTime")]
        public double CreationTime { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = default!;

        [JsonPropertyName("content")]
        public string Content { get; init; } = default!;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; init; } = default!;
    }
}
```

**Important:** Table names are **singularized** for class names:
- `notes` → `Note`
- `users` → `User`
- `messages` → `Message`

### Typed Document IDs

Enable strongly-typed IDs to prevent mixing up IDs from different tables:

```xml
<PropertyGroup>
  <ConvexGenerateTypedIds>true</ConvexGenerateTypedIds>
</PropertyGroup>
```

This generates:
```csharp
public class Note
{
    [JsonPropertyName("_id")]
    public NoteId Id { get; init; }  // ← Typed ID instead of string
}

public readonly record struct NoteId(string Value)
{
    public static implicit operator string(NoteId id) => id.Value;
    public static implicit operator NoteId(string value) => new(value);
}
```

## Troubleshooting

### Constants Not Generating

1. **Verify TypeScript files are included** - Check that the path in `<AdditionalFiles>` points to your actual `.ts` files (not `api.d.ts`)
2. **Check files exist** - Ensure your TypeScript function files exist at the specified path
3. **Rebuild project** - Run `dotnet clean && dotnet build`
4. **Check build output** - Look for generator messages in build output

### Wrong Function Type

The generator infers types from naming patterns. If a function is categorized incorrectly:

- The constant value is still correct (it's just in the wrong category)
- You can use it regardless of category
- Consider renaming your function to match the pattern, or ignore the category

### Generator Not Running

1. **Check package reference** - Ensure `Convex.Client` package is referenced (generator is bundled)
2. **Verify .NET version** - Generator requires .NET SDK that supports source generators
3. **Check build logs** - Look for generator errors in build output

## Viewing Generated Code

To see the generated code for debugging:

```bash
dotnet build -p:EmitCompilerGeneratedFiles=true
```

Generated files will be in: `obj/Debug/generated/Convex.SourceGenerator/`

## Benefits

### Compile-Time Safety

- ✅ Typos caught at build time
- ✅ Breaking changes detected immediately
- ✅ Safe refactoring with rename support

### Developer Experience

- ✅ IntelliSense autocomplete for all function names
- ✅ Navigate to definition (jumps to constant)
- ✅ Find all references across your codebase

### Maintenance

- ✅ Single source of truth (TypeScript backend)
- ✅ Automatic sync with backend changes
- ✅ No manual updates needed

## See Also

- [Getting Started Guide](getting-started.md)
- [API Reference](api-reference.md)
- [Troubleshooting Guide](troubleshooting.md)
