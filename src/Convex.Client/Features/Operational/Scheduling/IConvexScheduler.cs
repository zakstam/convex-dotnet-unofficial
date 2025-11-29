using System.Text.Json;

namespace Convex.Client.Features.Operational.Scheduling;

/// <summary>
/// Interface for Convex scheduling operations.
/// Provides capabilities to schedule functions for delayed execution and recurring tasks.
/// Use this to schedule one-time jobs, recurring cron jobs, or interval-based tasks.
/// </summary>
/// <remarks>
/// <para>
/// Scheduling allows you to run Convex functions at specific times or intervals:
/// <list type="bullet">
/// <item><strong>One-time</strong> - Run a function once after a delay or at a specific time</item>
/// <item><strong>Cron</strong> - Run a function on a recurring schedule (e.g., daily at 9 AM)</item>
/// <item><strong>Interval</strong> - Run a function at regular intervals (e.g., every 5 minutes)</item>
/// </list>
/// </para>
/// <para>
/// Scheduled jobs return a job ID that can be used to cancel or query the job status.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Schedule a one-time job
/// var jobId = await client.Scheduler.ScheduleAsync(
///     functionName: "functions/sendReminder",
///     delay: TimeSpan.FromHours(24),
///     args: new { userId = "user123" }
/// );
///
/// // Schedule a recurring daily job
/// var dailyJobId = await client.Scheduler.ScheduleRecurringAsync(
///     functionName: "functions/sendDailyDigest",
///     cronExpression: "0 9 * * *", // Daily at 9 AM
///     timezone: "America/New_York",
///     args: new { userId = "user123" }
/// );
/// </code>
/// </example>
/// <seealso cref="SchedulingSlice"/>
public interface IConvexScheduler
{
    /// <summary>
    /// Schedules a function to run after a specified delay.
    /// The function will execute once after the delay period elapses.
    /// </summary>
    /// <param name="functionName">The name of the Convex function to schedule (e.g., "functions/sendReminder"). Function names match file paths: `convex/functions/sendReminder.ts` becomes `"functions/sendReminder"`.</param>
    /// <param name="delay">The delay before the function executes. Must be greater than zero.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the job ID that can be used to cancel or query the job.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delay"/> is less than or equal to zero.</exception>
    /// <exception cref="ConvexSchedulingException">Thrown when scheduling fails (function not found, invalid schedule, etc.).</exception>
    /// <remarks>
    /// The function will execute once after the specified delay. Use <see cref="ScheduleRecurringAsync{TArgs}(string, string, TArgs, string, CancellationToken)"/>
    /// for recurring jobs or <see cref="ScheduleIntervalAsync{TArgs}(string, TimeSpan, TArgs, DateTimeOffset?, DateTimeOffset?, CancellationToken)"/>
    /// for interval-based jobs.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Schedule a reminder to send in 1 hour
    /// var jobId = await client.Scheduler.ScheduleAsync(
    ///     functionName: "functions/sendReminder",
    ///     delay: TimeSpan.FromHours(1)
    /// );
    ///
    /// // Schedule an email to send tomorrow
    /// var emailJobId = await client.Scheduler.ScheduleAsync(
    ///     functionName: "functions/sendEmail",
    ///     delay: TimeSpan.FromDays(1)
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="ScheduleAsync{TArgs}(string, TimeSpan, TArgs, CancellationToken)"/>
    /// <seealso cref="ScheduleAtAsync(string, DateTimeOffset, CancellationToken)"/>
    Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run after a specified delay with strongly-typed arguments.
    /// </summary>
    Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run at a specific time.
    /// </summary>
    Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run at a specific time with strongly-typed arguments.
    /// </summary>
    Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run on a recurring schedule using cron expression.
    /// </summary>
    Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run on a recurring schedule using cron expression with strongly-typed arguments.
    /// </summary>
    Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run at regular intervals.
    /// </summary>
    Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run at regular intervals with strongly-typed arguments.
    /// </summary>
    Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Cancels a scheduled job.
    /// </summary>
    Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a scheduled job.
    /// </summary>
    Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all scheduled jobs.
    /// </summary>
    Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the schedule of an existing job.
    /// </summary>
    Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents information about a scheduled job.
/// </summary>
public class ConvexScheduledJob
{
    public required string Id { get; init; }
    public required string FunctionName { get; init; }
    public required ConvexJobStatus Status { get; init; }
    public JsonElement? Arguments { get; init; }
    public required ConvexScheduleConfig Schedule { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? NextExecutionTime { get; init; }
    public DateTimeOffset? LastExecutionTime { get; init; }
    public int ExecutionCount { get; init; }
    public ConvexJobError? LastError { get; init; }
    public JsonElement? LastResult { get; init; }
    public Dictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>
/// Configuration for scheduling a job.
/// </summary>
public class ConvexScheduleConfig
{
    public required ConvexScheduleType Type { get; init; }
    public DateTimeOffset? ScheduledTime { get; init; }
    public string? CronExpression { get; init; }
    public TimeSpan? Interval { get; init; }
    public string? Timezone { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public int? MaxExecutions { get; init; }

    public static ConvexScheduleConfig OneTime(DateTimeOffset scheduledTime) => new()
    {
        Type = ConvexScheduleType.OneTime,
        ScheduledTime = scheduledTime
    };

    public static ConvexScheduleConfig Cron(string cronExpression, string timezone = "UTC") => new()
    {
        Type = ConvexScheduleType.Cron,
        CronExpression = cronExpression,
        Timezone = timezone
    };

    public static ConvexScheduleConfig CreateInterval(TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null) => new()
    {
        Type = ConvexScheduleType.Interval,
        Interval = interval,
        StartTime = startTime,
        EndTime = endTime
    };
}

/// <summary>
/// Types of schedules supported by Convex.
/// </summary>
public enum ConvexScheduleType
{
    OneTime,
    Cron,
    Interval
}

/// <summary>
/// Status of a scheduled job.
/// </summary>
public enum ConvexJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Active,
    Paused
}

/// <summary>
/// Error information for a failed job execution.
/// </summary>
public class ConvexJobError
{
    public string? Code { get; init; }
    public required string Message { get; init; }
    public string? StackTrace { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonElement? Details { get; init; }
}

/// <summary>
/// Exception thrown when scheduling operations fail.
/// </summary>
public class ConvexSchedulingException(SchedulingErrorType errorType, string message, string? jobId = null, Exception? innerException = null) : Exception(message, innerException)
{
    public SchedulingErrorType ErrorType { get; } = errorType;
    public string? JobId { get; } = jobId;
}

/// <summary>
/// Types of scheduling errors.
/// </summary>
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
