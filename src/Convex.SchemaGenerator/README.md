# Convex.SchemaGenerator

Roslyn source generator that creates type-safe C# classes from your Convex `schema.ts` file.

## Overview

This source generator automatically generates C# record classes from your Convex backend's `schema.ts` file, providing type-safe document models that match your database schema exactly.

## Features

- **Automatic Code Generation**: Parses `schema.ts` and generates matching C# classes
- **Full Type Support**: Handles all Convex validator types including nested objects, arrays, unions, and records
- **System Fields Included**: Automatically adds `_id` and `_creationTime` fields to table types
- **JSON Serialization Ready**: Classes include `JsonPropertyName` attributes for proper serialization
- **Init-Only Properties**: Generated classes use `init` properties for immutability
- **Nested Type Support**: Complex nested objects are extracted into separate classes
- **Zero Configuration**: Works automatically during build

## Installation

### 1. Add Package Reference

```xml
<ItemGroup>
  <!-- Source generator for type-safe schema models -->
  <ProjectReference Include="..\..\src\Convex.SchemaGenerator\Convex.SchemaGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 2. Point to Your Schema

```xml
<ItemGroup>
  <!-- Point to the Convex backend schema.ts for code generation -->
  <AdditionalFiles Include="../backend/convex/schema.ts" />
</ItemGroup>
```

### 3. (Optional) Configure Namespace

```xml
<PropertyGroup>
  <!-- Customize the generated namespace (default: Convex.Generated.Models) -->
  <ConvexGeneratedNamespace>MyApp.Models</ConvexGeneratedNamespace>
</PropertyGroup>
```

## Usage

### Before (Manual Types)
```csharp
// Manual class definition - can get out of sync with schema
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // Did we forget a field? Is the type correct?
}
```

### After (Generated Types)
```csharp
using Convex.Generated.Models;

// Type-safe, always in sync with schema.ts
var users = await client.QueryAsync<List<Users>>("users:list");
Console.WriteLine($"User: {users[0].Name}, Email: {users[0].Email}");
```

## Type Mapping

The generator maps Convex validators to C# types:

| Convex Validator | C# Type |
|-----------------|---------|
| `v.string()` | `string` |
| `v.number()` | `double` |
| `v.float64()` | `double` |
| `v.int64()` | `long` |
| `v.boolean()` | `bool` |
| `v.bytes()` | `byte[]` |
| `v.null()` | `object` |
| `v.any()` | `System.Text.Json.JsonElement` |
| `v.id("tableName")` | `string` |
| `v.literal(value)` | Inferred (`string`, `long`, `bool`, `double`) |
| `v.optional(T)` | `T?` |
| `v.array(T)` | `List<T>` |
| `v.object({...})` | Nested class |
| `v.union(T1, T2)` | `object` (or `T?` for `T | null`) |
| `v.record(K, V)` | `Dictionary<K, V>` |

## Generated Code Example

Given this `schema.ts`:

```typescript
import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  users: defineTable({
    name: v.string(),
    email: v.string(),
    age: v.optional(v.number()),
    settings: v.object({
      theme: v.string(),
      notifications: v.boolean(),
    }),
  }).index("by_email", ["email"]),

  messages: defineTable({
    userId: v.id("users"),
    content: v.string(),
    reactions: v.array(v.object({
      emoji: v.string(),
      count: v.number(),
    })),
  }),
});
```

The generator produces:

```csharp
// Users.g.cs
namespace Convex.Generated.Models
{
    /// <summary>
    /// Document type for the 'users' table.
    /// </summary>
    public class Users
    {
        /// <summary>The document ID.</summary>
        [JsonPropertyName("_id")]
        public string Id { get; init; } = default!;

        /// <summary>The document creation time (Unix timestamp in milliseconds).</summary>
        [JsonPropertyName("_creationTime")]
        public double CreationTime { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = default!;

        [JsonPropertyName("email")]
        public string Email { get; init; } = default!;

        [JsonPropertyName("age")]
        public double? Age { get; init; }

        [JsonPropertyName("settings")]
        public UsersSettings Settings { get; init; } = default!;
    }

    /// <summary>
    /// Nested type for UsersSettings.
    /// </summary>
    public class UsersSettings
    {
        [JsonPropertyName("theme")]
        public string Theme { get; init; } = default!;

        [JsonPropertyName("notifications")]
        public bool Notifications { get; init; }
    }
}
```

```csharp
// Messages.g.cs
namespace Convex.Generated.Models
{
    /// <summary>
    /// Document type for the 'messages' table.
    /// </summary>
    public class Messages
    {
        /// <summary>The document ID.</summary>
        [JsonPropertyName("_id")]
        public string Id { get; init; } = default!;

        /// <summary>The document creation time (Unix timestamp in milliseconds).</summary>
        [JsonPropertyName("_creationTime")]
        public double CreationTime { get; init; }

        [JsonPropertyName("userId")]
        public string UserId { get; init; } = default!;

        [JsonPropertyName("content")]
        public string Content { get; init; } = default!;

        [JsonPropertyName("reactions")]
        public List<MessagesReactions> Reactions { get; init; } = default!;
    }

    /// <summary>
    /// Nested type for MessagesReactions.
    /// </summary>
    public class MessagesReactions
    {
        [JsonPropertyName("emoji")]
        public string Emoji { get; init; } = default!;

        [JsonPropertyName("count")]
        public double Count { get; init; }
    }
}
```

## How It Works

### 1. Parse Schema
The generator reads your `schema.ts` file and extracts table definitions using regex patterns to find `defineTable()` calls.

### 2. Extract Validators
Each field's validator is parsed recursively, handling nested objects, arrays, optionals, and other complex types.

### 3. Generate Classes
For each table, a C# class is generated with:
- **System fields**: `_id` and `_creationTime`
- **User fields**: All fields from the schema with proper types
- **Nested types**: Complex objects become separate classes
- **JSON attributes**: `JsonPropertyName` for serialization

### 4. Emit Polyfill
For .NET Standard 2.1 compatibility, an `IsExternalInit` polyfill is generated to enable `init` properties.

## Configuration

### Custom Namespace

By default, types are generated in `Convex.Generated.Models`. To customize:

```xml
<PropertyGroup>
  <ConvexGeneratedNamespace>MyCompany.MyApp.ConvexModels</ConvexGeneratedNamespace>
</PropertyGroup>
```

### View Generated Files

To see the generated code (for debugging):

```bash
dotnet build -p:EmitCompilerGeneratedFiles=true
```

Generated files will be in: `obj/Debug/generated/Convex.SchemaGenerator/`

## Diagnostics

### CVX101: Schema Parse Error

If the generator cannot parse your schema, you'll see this warning:

```
CVX101: Failed to parse schema.ts: [error details]
```

**Common causes:**
- Syntax errors in schema.ts
- Unsupported TypeScript features
- Complex expressions in validators

## Supported Schema Features

### Tables
- `defineTable({...})` with field definitions
- `.index("name", ["field1", "field2"])` (indexes are parsed but not used in generated code)

### Validators
- All primitive validators: `v.string()`, `v.number()`, `v.boolean()`, etc.
- `v.id("tableName")` for foreign key references
- `v.optional(T)` for optional fields
- `v.array(T)` for arrays
- `v.object({...})` for nested objects
- `v.union(T1, T2, ...)` for union types
- `v.record(K, V)` for dictionaries
- `v.literal(value)` for literal types

### Comments
Both single-line (`//`) and multi-line (`/* */`) comments are stripped before parsing.

## Limitations

- **Union types**: Complex unions (other than `T | null`) are mapped to `object`
- **Generic validators**: Custom validators are not supported
- **Schema imports**: External schema imports are not resolved

## Examples

See the [CursorPlayground example](../../examples/CursorPlayground) for a complete working implementation using generated schema types.

## License

MIT License - see [LICENSE](../../LICENSE) for details.
