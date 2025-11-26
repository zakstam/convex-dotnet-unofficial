# Convex.Client Infrastructure

This directory contains **cross-cutting technical infrastructure** that all features can depend on.

## Purpose

Infrastructure provides common infrastructure that:
- Is **purely technical** (no business logic)
- Used by **3 or more features**
- Has **clear, stable interfaces**
- Can be **tested independently**

## What Belongs in Infrastructure?

### ✅ DO Put in Infrastructure

- **HTTP Transport:** Connection management, request/response handling
- **Serialization:** JSON serialization/deserialization contracts
- **Error Handling:** Exception types, error result models
- **Configuration:** Client configuration, options patterns
- **Common Types:** Value objects used across multiple features (e.g., ConvexNumber)
- **Utilities:** Pure functions with no dependencies (string formatting, validation)

### ❌ DON'T Put in Infrastructure

- **Business Logic:** Query execution, mutation processing, subscription management
- **Feature-Specific Code:** Pagination logic, caching strategies, auth workflows
- **One-Off Infrastructure:** Code used by only 1-2 features (keep in feature instead)
- **Feature Coordination:** Cross-feature communication (belongs in ConvexClient facade)

## Dependency Rules

**Rule:** `Infrastructure cannot depend on Features`

```
❌ FORBIDDEN:
┌─────────────┐
│   Infrastructure    │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Features    │
└─────────────┘

✅ ALLOWED:
┌─────────────┐
│   Features    │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Infrastructure    │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  .NET BCL   │
└─────────────┘
```

**Enforced by:**
- Roslyn analyzer (compile-time error)
- Architecture unit tests (CI/CD)

## Modules

### 1. Http Infrastructure

**Purpose:** Abstract HTTP communication for testing and flexibility

**Location:** `Infrastructure/Http/`

**Key Types:**
```csharp
// Primary abstraction
public interface IHttpClientProvider
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct);
    Task<TResult> PostAsJsonAsync<TResult>(string url, object body, CancellationToken ct);
}

// Default implementation
public class DefaultHttpClientProvider : IHttpClientProvider
{
    // Uses HttpClient internally
}
```

**Used By:** Queries, Mutations, Actions, FileStorage, VectorSearch, HttpActions, Scheduling

**Why in Infrastructure?**
- All HTTP-based features need this
- Enables testing with mock HTTP
- Centralizes retry/timeout logic
- Single place to add telemetry

---

### 2. Serialization

**Purpose:** JSON serialization/deserialization with Convex-specific handling

**Location:** `Infrastructure/Serialization/`

**Key Types:**
```csharp
public interface IConvexSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string json);
    JsonSerializerOptions Options { get; }
}

public class ConvexSerializer : IConvexSerializer
{
    // Handles ConvexNumber, ConvexValue, etc.
}
```

**Used By:** All features that send/receive JSON

**Why in Infrastructure?**
- Consistent serialization behavior
- Centralized Convex-specific type handling
- Easy to swap serialization libraries
- Performance optimizations in one place

---

### 3. ErrorHandling

**Purpose:** Common exception types and result patterns

**Location:** `Infrastructure/ErrorHandling/`

**Key Types:**
```csharp
public class ConvexException : Exception
{
    public string Code { get; }
    public int? StatusCode { get; }
}

public class ConvexResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ConvexError? Error { get; }
}
```

**Used By:** All features

**Why in Infrastructure?**
- Consistent error handling patterns
- Type-safe error propagation
- Centralized error categorization

---

### 4. Configuration

**Purpose:** Client configuration and options

**Location:** `Infrastructure/Configuration/`

**Key Types:**
```csharp
public class ConvexClientOptions
{
    public string DeploymentUrl { get; set; }
    public TimeSpan DefaultTimeout { get; set; }
    public RetryPolicy RetryPolicy { get; set; }
}

public interface IConvexOptions
{
    ConvexClientOptions Value { get; }
}
```

**Used By:** All features that need configuration

**Why in Infrastructure?**
- Single source of configuration
- Consistent validation
- Easy to add new options

---

## Adding New Infrastructure

### When to Add

1. **Usage Check:** Is it used by 3+ features?
   - Yes → Consider adding to Infrastructure
   - No → Keep in features (some duplication is OK)

2. **Technical Check:** Is it purely infrastructure?
   - Yes → Can go in Infrastructure
   - No → Belongs in feature or facade

3. **Stability Check:** Is the interface stable?
   - Yes → Safe to add to Infrastructure
   - No → Wait until it stabilizes in a slice

### RFC Process

For significant Infrastructure additions:

1. **Create RFC** in GitHub Discussions
2. **Get approval** from 66% of feature owners
3. **Implement** with backward compatibility when possible
4. **Update** this README.md
5. **Migrate** existing features to use it

### Example RFC Template

```markdown
## RFC: Add Infrastructure/Caching

**Author:** Jane Developer
**Date:** 2025-01-15

### Problem
5 features (Queries, Mutations, Subscriptions, Pagination, VectorSearch)
each have their own cache invalidation logic with subtle differences.

### Proposal
Extract `ICacheProvider` interface to Infrastructure/Caching/

\`\`\`csharp
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task InvalidateAsync(string pattern);
}
\`\`\`

### Benefits
- Consistent cache behavior
- Easier testing
- Pluggable cache backends

### Risks
- Breaking change for existing cache code
- Migration effort for 5 features

### Migration Path
1. Add ICacheProvider to Infrastructure/
2. Create adapter for existing QueryCache
3. Migrate features one-by-one
4. Remove old cache implementations

**Vote:** Please comment with +1 (approve) or -1 (reject) with reasoning
```

## Testing Infrastructure

### Unit Tests

**Location:** `tests/Infrastructure/`

```csharp
// tests/Infrastructure/Http/HttpClientProviderTests.cs
public class HttpClientProviderTests
{
    [Test]
    public async Task SendAsync_WithTimeout_ThrowsOnTimeout()
    {
        var provider = new DefaultHttpClientProvider(timeout: TimeSpan.FromMilliseconds(10));
        var request = new HttpRequestMessage(HttpMethod.Get, "https://slow-endpoint.com");

        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await provider.SendAsync(request, CancellationToken.None));
    }
}
```

### Integration Tests

Test Infrastructure with real dependencies:

```csharp
[Test]
public async Task Serializer_RoundTrip_PreservesConvexNumber()
{
    var serializer = new ConvexSerializer();
    var original = new ConvexNumber(42.5);

    var json = serializer.Serialize(original);
    var deserialized = serializer.Deserialize<ConvexNumber>(json);

    Assert.That(deserialized, Is.EqualTo(original));
}
```

## Guidelines for Infrastructure Code

### Interface Design

1. **Keep interfaces minimal**
   ```csharp
   // ✅ Good - focused interface
   public interface IHttpClientProvider
   {
       Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct);
   }

   // ❌ Bad - too many responsibilities
   public interface IHttpClientProvider
   {
       Task<HttpResponseMessage> SendAsync(...);
       Task AuthenticateAsync(...);
       Task<Stream> DownloadFileAsync(...);
       Task UploadFileAsync(...);
   }
   ```

2. **Avoid leaky abstractions**
   ```csharp
   // ✅ Good - abstracts implementation details
   public interface IConvexSerializer
   {
       T? Deserialize<T>(string json);
   }

   // ❌ Bad - exposes System.Text.Json details
   public interface IConvexSerializer
   {
       T? Deserialize<T>(string json, JsonSerializerOptions options);
   }
   ```

3. **Design for testability**
   ```csharp
   // ✅ Good - easy to mock
   public interface IHttpClientProvider
   {
       Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct);
   }

   // ❌ Bad - hard to mock static/sealed classes
   public static class HttpClientProvider
   {
       public static Task<HttpResponseMessage> SendAsync(...) { }
   }
   ```

### Implementation Guidelines

1. **No business logic**
   ```csharp
   // ✅ Good - pure infrastructure
   public class DefaultHttpClientProvider : IHttpClientProvider
   {
       public async Task<HttpResponseMessage> SendAsync(...)
       {
           return await _httpClient.SendAsync(request, ct);
       }
   }

   // ❌ Bad - contains business logic
   public class DefaultHttpClientProvider : IHttpClientProvider
   {
       public async Task<HttpResponseMessage> SendAsync(...)
       {
           // ❌ This is business logic, not infrastructure
           if (request.RequestUri.Contains("query"))
               await InvalidateCacheAsync();

           return await _httpClient.SendAsync(request, ct);
       }
   }
   ```

2. **Fail fast with clear errors**
   ```csharp
   public ConvexSerializer(JsonSerializerOptions? options = null)
   {
       _options = options ?? throw new ArgumentNullException(nameof(options));
   }
   ```

3. **Document expected behavior**
   ```csharp
   /// <summary>
   /// Sends an HTTP request with automatic retry on transient failures.
   /// </summary>
   /// <param name="request">The HTTP request to send.</param>
   /// <param name="ct">Cancellation token.</param>
   /// <returns>The HTTP response.</returns>
   /// <exception cref="ConvexException">Thrown on network errors or HTTP errors after retries.</exception>
   public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
   ```

## Breaking Changes

### Adding to Infrastructure (Non-Breaking)

✅ **Safe changes:**
- Add new interface
- Add new method to existing interface (with default implementation)
- Add new optional parameter (with default value)
- Add new implementation class

### Modifying Infrastructure (Breaking)

⚠️ **Breaking changes require careful migration:**
- Change method signature
- Remove interface/method
- Change exception types
- Change serialization format

**Process for breaking changes:**
1. Create deprecation notice (XML docs + ObsoleteAttribute)
2. Add new API alongside old one
3. Migrate all features to new API
4. Remove old API in next major version

Example:
```csharp
// Old API (deprecated)
[Obsolete("Use SendAsync instead. Will be removed in v2.0.0")]
public Task<HttpResponseMessage> Send(HttpRequestMessage request)
    => SendAsync(request, CancellationToken.None);

// New API
public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
{
    // Implementation
}
```

## Performance Considerations

### Caching

Cache expensive operations in Infrastructure:

```csharp
public class ConvexSerializer : IConvexSerializer
{
    // Cache JsonSerializerOptions (expensive to create)
    private static readonly JsonSerializerOptions _defaultOptions = CreateDefaultOptions();
}
```

### Object Pooling

Consider pooling for frequently allocated objects:

```csharp
public class DefaultHttpClientProvider : IHttpClientProvider
{
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
}
```

### Async Best Practices

```csharp
// ✅ Good - ConfigureAwait(false) for library code
public async Task<T> GetAsync<T>(string key)
{
    var json = await _cache.GetStringAsync(key).ConfigureAwait(false);
    return Deserialize<T>(json);
}
```

## Migration Checklist

When extracting code to Infrastructure:

- [ ] Used by 3+ features?
- [ ] Purely technical infrastructure?
- [ ] Interface is stable?
- [ ] Unit tests added to `tests/Infrastructure/`
- [ ] Documentation added to this README
- [ ] All existing features migrated to use it
- [ ] Architecture tests pass
- [ ] No Roslyn analyzer violations

## Questions?

- **Should this go in Infrastructure?** → Ask in GitHub Discussions
- **How to propose new Infrastructure?** → Create RFC (see above)
- **Breaking change needed?** → Follow deprecation process (see above)

## References

- **Feature Architecture:** [Features/README.md](../Features/README.md)
- **Architecture Overview:** [CLAUDE.md](../../CLAUDE.md)
- **Architecture Tests:** `tests/ArchitectureTests/`
- **Roslyn Analyzer:** `analyzers/Convex.Analyzers/SliceDependencyAnalyzer.cs`
