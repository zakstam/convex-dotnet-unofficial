# Convex.FunctionGenerator

Roslyn source generator that creates type-safe constants for Convex function names from your TypeScript backend.

## Overview

This source generator automatically generates C# constants from your Convex backend's `api.d.ts` file, providing compile-time type safety for function names and preventing runtime errors from typos or refactoring mistakes.

## Features

- ✅ **Automatic Code Generation**: Parses `api.d.ts` and generates type-safe constants
- ✅ **IntelliSense Support**: Full autocomplete for function names
- ✅ **Compile-Time Validation**: Typos caught at build time, not runtime
- ✅ **Organized by Type**: Constants grouped as Queries, Mutations, and Actions
- ✅ **Zero Configuration**: Works automatically during build
- ✅ **Roslyn Analyzer**: CVX004 warns when using raw strings instead of constants
- ✅ **Code Fix Provider**: Automatically converts string literals to constants

## Installation

### 1. Add Package Reference

```xml
<ItemGroup>
  <!-- Source generator for type-safe function constants -->
  <ProjectReference Include="..\..\src\Convex.FunctionGenerator\Convex.FunctionGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 2. Point to Your Backend

```xml
<ItemGroup>
  <!-- Point to the Convex backend api.d.ts for code generation -->
  <AdditionalFiles Include="../backend/convex/_generated/api.d.ts" />
</ItemGroup>
```

## Usage

### Before (Unsafe)
```csharp
var response = await _client.QueryAsync<GetMessagesResponse, GetMessagesArgs>(
    "functions/getMessages",  // ❌ Magic string - typos = runtime errors
    new GetMessagesArgs { Limit = 50 }
);
```

### After (Type-Safe)
```csharp
using Convex.Generated;

var response = await _client.QueryAsync<GetMessagesResponse, GetMessagesArgs>(
    ConvexFunctions.Queries.GetMessages,  // ✅ IntelliSense + compile-time validation
    new GetMessagesArgs { Limit = 50 }
);
```

## Generated Code

The generator creates a static class with constants organized by function type:

```csharp
namespace Convex.Generated
{
    public static class ConvexFunctions
    {
        public static class Queries
        {
            /// <summary>Query: functions/getMessages</summary>
            public const string GetMessages = "functions/getMessages";

            /// <summary>Query: functions/getOnlineUsers</summary>
            public const string GetOnlineUsers = "functions/getOnlineUsers";

            // ... other queries
        }

        public static class Mutations
        {
            /// <summary>Mutation: functions/sendMessage</summary>
            public const string SendMessage = "functions/sendMessage";

            /// <summary>Mutation: functions/deleteMessage</summary>
            public const string DeleteMessage = "functions/deleteMessage";

            // ... other mutations
        }

        public static class Actions
        {
            // ... actions
        }
    }
}
```

## Analyzer & Code Fixes

### CVX004: Use Type-Safe Function Names

The `FunctionNameAnalyzer` detects raw string literals for function names:

**Warning:**
```csharp
// CVX004: Use 'ConvexFunctions.Queries.GetMessages' instead of string literal 'functions/getMessages'
var response = await _client.QueryAsync<T, TArgs>("functions/getMessages", args);
```

**Fix (Automatic):**
The code fix provider can automatically replace the string literal:
1. Place cursor on the string literal
2. Press `Ctrl+.` (Quick Actions and Refactorings)
3. Select "Use ConvexFunctions.Queries.GetMessages"

## How It Works

### 1. Parse TypeScript
The generator reads your `api.d.ts` file and extracts function definitions:

```typescript
declare const fullApi: ApiFromModules<{
  "functions/getMessages": typeof functions_getMessages;
  "functions/sendMessage": typeof functions_sendMessage;
  // ...
}>;
```

### 2. Infer Function Types
Function types are inferred from naming patterns:
- `get*`, `list*`, `search*` → **Queries**
- `send*`, `create*`, `update*`, `delete*`, `edit*`, `toggle*`, `set*` → **Mutations**
- Everything else → **Actions**

### 3. Generate Constants
For each function, a constant is generated with:
- **PascalCase name** (e.g., `getMessages` → `GetMessages`)
- **Proper pluralization** (e.g., `Query` → `Queries`)
- **XML documentation** with the full function path

## Configuration

The generator runs automatically during build. No configuration needed!

### Optional: Emit Generated Files

To see the generated code (for debugging):

```bash
dotnet build -p:EmitCompilerGeneratedFiles=true
```

Generated files will be in: `obj/Debug/generated/Convex.FunctionGenerator/`

## Migration Guide

### Step 1: Add Package References
Add the source generator and analyzer to your project (see Installation above).

### Step 2: Add Using Directive
```csharp
using Convex.Generated;
```

### Step 3: Replace String Literals

**Manual:**
```csharp
// Before
"functions/getMessages"

// After
ConvexFunctions.Queries.GetMessages
```

**Automatic (with Code Fix):**
1. Build your project to see CVX004 warnings
2. Use Quick Actions (`Ctrl+.`) on each warning
3. Select the suggested fix

### Step 4: Build and Verify
```bash
dotnet build
```

All CVX004 warnings should be resolved!

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

## Troubleshooting

### Constants Not Generating
1. Verify `api.d.ts` path in `<AdditionalFiles>`
2. Check that the file exists
3. Rebuild project: `dotnet clean && dotnet build`

### Wrong Function Type
The generator infers types from naming patterns. If a function is categorized incorrectly, check the naming:
- Queries: Start with `get`, `list`, or `search`
- Mutations: Start with `send`, `create`, `update`, `delete`, `edit`, `toggle`, or `set`
- Actions: Everything else

### Analyzer Not Running
1. Ensure `Convex.Client.Analyzer` is referenced
2. Check IDE analyzer settings (VS/Rider/VSCode)
3. Rebuild solution

## Examples

See the [GodotRealtimeChat example](../../examples/GodotRealtimeChat) for a complete working implementation.

## License

MIT License - see [LICENSE](../../LICENSE) for details.
