# Convex.Client Features

This directory contains **feature modules** organized by domain - self-contained capabilities grouped into logical modules.

## Module-Based Architecture

Features are organized into **5 core modules**:

```
Features/
├── DataAccess/        # CRUD operations (Queries, Mutations, Actions, Caching)
├── RealTime/          # Live data (Subscriptions, Pagination)
├── Security/          # Authentication & authorization
├── Storage/           # Files & vector search
└── Observability/     # Health, diagnostics, resilience
```

## What is a Feature Module?

A **feature module** groups related capabilities that:
- Share a common domain or purpose
- Work together to provide cohesive functionality
- Can be understood and maintained as a unit
- Have clear boundaries with other modules

Each **feature** within a module:
- Contains all code needed for that capability
- Depends ONLY on `Infrastructure/` (never other features)
- Can be tested in complete isolation
- Includes its own README with documentation

## Architecture Rules

### ✅ DO

1. **Organize features by module**
   ```
   Features/
   ├── DataAccess/
   │   ├── Queries/
   │   │   ├── QueriesSlice.cs      ← Entry point
   │   │   ├── QueryBuilder.cs      ← Feature-specific builders
   │   │   ├── BatchQueryBuilder.cs ← Internal implementation
   │   │   └── README.md            ← Documentation
   │   ├── Mutations/
   │   └── Actions/
   ```

2. **Depend only on Infrastructure/**
   ```csharp
   using Convex.Client.Infrastructure.Http;           // ✅ OK
   using Convex.Client.Infrastructure.Serialization;  // ✅ OK
   using Convex.Client.Infrastructure.ErrorHandling;  // ✅ OK
   ```

3. **Coordinate through ConvexClient facade**
   ```csharp
   // In feature
   public void SetCacheInvalidator(Action<string> invalidator)
   {
       _cacheInvalidator = invalidator;
   }

   // In ConvexClient.cs
   _mutations.SetCacheInvalidator((pattern) => _caching.Invalidate(pattern));
   ```

4. **Document ownership and API**
   - Every feature must have a README.md
   - Document public API and dependencies
   - Include usage examples

### ❌ DON'T

1. **NO feature-to-feature dependencies** (enforced by Roslyn analyzer)
   ```csharp
   using Convex.Client.Features.DataAccess.Mutations;  // ❌ FORBIDDEN

   class QueriesSlice
   {
       private readonly MutationsSlice _mutations;  // ❌ COMPILE ERROR
   }
   ```

2. **NO direct feature-to-feature calls**
   ```csharp
   _mutationsSlice.Execute();  // ❌ Must go through ConvexClient facade
   ```

3. **NO business logic in Infrastructure/**
   - Infrastructure is for technical utilities only
   - If 3+ features need it → extract to Infrastructure
   - If <3 features need it → keep in features (duplication OK)

## Dependency Rule

**Rule:** `Features → Infrastructure → Nothing`

```
┌─────────────┐
│  Features   │ ← Can depend on Infrastructure
└──────┬──────┘
       │
       ▼
┌─────────────┐
│Infrastructure│ ← Cannot depend on Features
└─────────────┘
```

**Enforced by:**
- Roslyn analyzer (compile-time error)
- Architecture unit tests (CI/CD)

## Module Overview

### DataAccess Module
**Purpose**: Synchronous data operations (CRUD)

| Feature | Purpose | Status |
|---------|---------|--------|
| **Queries** | Read-only function execution | ✅ Complete |
| **Mutations** | Write operations with cache invalidation | ✅ Complete |
| **Actions** | Server-side execution | ✅ Complete |
| **Caching** | Query result caching | ✅ Complete |

### RealTime Module
**Purpose**: Real-time data synchronization

| Feature | Purpose | Status |
|---------|---------|--------|
| **Subscriptions** | WebSocket-based live updates | ✅ Complete |
| **Pagination** | Cursor-based pagination | ✅ Complete |

### Security Module
**Purpose**: Authentication and authorization

| Feature | Purpose | Status |
|---------|---------|--------|
| **Authentication** | Token management and auth state | ✅ Complete |

### Storage Module
**Purpose**: File and vector storage

| Feature | Purpose | Status |
|---------|---------|--------|
| **Files** | File upload/download | ✅ Complete |
| **VectorSearch** | Vector similarity search | ✅ Complete |

### Observability Module
**Purpose**: Monitoring and diagnostics

| Feature | Purpose | Status |
|---------|---------|--------|
| **Health** | Connection health monitoring | ✅ Complete |
| **Diagnostics** | Telemetry and debugging | ✅ Complete |
| **Resilience** | Retry and circuit breaker | ✅ Complete |

## Creating a New Feature

### Step 1: Choose the Right Module

Determine which module your feature belongs to:
- **Data operations?** → `DataAccess/`
- **Real-time sync?** → `RealTime/`
- **Auth/security?** → `Security/`
- **File/vector storage?** → `Storage/`
- **Monitoring/ops?** → `Observability/`

### Step 2: Create Folder Structure

```bash
mkdir Features/[Module]/[FeatureName]
cd Features/[Module]/[FeatureName]
```

### Step 3: Create Entry Point

```csharp
// Features/[Module]/[FeatureName]/[FeatureName]Slice.cs
namespace Convex.Client.Features.[Module].[FeatureName];

public class [FeatureName]Slice
{
    private readonly IHttpClientProvider _httpProvider;
    private readonly IConvexSerializer _serializer;

    public [FeatureName]Slice(
        IHttpClientProvider httpProvider,
        IConvexSerializer serializer)
    {
        _httpProvider = httpProvider;
        _serializer = serializer;
    }

    public I[FeatureName]Builder CreateFeature(string functionName)
    {
        return new [FeatureName]Builder(
            _httpProvider,
            _serializer,
            functionName
        );
    }
}
```

### Step 4: Create README.md

```markdown
# [FeatureName] Feature

## Purpose
Brief description of what this feature does.

## Module
[Module Name]

## Public API

\`\`\`csharp
// Entry point
public I[FeatureName]Builder CreateFeature(string functionName)

// Builder methods
.WithArgs<TArgs>(TArgs args)
.ExecuteAsync()
\`\`\`

## Dependencies
- `Infrastructure/Http/IHttpClientProvider`
- `Infrastructure/Serialization/IConvexSerializer`

## Usage Example

\`\`\`csharp
var result = await client.[FeatureName]("function:name")
    .WithArgs(new { param = "value" })
    .ExecuteAsync();
\`\`\`

## Testing
See `tests/Convex.Client.Tests.Unit/[FeatureName]SliceTests.cs`
```

### Step 5: Register in ConvexClient

```csharp
// ConvexClient.cs
private readonly [FeatureName]Slice _featureName;

public ConvexClient(...)
{
    _featureName = new [FeatureName]Slice(httpProvider, serializer);
}

public I[FeatureName]Builder [FeatureName](string functionName)
    => _featureName.CreateFeature(functionName);
```

### Step 6: Run Validation

```bash
# Check for dependency violations
dotnet build  # Roslyn analyzer will fail if violations exist

# Run architecture tests
dotnet test --filter "FullyQualifiedName~ArchitectureTests"
```

## Feature Checklist

When creating a new feature, ensure:

- [ ] Module selected: `Features/[Module]/`
- [ ] Folder created: `Features/[Module]/[FeatureName]/`
- [ ] Entry point created: `[FeatureName]Slice.cs`
- [ ] Depends only on `Infrastructure/*` (no feature-to-feature)
- [ ] README.md created with:
  - [ ] Purpose and responsibilities
  - [ ] Module classification
  - [ ] Public API documentation
  - [ ] Infrastructure dependencies listed
  - [ ] Usage examples
  - [ ] Testing guidance
- [ ] Registered in `ConvexClient.cs` facade
- [ ] Unit tests created
- [ ] Architecture tests pass
- [ ] Build succeeds (no Roslyn violations)

## Cross-Feature Coordination

When features need to coordinate:

### Pattern 1: Callbacks via Facade

```csharp
// In feature
public void SetOnMutationComplete(Func<Task> callback)
{
    _onMutationComplete = callback;
}

// In ConvexClient
_mutations.SetOnMutationComplete(async () =>
{
    await _caching.InvalidateAllAsync();
});
```

### Pattern 2: Events via Facade

```csharp
// In feature
public event EventHandler<DataChangedEventArgs>? DataChanged;

// In ConvexClient
_subscriptions.DataChanged += (sender, e) =>
{
    _caching.Invalidate(e.Pattern);
};
```

### Pattern 3: Shared State in Facade

```csharp
// ConvexClient owns shared state
private readonly QueryCache _cache;

// Features access through injection
public IQueryBuilder<TResult> Query<TResult>(string functionName)
{
    var builder = _queries.Query<TResult>(functionName);
    builder.SetCache(_cache);  // Inject shared state
    return builder;
}
```

## Benefits of Module Organization

✅ **Discoverability** - Related features are grouped together
✅ **Maintainability** - Clear boundaries and responsibilities
✅ **Scalability** - Easy to add features to existing modules
✅ **Team Productivity** - Multiple developers can work on different modules
✅ **Documentation** - Module-level docs provide context
✅ **Onboarding** - New developers can understand one module at a time

## Questions?

- **Architecture questions** → GitHub Discussions or architecture review
- **Feature-specific questions** → See feature's README.md
- **Build/analyzer issues** → Check `Convex.Client.Analyzer` project
- **General guidance** → See [CLAUDE.md](../../CLAUDE.md)

## References

- **Architecture Overview:** [docs/architecture/VERTICAL_SLICE_ARCHITECTURE.md](../../../docs/architecture/VERTICAL_SLICE_ARCHITECTURE.md)
- **Infrastructure:** [../Infrastructure/README.md](../Infrastructure/README.md)
- **Architecture Tests:** `tests/Convex.Client.ArchitectureTests/`
- **Roslyn Analyzer:** `src/Convex.Client.Analyzer/`
