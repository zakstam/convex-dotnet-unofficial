# Resilience Slice

## Purpose

Provides resilience patterns (retry and circuit breaker) for robust client operations. Implements retry logic with exponential backoff and circuit breaker pattern to prevent cascade failures. Enables fault-tolerant and self-healing operation execution.

## Responsibilities

- Retry policy management (exponential backoff, custom strategies)
- Circuit breaker policy management (failure threshold, break duration)
- Combined resilience execution (retry + circuit breaker)
- Transient failure detection
- Failure counting and state transitions
- Thread-safe policy configuration and execution

## Public API Surface

### Main Interface

```csharp
public interface IConvexResilience
{
    RetryPolicy? RetryPolicy { get; set; }
    ICircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }

    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
```

### Retry Policy

```csharp
public sealed class RetryPolicy
{
    public int MaxRetries { get; }
    public bool ShouldRetry(Exception exception);
    public TimeSpan CalculateDelay(int attemptNumber); // 1-based attempt number

    // Factory methods
    public static RetryPolicy Default();
    public static RetryPolicy Aggressive();
    public static RetryPolicy Conservative();
    public static RetryPolicy None();
}

// Builder API for custom policies
public sealed class RetryPolicyBuilder
{
    public RetryPolicyBuilder MaxRetries(int maxRetries);
    public RetryPolicyBuilder ExponentialBackoff(TimeSpan initialDelay, double multiplier = 2.0, bool useJitter = true);
    public RetryPolicyBuilder LinearBackoff(TimeSpan initialDelay);
    public RetryPolicyBuilder ConstantBackoff(TimeSpan delay);
    public RetryPolicyBuilder WithMaxDelay(TimeSpan maxDelay);
    public RetryPolicyBuilder RetryOn<TException>() where TException : Exception;
    public RetryPolicyBuilder OnRetry(Action<int, Exception, TimeSpan> callback);
    public RetryPolicy Build();
}
```

### Circuit Breaker Policy

```csharp
public interface ICircuitBreakerPolicy
{
    int FailureThreshold { get; }
    TimeSpan BreakDuration { get; }
    CircuitBreakerState State { get; }

    void RecordSuccess();
    void RecordFailure(Exception exception);
    bool AllowRequest();
}

public class CircuitBreakerPolicy : ICircuitBreakerPolicy
{
    // Implementation with configurable thresholds
}

public enum CircuitBreakerState
{
    Closed,     // Normal operation
    Open,       // Circuit tripped, blocking requests
    HalfOpen    // Testing if service recovered
}
```

### Exception Types

```csharp
public class ConvexCircuitBreakerException : ConvexException
{
    public CircuitBreakerState CircuitState { get; }
}
```

## Shared Dependencies

- **RetryPolicy**: Builder-based retry policy (in `Shared/Resilience/`)
- **ICircuitBreakerPolicy**: Circuit breaker interface (in `Shared/Resilience/`)
- **ResilienceCoordinator**: Coordinates retry and circuit breaker policies (in `Shared/Resilience/`)
- **ConvexException types**: For transient failure detection

## Architecture

- **ResilienceSlice**: Public facade implementing IConvexResilience
- **ResilienceCoordinatorWrapper**: Internal wrapper around Shared/Resilience/ResilienceCoordinator
- **ResilienceCoordinator**: Coordinates retry and circuit breaker policies (in Shared/Resilience/)
- **RetryPolicy**: Builder-based retry policy with exponential backoff and jitter (in Shared/Resilience/)
- **CircuitBreakerPolicy**: Standard circuit breaker implementation (in Shared/Resilience/)

## Usage Examples

### Basic Retry Configuration

```csharp
var resilience = client.ResilienceSlice;

// Use default retry policy (3 retries, exponential backoff with jitter)
resilience.RetryPolicy = RetryPolicy.Default();

// Or create custom retry policy using builder
resilience.RetryPolicy = new RetryPolicyBuilder()
    .MaxRetries(3)
    .ExponentialBackoff(TimeSpan.FromMilliseconds(100), multiplier: 2.0, useJitter: true)
    .WithMaxDelay(TimeSpan.FromSeconds(30))
    .Build();

// Execute operation with retry
var result = await resilience.ExecuteAsync(async () =>
{
    return await client.QueryAsync<MyData>("myQuery");
});
```

### Circuit Breaker Configuration

```csharp
var resilience = client.ResilienceSlice;

// Configure circuit breaker
resilience.CircuitBreakerPolicy = new CircuitBreakerPolicy(
    failureThreshold: 5,
    breakDuration: TimeSpan.FromSeconds(30)
);

// Execute operation with circuit breaker
try
{
    await resilience.ExecuteAsync(async () =>
    {
        await client.MutationAsync("myMutation", args);
    });
}
catch (ConvexCircuitBreakerException ex)
{
    Console.WriteLine($"Circuit breaker is {ex.CircuitState}");
    // Handle circuit open state
}
```

### Combined Retry + Circuit Breaker

```csharp
var resilience = client.ResilienceSlice;

// Configure both policies
resilience.RetryPolicy = RetryPolicy.Default(); // 3 retries, exponential backoff with jitter

resilience.CircuitBreakerPolicy = new CircuitBreakerPolicy(
    failureThreshold: 5,
    breakDuration: TimeSpan.FromSeconds(30)
);

// Execute with both patterns
var result = await resilience.ExecuteAsync(async () =>
{
    return await client.QueryAsync<MyData>("myQuery");
});
// Flow:
// 1. Check circuit breaker (throw if open)
// 2. Execute operation
// 3. If fails, record failure in circuit breaker
// 4. If retryable, wait and retry (up to MaxRetries)
// 5. Record success in circuit breaker on successful completion
```

### Custom Retry Logic

```csharp
var resilience = client.ResilienceSlice;

// Create custom retry policy using builder
resilience.RetryPolicy = new RetryPolicyBuilder()
    .MaxRetries(5)
    .ExponentialBackoff(TimeSpan.FromSeconds(1), multiplier: 2.0)
    .RetryOn<ConvexNetworkException>() // Only retry on network errors
    .OnRetry((attempt, ex, delay) =>
    {
        Console.WriteLine($"Retry attempt {attempt} after {delay.TotalSeconds}s");
    })
    .Build();
```

### Monitoring Circuit Breaker State

```csharp
var resilience = client.ResilienceSlice;
var circuitBreaker = new CircuitBreakerPolicy(
    failureThreshold: 5,
    breakDuration: TimeSpan.FromSeconds(30)
);
resilience.CircuitBreakerPolicy = circuitBreaker;

// Monitor state
Console.WriteLine($"Circuit State: {circuitBreaker.State}");
// Closed: Normal operation
// Open: Too many failures, blocking requests
// HalfOpen: Testing if service recovered

// Circuit breaker state transitions:
// Closed -> Open (after 5 failures)
// Open -> HalfOpen (after 30 seconds)
// HalfOpen -> Closed (on successful request)
// HalfOpen -> Open (on failed request)
```

### Disabling Resilience Patterns

```csharp
var resilience = client.ResilienceSlice;

// Disable retry (fail immediately)
resilience.RetryPolicy = null;

// Disable circuit breaker (no cascade failure protection)
resilience.CircuitBreakerPolicy = null;

// Execute without resilience patterns
var result = await resilience.ExecuteAsync(async () =>
{
    return await client.QueryAsync<MyData>("myQuery");
});
```

### Jitter for Thundering Herd Prevention

```csharp
// Enable jitter to prevent synchronized retries (default: enabled)
var retryPolicy = new RetryPolicyBuilder()
    .MaxRetries(3)
    .ExponentialBackoff(TimeSpan.FromMilliseconds(100), multiplier: 2.0, useJitter: true)
    .WithMaxDelay(TimeSpan.FromSeconds(30))
    .Build();

// Without jitter: All clients retry at same time (100ms, 200ms, 400ms, ...)
// With jitter: Clients retry at slightly different times (±25% variance)
```

## Implementation Details

- Circuit breaker checks happen BEFORE retry attempts
- Circuit breaker records failures AFTER each attempt (including retries)
- Circuit breaker only counts transient failures (not function/argument errors)
- Retry delay uses exponential backoff with configurable jitter
- Thread-safe policy configuration with property setters
- No HTTP calls - pure resilience pattern execution

## Retry Policy Details

### Transient Failures (Retryable)

- **Network Errors**: Timeout, connection failure, server errors (500-504, 429)
- **HTTP Errors**: TaskCanceledException, HttpRequestException

### Non-Transient Failures (Not Retryable)

- **Function Errors**: ConvexFunctionException (business logic errors)
- **Argument Errors**: ConvexArgumentException (validation errors)
- **Auth Errors**: ConvexAuthenticationException (authentication/authorization)
- **DNS/SSL Errors**: Usually persistent infrastructure issues

### Retry Delay Calculation

```
Exponential: BaseDelay * 2^attempt (capped at MaxDelay)
With Jitter: ±25% random variance to prevent synchronized retries

Example (BaseDelay=100ms, MaxDelay=30s):
  Attempt 0: 100ms ± 25ms = 75-125ms
  Attempt 1: 200ms ± 50ms = 150-250ms
  Attempt 2: 400ms ± 100ms = 300-500ms
  Attempt 3: 800ms ± 200ms = 600-1000ms
  ...
  Attempt 9+: 30s ± 7.5s = 22.5-37.5s (capped)
```

## Circuit Breaker Details

### State Transitions

```
Closed (Normal):
  - All requests allowed
  - Failures increment counter
  - FailureThreshold failures -> Open

Open (Circuit Tripped):
  - All requests blocked immediately
  - After BreakDuration -> HalfOpen

HalfOpen (Testing Recovery):
  - Single request allowed through
  - Success -> Closed (reset counter)
  - Failure -> Open (restart break duration)
```

### Failure Counting

Only transient failures count toward circuit breaker:

- Network errors (timeout, connection failure)
- Server errors (500-504)
- TaskCanceledException

Function, argument, and auth errors do NOT count (client-side issues).

## Thread Safety

All resilience operations are thread-safe:

- Policy properties use simple get/set (atomic reference assignment)
- Circuit breaker state uses lock for atomic state transitions
- Failure counter uses lock for thread-safe increments
- Retry execution serializes attempts (no concurrent retries)

## Limitations

- No distributed circuit breaker (each client instance has own state)
- No circuit breaker metrics/events
- Circuit breaker doesn't decay failure count over time
- No half-open request limiting (allows unlimited test requests)

## Architectural Notes

All resilience types are consolidated in `Shared/Resilience/`:

- **RetryPolicy**: Builder-based retry policy (replaces old IRetryPolicy interface-based system)
- **ICircuitBreakerPolicy** and **CircuitBreakerPolicy**: Circuit breaker implementation
- **ResilienceCoordinator**: Coordinates retry and circuit breaker policies
- **IReconnectPolicy**: Interface for WebSocket reconnection

The retry system uses a single unified `RetryPolicy` class with a fluent builder API. This replaces the previous dual system (IRetryPolicy interface + ExponentialBackoffRetryPolicy implementation). The builder-based API provides more flexibility and better defaults for transient exception detection.

This slice provides a facade over Shared/Resilience types for easy access from ConvexClient.

## Owner

TBD
