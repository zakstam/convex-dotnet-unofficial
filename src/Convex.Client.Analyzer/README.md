# Convex .NET Client Analyzer

Roslyn analyzers for the Convex .NET Client library. Provides compile-time analysis to enforce best practices, prevent common issues, and suggest performance optimizations.

## Installation

Install the analyzer NuGet package:

```bash
dotnet add package Convex.Client.Analyzer
```

The analyzer will automatically run during compilation and provide warnings for code that violates the rules.

## Rules

### CVX001: Avoid direct IConvexClient method calls

**Severity**: Warning

Direct calls to IConvexClient methods bypass error handling, retry logic, and performance optimizations provided by extension methods.

**Example**:
```csharp
// ❌ Avoid
var result = client.Query<object>("functions/getData");

// ✅ Prefer
var result = await client.Query<object>("functions/getData").WithArgs(args).ExecuteAsync();
```

### CVX002: Ensure connection state monitoring for real-time features

**Severity**: Warning

Real-time subscriptions can fail silently when the connection is lost. Monitor connection state to handle disconnections gracefully.

**Example**:
```csharp
// ❌ Avoid
var subscription = client.Observe<Data>("functions/getData");

// ✅ Prefer
var subscription = client.CreateResilientSubscription<Data>("functions/getData");
// Or
client.ConnectionStateChanges.Subscribe(state => { /* handle state */ });
var subscription = client.Observe<Data>("functions/getData");
```

### CVX003: Avoid generic Exception types in Convex operations

**Severity**: Warning

Generic exception handling makes error diagnosis difficult and can hide Convex-specific issues. Use specific Convex exception types for better error handling.

**Example**:
```csharp
// ❌ Avoid
try
{
    await client.Query<object>("functions/getData").ExecuteAsync();
}
catch (Exception ex)
{
    // Too generic
}

// ✅ Prefer
try
{
    await client.Query<object>("functions/getData").ExecuteAsync();
}
catch (ConvexFunctionException ex)
{
    // Handle function errors
}
catch (ConvexNetworkException ex)
{
    // Handle network errors
}
catch (ConvexException ex)
{
    // Handle other Convex errors
}
```

### CVX004: Use type-safe function name constants

**Severity**: Warning

String literals for function names are error-prone and prevent compile-time validation. Use the generated ConvexFunctions constants for type safety.

**Example**:
```csharp
// ❌ Avoid
var result = await client.Query<object>("functions/getMessages").ExecuteAsync();

// ✅ Prefer
var result = await client.Query<object>(ConvexFunctions.Queries.GetMessages).ExecuteAsync();
```

### CVX005: Missing error handling in async operations

**Severity**: Info

Unhandled async Convex operations can hide errors. Add error handling with try-catch or OnError() handlers.

**Example**:
```csharp
// ❌ Avoid
var result = await client.Query<object>("functions/getData").ExecuteAsync();

// ✅ Prefer
try
{
    var result = await client.Query<object>("functions/getData").ExecuteAsync();
}
catch (ConvexException ex)
{
    // Handle error
}

// Or use OnError handler
await client.Query<object>("functions/getData")
    .OnError(ex => { /* handle error */ })
    .ExecuteAsync();
```

### CVX006: Subscription disposal

**Severity**: Warning

Subscriptions that are not properly disposed can cause memory leaks. Implement IDisposable pattern for classes with subscriptions.

**Example**:
```csharp
// ❌ Avoid
class MyClass
{
    private IDisposable? _subscription;
    
    public void Start()
    {
        _subscription = client.Observe<Data>("functions/getData").Subscribe(/* ... */);
    }
    // Missing Dispose()
}

// ✅ Prefer
class MyClass : IDisposable
{
    private IDisposable? _subscription;
    
    public void Start()
    {
        _subscription = client.Observe<Data>("functions/getData").Subscribe(/* ... */);
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### CVX007: Builder pattern best practices

**Severity**: Warning

Builder pattern issues: missing ExecuteAsync(), invalid chaining, or missing required methods.

**Example**:
```csharp
// ❌ Avoid
var builder = client.Query<object>("functions/getData");
// Missing ExecuteAsync()

// ✅ Prefer
var result = await client.Query<object>("functions/getData").ExecuteAsync();
```

### CVX008: Type safety for function arguments

**Severity**: Info

Anonymous objects for arguments reduce type safety. Use typed argument classes when available.

**Example**:
```csharp
// ❌ Avoid
var result = await client.Query<object>("functions/getData")
    .WithArgs(new { id = 123, name = "test" })
    .ExecuteAsync();

// ✅ Prefer
class GetDataArgs
{
    public int Id { get; set; }
    public string Name { get; set; }
}

var result = await client.Query<object>("functions/getData")
    .WithArgs(new GetDataArgs { Id = 123, Name = "test" })
    .ExecuteAsync();
```

### CVX009: Optimistic update best practices

**Severity**: Info

Optimistic update issues: missing rollback handlers, optimistic updates on queries, or missing optimistic updates on mutations.

**Example**:
```csharp
// ❌ Avoid - optimistic update on query
await client.Query<object>("functions/getData")
    .OptimisticWithAutoRollback(/* ... */)
    .ExecuteAsync();

// ✅ Prefer - optimistic update on mutation with rollback
await client.Mutate<object>("functions/updateData")
    .OptimisticWithAutoRollback(
        optimisticUpdate: state => { /* update */ },
        rollback: state => { /* rollback */ })
    .ExecuteAsync();
```

### CVX010: Cache invalidation patterns

**Severity**: Info

Mutations that modify data should invalidate related queries to ensure cache consistency.

**Example**:
```csharp
// ❌ Missing cache invalidation
await client.Mutate<object>("functions/createTodo").ExecuteAsync();

// ✅ Prefer
// Set up cache dependencies
client.DefineQueryDependency("functions/createTodo", "functions/listTodos", "functions/getTodoCount");

await client.Mutate<object>("functions/createTodo").ExecuteAsync();
```

## Configuration

You can configure analyzer rules in your `.editorconfig` file:

```ini
# Disable a specific rule
dotnet_diagnostic.CVX001.severity = none

# Change severity
dotnet_diagnostic.CVX005.severity = warning

# Enable as error
dotnet_diagnostic.CVX003.severity = error
```

## Code Fixes

The analyzer package includes automatic code fixes for many rules. Use the lightbulb (Ctrl+.) in your IDE to apply fixes automatically.

## Troubleshooting

### Analyzers not running

1. Ensure the `Convex.Client.Analyzer` package is installed
2. Restart your IDE
3. Run `dotnet clean && dotnet build`

### False positives

If you encounter false positives, you can:
1. Suppress the warning with `#pragma warning disable CVX###`
2. Configure the rule severity in `.editorconfig`
3. Report the issue on GitHub

## Contributing

Contributions are welcome! Please see the main repository for contribution guidelines.
