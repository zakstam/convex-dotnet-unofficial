# Health Slice

## Purpose
Provides connection health monitoring and metrics tracking for Convex clients. Tracks performance metrics like latency, message counts, reconnections, and errors to determine overall connection health status. Enables observability and debugging of connection issues.

## Responsibilities
- Health status determination (Healthy, Degraded, Unhealthy, Unknown)
- Latency tracking with rolling window (last 100 samples)
- Message count tracking (sent/received)
- Reconnection count tracking
- Error tracking (last 10 errors)
- Connection uptime tracking
- Time since last message tracking
- Health check generation with comprehensive metrics
- Thread-safe metric recording

## Public API Surface

### Main Interface
```csharp
public interface IConvexHealth
{
    // Metric recording
    void RecordMessageReceived();
    void RecordMessageSent();
    void RecordLatency(double latencyMs);
    void RecordReconnection();
    void RecordConnectionEstablished();
    void RecordError(Exception error);

    // Metric retrieval
    double? GetAverageLatency();
    long GetMessagesReceived();
    long GetMessagesSent();
    int GetReconnectionCount();
    TimeSpan? GetTimeSinceLastMessage();
    TimeSpan? GetConnectionUptime();
    IReadOnlyList<Exception> GetRecentErrors();

    // Management
    void Reset();
    ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions);
}
```

### Health Types
```csharp
public enum ConvexHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public class ConvexHealthCheck
{
    public ConvexHealthStatus Status { get; }
    public string Description { get; }
    public DateTimeOffset Timestamp { get; }
    public ConnectionState ConnectionState { get; }
    public double? AverageLatencyMs { get; }
    public int ActiveSubscriptions { get; }
    public int ReconnectionCount { get; }
    public long MessagesReceived { get; }
    public long MessagesSent { get; }
    public TimeSpan? TimeSinceLastMessage { get; }
    public IReadOnlyList<Exception> RecentErrors { get; }
    public IReadOnlyDictionary<string, object> AdditionalData { get; }
}
```

## Shared Dependencies
None - this is a pure state management slice with no external dependencies beyond Common types (ConnectionState).

## Architecture
- **HealthSlice**: Public facade implementing IConvexHealth
- **HealthMonitor**: Internal implementation with ConcurrentQueue for metrics
- **HealthCheckBuilder**: Internal fluent builder for creating health checks
- **Metric Storage**: ConcurrentQueue for latency samples and errors, Interlocked for counters

## Usage Examples

### Basic Health Check
```csharp
// Get current health status
var healthCheck = client.HealthSlice.CreateHealthCheck(
    client.ConnectionState,
    client.ActiveSubscriptionCount);

Console.WriteLine(healthCheck.ToString());
// Output: Health: Healthy - Connection is healthy (latency: 45ms)
//         (ConnectionState: Connected, Latency: 45.00ms, Subscriptions: 3, Reconnections: 0)

// Check specific status
if (healthCheck.Status == ConvexHealthStatus.Healthy)
{
    Console.WriteLine($"All systems operational. Average latency: {healthCheck.AverageLatencyMs}ms");
}
```

### Recording Metrics
```csharp
// Record latency for a query
var stopwatch = Stopwatch.StartNew();
var result = await client.QueryAsync<MyData>("myQuery");
stopwatch.Stop();
client.HealthSlice.RecordLatency(stopwatch.Elapsed.TotalMilliseconds);

// Record message sent/received
client.HealthSlice.RecordMessageSent();
client.HealthSlice.RecordMessageReceived();

// Record errors
try
{
    await client.MutationAsync("failingMutation", args);
}
catch (Exception ex)
{
    client.HealthSlice.RecordError(ex);
    throw;
}

// Record reconnection events
client.ConnectionStateChanged += (sender, state) =>
{
    if (state == ConnectionState.Connected)
    {
        client.HealthSlice.RecordConnectionEstablished();
    }
    else if (state == ConnectionState.Reconnecting)
    {
        client.HealthSlice.RecordReconnection();
    }
};
```

### Retrieving Metrics
```csharp
// Get average latency
var avgLatency = client.HealthSlice.GetAverageLatency();
if (avgLatency.HasValue)
{
    Console.WriteLine($"Average latency: {avgLatency.Value:F2}ms");
}

// Get message counts
var sent = client.HealthSlice.GetMessagesSent();
var received = client.HealthSlice.GetMessagesReceived();
Console.WriteLine($"Messages sent: {sent}, received: {received}");

// Get reconnection count
var reconnections = client.HealthSlice.GetReconnectionCount();
Console.WriteLine($"Reconnections: {reconnections}");

// Get time since last message
var timeSinceLastMessage = client.HealthSlice.GetTimeSinceLastMessage();
if (timeSinceLastMessage.HasValue)
{
    Console.WriteLine($"Last message received {timeSinceLastMessage.Value.TotalSeconds:F1} seconds ago");
}

// Get connection uptime
var uptime = client.HealthSlice.GetConnectionUptime();
if (uptime.HasValue)
{
    Console.WriteLine($"Connected for {uptime.Value.TotalMinutes:F1} minutes");
}

// Get recent errors
var errors = client.HealthSlice.GetRecentErrors();
if (errors.Count > 0)
{
    Console.WriteLine($"Recent errors ({errors.Count}):");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error.Message}");
    }
}
```

### Comprehensive Health Check
```csharp
var healthCheck = client.HealthSlice.CreateHealthCheck(
    client.ConnectionState,
    client.ActiveSubscriptionCount);

// Display all metrics
Console.WriteLine($"Status: {healthCheck.Status}");
Console.WriteLine($"Description: {healthCheck.Description}");
Console.WriteLine($"Connection State: {healthCheck.ConnectionState}");
Console.WriteLine($"Active Subscriptions: {healthCheck.ActiveSubscriptions}");
Console.WriteLine($"Messages: {healthCheck.MessagesReceived} received, {healthCheck.MessagesSent} sent");
Console.WriteLine($"Reconnections: {healthCheck.ReconnectionCount}");

if (healthCheck.AverageLatencyMs.HasValue)
{
    Console.WriteLine($"Average Latency: {healthCheck.AverageLatencyMs.Value:F2}ms");
}

if (healthCheck.TimeSinceLastMessage.HasValue)
{
    Console.WriteLine($"Time Since Last Message: {healthCheck.TimeSinceLastMessage.Value.TotalSeconds:F1}s");
}

// Check additional data
if (healthCheck.AdditionalData.TryGetValue("ConnectionUptime", out var uptimeObj) && uptimeObj is TimeSpan uptime)
{
    Console.WriteLine($"Connection Uptime: {uptime.TotalMinutes:F1} minutes");
}

// Display recent errors
if (healthCheck.RecentErrors.Count > 0)
{
    Console.WriteLine($"\nRecent Errors ({healthCheck.RecentErrors.Count}):");
    foreach (var error in healthCheck.RecentErrors)
    {
        Console.WriteLine($"  - {error.GetType().Name}: {error.Message}");
    }
}
```

### Resetting Metrics
```csharp
// Reset all metrics (useful for testing or new connection)
client.HealthSlice.Reset();

// Verify metrics are reset
var healthCheck = client.HealthSlice.CreateHealthCheck(
    client.ConnectionState,
    0);

Console.WriteLine($"Messages received: {healthCheck.MessagesReceived}"); // 0
Console.WriteLine($"Reconnections: {healthCheck.ReconnectionCount}"); // 0
Console.WriteLine($"Average latency: {healthCheck.AverageLatencyMs}"); // null
```

### Monitoring Health Status Over Time
```csharp
// Periodic health monitoring
var healthTimer = new Timer(_ =>
{
    var health = client.HealthSlice.CreateHealthCheck(
        client.ConnectionState,
        client.ActiveSubscriptionCount);

    if (health.Status == ConvexHealthStatus.Degraded)
    {
        Console.WriteLine($"WARNING: Connection degraded - {health.Description}");
    }
    else if (health.Status == ConvexHealthStatus.Unhealthy)
    {
        Console.WriteLine($"ERROR: Connection unhealthy - {health.Description}");
        // Trigger reconnection logic
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
```

## Implementation Details
- Uses ConcurrentQueue for latency samples (max 100) and errors (max 10)
- Uses Interlocked operations for thread-safe counter increments
- Latency average calculated from all samples in rolling window
- Health status determined by configurable thresholds
- No HTTP calls - pure state management and metric tracking

## Health Status Determination Algorithm

The health status is determined by checking conditions in order:

1. **Unhealthy**:
   - Connection state is Disconnected

2. **Degraded**:
   - Connection state is Connecting or Reconnecting
   - Recent error count >= 5
   - Average latency > 1000ms (high)
   - Average latency > 500ms (elevated)
   - No messages received in > 5 minutes
   - Reconnection count > 10

3. **Healthy**:
   - All checks pass, connection operating normally

4. **Unknown**:
   - Default state when insufficient data available

## Health Thresholds

| Metric | Threshold | Status |
|--------|-----------|--------|
| Latency | > 1000ms | Degraded (high) |
| Latency | > 500ms | Degraded (elevated) |
| Recent Errors | >= 5 | Degraded |
| Time Since Last Message | > 5 minutes | Degraded |
| Reconnection Count | > 10 | Degraded |
| Connection State | Disconnected | Unhealthy |
| Connection State | Connecting/Reconnecting | Degraded |

## Metric Limits
- **Latency samples**: Max 100 (rolling window, FIFO)
- **Recent errors**: Max 10 (rolling window, FIFO)
- **Message counters**: long (max 9,223,372,036,854,775,807)
- **Reconnection counter**: int (max 2,147,483,647)

## Thread Safety
All metric recording operations are thread-safe:
- Latency and error recording use ConcurrentQueue (lock-free)
- Message and reconnection counters use Interlocked operations (atomic)
- Reset() uses lock for clearing all metrics atomically
- GetAverageLatency() creates snapshot with ToArray() before averaging

## Limitations
- No metric persistence (resets on application restart)
- Fixed sample window sizes (100 latency, 10 errors)
- No configurable health thresholds (hardcoded in DetermineHealthStatus)
- No historical trend analysis (only current/recent metrics)
- No alerting system (consumers must poll CreateHealthCheck)

## Owner
TBD
