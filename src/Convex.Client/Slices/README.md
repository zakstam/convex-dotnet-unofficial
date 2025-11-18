# Convex.Client Slices

This directory contains **vertical slices** - self-contained features that can be owned and developed independently by a single developer.

## What is a Vertical Slice?

A vertical slice is a complete feature implementation that:
- Contains all code needed for that feature (entry point, builders, implementation)
- Depends ONLY on `Shared/` infrastructure (never other slices)
- Can be tested in complete isolation
- Has a single developer owner
- Includes its own README with documentation

## Slice Architecture Rules

### ✅ DO

1. **Create self-contained slices**
   ```
   Slices/MyFeature/
   ├── MyFeatureSlice.cs    ← Entry point
   ├── Builder.cs           ← Feature-specific builders
   ├── Implementation.cs    ← Internal implementation
   └── README.md            ← Documentation + owner
   ```

2. **Depend only on Shared/ infrastructure**
   ```csharp
   using Convex.Client.Shared.Http;           // ✅ OK
   using Convex.Client.Shared.Serialization;  // ✅ OK
   using Convex.Client.Shared.ErrorHandling;  // ✅ OK
   ```

3. **Coordinate through ConvexClient facade**
   ```csharp
   // In slice
   public void SetCacheInvalidator(Action<string> invalidator)
   {
       _cacheInvalidator = invalidator;
   }

   // In ConvexClient.cs
   _mutations.SetCacheInvalidator((pattern) => _caching.Invalidate(pattern));
   ```

4. **Document ownership**
   - Every slice must have a README.md
   - Must specify owner name
   - Must document public API and dependencies

### ❌ DON'T

1. **NO slice-to-slice dependencies** (enforced by Roslyn analyzer)
   ```csharp
   using Convex.Client.Slices.Mutations;  // ❌ FORBIDDEN

   class QuerySlice
   {
       private readonly MutationSlice _mutations;  // ❌ COMPILE ERROR
   }
   ```

2. **NO direct slice-to-slice calls**
   ```csharp
   _mutationSlice.Execute();  // ❌ Must go through ConvexClient facade
   ```

3. **NO business logic in Shared/**
   - Shared is for technical infrastructure only
   - If 3+ slices need it → extract to Shared
   - If <3 slices need it → keep in slices (duplication OK)

## Dependency Rule

**Rule:** `Slices → Shared → Nothing`

```
┌─────────────┐
│   Slices    │ ← Can depend on Shared
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Shared    │ ← Cannot depend on Slices
└─────────────┘
```

**Enforced by:**
- Roslyn analyzer (compile-time error)
- Architecture unit tests (CI/CD)

## Current Slices

| Slice | Purpose | Owner | Status |
|-------|---------|-------|--------|
| **Queries** | Read-only Convex function execution | TBD | 🔄 Planned |
| **Mutations** | Write operations with cache invalidation | TBD | 🔄 Planned |
| **Actions** | Arbitrary server-side execution | TBD | 🔄 Planned |
| **Subscriptions** | Real-time WebSocket updates | TBD | 🔄 Planned |
| **FileStorage** | File upload/download | TBD | 🔄 Planned |
| **VectorSearch** | Vector similarity search | TBD | 🔄 Planned |
| **HttpActions** | HTTP API routing | TBD | 🔄 Planned |
| **Scheduling** | Delayed/recurring execution | TBD | 🔄 Planned |
| **Pagination** | Cursor-based pagination | TBD | 🔄 Planned |
| **Caching** | Query result caching | TBD | 🔄 Planned |
| **Authentication** | Auth token management | TBD | 🔄 Planned |
| **Health** | Connection health monitoring | TBD | 🔄 Planned |
| **Diagnostics** | Telemetry and debugging | TBD | 🔄 Planned |
| **Resilience** | Retry and circuit breaker | TBD | 🔄 Planned |

## Creating a New Slice

### Step 1: Create Folder Structure

```bash
mkdir Slices/MyFeature
cd Slices/MyFeature
```

### Step 2: Create Entry Point

```csharp
// MyFeatureSlice.cs
namespace Convex.Client.Slices.MyFeature;

public class MyFeatureSlice
{
    private readonly IHttpClientProvider _httpProvider;
    private readonly IConvexSerializer _serializer;

    public MyFeatureSlice(
        IHttpClientProvider httpProvider,
        IConvexSerializer serializer)
    {
        _httpProvider = httpProvider;
        _serializer = serializer;
    }

    public IMyFeatureBuilder MyFeature(string functionName)
    {
        return new MyFeatureBuilder(_httpProvider, _serializer, functionName);
    }
}
```

### Step 3: Create README.md

```markdown
# MyFeature Slice

## Purpose
Brief description of what this feature does.

## Owner
- **Name:** Your Name
- **Contact:** your.email@example.com

## Public API

\`\`\`csharp
// Entry point
public IMyFeatureBuilder MyFeature(string functionName)

// Builder methods
.WithArgs<TArgs>(TArgs args)
.ExecuteAsync()
\`\`\`

## Dependencies
- `Shared/Http/IHttpClientProvider`
- `Shared/Serialization/IConvexSerializer`

## Testing
See `tests/Slices/MyFeatureTests.cs`
```

### Step 4: Register in ConvexClient

```csharp
// ConvexClient.cs
private readonly MyFeatureSlice _myFeature;

public ConvexClient(...)
{
    _myFeature = new MyFeatureSlice(httpProvider, serializer);
}

public IMyFeatureBuilder MyFeature(string functionName)
    => _myFeature.MyFeature(functionName);
```

### Step 5: Run Validation

```bash
# Check for dependency violations
dotnet build  # Roslyn analyzer will fail if violations exist

# Run architecture tests
dotnet test --filter "FullyQualifiedName~ArchitectureTests"
```

## Slice Checklist

When creating a new slice, ensure:

- [ ] Folder created: `Slices/[FeatureName]/`
- [ ] Entry point created: `[FeatureName]Slice.cs`
- [ ] Depends only on `Shared/*` (no slice-to-slice)
- [ ] README.md created with:
  - [ ] Purpose and responsibilities
  - [ ] Owner name and contact
  - [ ] Public API documentation
  - [ ] Shared dependencies listed
  - [ ] Testing guidance
- [ ] Registered in `ConvexClient.cs` facade
- [ ] Unit tests created
- [ ] Architecture tests pass
- [ ] Build succeeds (no Roslyn violations)

## Cross-Slice Coordination

When slices need to coordinate:

### Pattern 1: Callbacks via Facade

```csharp
// In slice
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
// In slice
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

// Slices access through properties/methods
public IQueryBuilder<TResult> Query<TResult>(string functionName)
{
    var builder = _queries.Query<TResult>(functionName);
    builder.SetCache(_cache);  // Inject shared state
    return builder;
}
```

## Migration Status

**Current Phase:** Foundation (Weeks 1-2)
**Target:** 14 weeks for complete migration

See [docs/VERTICAL_SLICE_ARCHITECTURE.md](../../docs/VERTICAL_SLICE_ARCHITECTURE.md) for detailed migration plan.

## Questions?

- **Architecture questions** → GitHub Discussions or architecture review meeting
- **Slice-specific questions** → Contact slice owner
- **Build/analyzer issues** → Check `Convex.Analyzers` project
- **General guidance** → See [CLAUDE.md](../../CLAUDE.md)

## References

- **Architecture Overview:** [CLAUDE.md](../../CLAUDE.md)
- **Detailed Design Doc:** [docs/VERTICAL_SLICE_ARCHITECTURE.md](../../docs/VERTICAL_SLICE_ARCHITECTURE.md)
- **Shared Infrastructure:** [Shared/README.md](../Shared/README.md)
- **Architecture Tests:** `tests/ArchitectureTests/`
- **Roslyn Analyzer:** `analyzers/Convex.Analyzers/SliceDependencyAnalyzer.cs`
