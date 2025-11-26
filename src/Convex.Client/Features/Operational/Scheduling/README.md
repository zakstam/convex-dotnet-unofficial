# Scheduling Slice

## Purpose
Provides delayed and recurring function execution capabilities in Convex. Supports one-time scheduling, cron-based recurring schedules, and interval-based recurring execution.

## Responsibilities
- One-time function scheduling (delay-based or at specific time)
- Recurring schedules with cron expressions
- Interval-based recurring schedules
- Job management (cancel, update, retrieve info)
- Job listing with filtering by status and function name
- Schedule validation and error handling

## Public API Surface

### Main Interface
```csharp
public interface IConvexScheduler
{
    // One-time scheduling
    Task<string> ScheduleAsync(string functionName, TimeSpan delay, ...);
    Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, ...);
    Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, ...);
    Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, ...);

    // Recurring scheduling
    Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, ...);
    Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, ...);
    Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, ...);
    Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, ...);

    // Job management
    Task<bool> CancelAsync(string jobId, ...);
    Task<ConvexScheduledJob> GetJobAsync(string jobId, ...);
    Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(...);
    Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, ...);
}
```

### Schedule Types
```csharp
public class ConvexScheduleConfig
{
    public ConvexScheduleType Type { get; }
    public DateTimeOffset? ScheduledTime { get; }      // For OneTime
    public string? CronExpression { get; }             // For Cron
    public TimeSpan? Interval { get; }                 // For Interval
    public string? Timezone { get; }                   // For Cron
    public DateTimeOffset? StartTime { get; }          // For Interval
    public DateTimeOffset? EndTime { get; }            // For Interval

    // Factory methods
    static ConvexScheduleConfig OneTime(DateTimeOffset scheduledTime);
    static ConvexScheduleConfig Cron(string cronExpression, string timezone = "UTC");
    static ConvexScheduleConfig CreateInterval(TimeSpan interval, ...);
}

public enum ConvexScheduleType { OneTime, Cron, Interval }
public enum ConvexJobStatus { Pending, Running, Completed, Failed, Cancelled, Active, Paused }
```

### Job Information
```csharp
public class ConvexScheduledJob
{
    public string Id { get; }
    public string FunctionName { get; }
    public ConvexJobStatus Status { get; }
    public JsonElement? Arguments { get; }
    public ConvexScheduleConfig Schedule { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public DateTimeOffset? NextExecutionTime { get; }
    public DateTimeOffset? LastExecutionTime { get; }
    public int ExecutionCount { get; }
    public ConvexJobError? LastError { get; }
    public JsonElement? LastResult { get; }
    public Dictionary<string, JsonElement>? Metadata { get; }
}
```

### Exception Types
```csharp
public class ConvexSchedulingException : Exception
{
    public SchedulingErrorType ErrorType { get; }
    public string? JobId { get; }
}

public enum SchedulingErrorType
{
    InvalidSchedule,
    JobNotFound,
    FunctionNotFound,
    QuotaExceeded,
    InvalidCronExpression,
    SchedulingFailed,
    CannotCancel,
    CannotUpdate
}
```

## Shared Dependencies
- **IHttpClientProvider**: For HTTP request execution
- **IConvexSerializer**: For JSON serialization/deserialization

## Architecture
- **SchedulingSlice**: Public facade implementing IConvexScheduler
- **SchedulingImplementation**: Internal implementation with direct HTTP calls
- **Uses**: `/api/mutation` for schedule/cancel/update, `/api/query` for getJob/listJobs

## Usage Examples

### One-Time Scheduling
```csharp
// Schedule after delay
var jobId = await client.SchedulingSlice.ScheduleAsync(
    "sendReminder",
    TimeSpan.FromHours(24)
);

// Schedule at specific time with args
var jobId = await client.SchedulingSlice.ScheduleAtAsync(
    "sendEmail",
    DateTimeOffset.UtcNow.AddDays(1),
    new { to = "user@example.com", subject = "Reminder" }
);
```

### Recurring Scheduling
```csharp
// Cron-based (daily at 9 AM)
var jobId = await client.SchedulingSlice.ScheduleRecurringAsync(
    "dailyReport",
    "0 9 * * *",
    timezone: "America/New_York"
);

// Interval-based (every 5 minutes)
var jobId = await client.SchedulingSlice.ScheduleIntervalAsync(
    "healthCheck",
    TimeSpan.FromMinutes(5)
);
```

### Job Management
```csharp
// Get job info
var job = await client.SchedulingSlice.GetJobAsync(jobId);
Console.WriteLine($"Status: {job.Status}, Next: {job.NextExecutionTime}");

// List all pending jobs
var pendingJobs = await client.SchedulingSlice.ListJobsAsync(
    status: ConvexJobStatus.Pending,
    limit: 50
);

// Cancel a job
var cancelled = await client.SchedulingSlice.CancelAsync(jobId);

// Update schedule
var newSchedule = ConvexScheduleConfig.Cron("0 10 * * *", "UTC");
await client.SchedulingSlice.UpdateScheduleAsync(jobId, newSchedule);
```

## Implementation Details
- Uses direct HTTP calls to `/api/mutation` for mutations (schedule, cancel, update)
- Uses direct HTTP calls to `/api/query` for queries (getJob, listJobs)
- Calls Convex functions: `scheduler:schedule`, `scheduler:cancel`, `scheduler:getJob`, `scheduler:listJobs`, `scheduler:updateSchedule`
- Comprehensive validation for schedule parameters, cron expressions, and intervals
- Time conversion to/from Unix milliseconds for API compatibility
- Supports complex schedule configurations with timezone handling

## Validation Rules
- Function name cannot be null or empty
- Scheduled time must be in the future (for one-time schedules)
- Interval must be at least 1 second
- End time must be after start time (for interval schedules)
- Cron expression cannot be null or empty
- Job ID cannot be null or empty
- List limit must be between 1 and 1000

## Error Handling
- Invalid schedule parameters → `SchedulingErrorType.InvalidSchedule`
- Invalid cron expression → `SchedulingErrorType.InvalidCronExpression`
- Job not found → `SchedulingErrorType.JobNotFound`
- Function not found → `SchedulingErrorType.FunctionNotFound`
- Quota exceeded → `SchedulingErrorType.QuotaExceeded`
- Cannot cancel/update → `SchedulingErrorType.CannotCancel`/`CannotUpdate`

## Owner
TBD
