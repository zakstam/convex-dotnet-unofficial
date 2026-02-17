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
    /// Schedules a function to run once after a delay.
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
    /// Schedules a function to run once after a delay with typed arguments.
    /// </summary>
    Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run at a specific time.
    /// </summary>
    Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run at a specific time with typed arguments.
    /// </summary>
    Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run on a recurring cron schedule.
    /// </summary>
    Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run on a recurring cron schedule with typed arguments.
    /// </summary>
    Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull;

    /// <summary>
    /// Schedules a function to run at a fixed interval.
    /// </summary>
    Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a function to run at a fixed interval with typed arguments.
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
    /// Lists scheduled jobs with optional filters.
    /// </summary>
    Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the schedule configuration for an existing job.
    /// </summary>
    Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a scheduled Convex job.
/// </summary>
public class ConvexScheduledJob
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public required string Id { get; init; }
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public required string FunctionName { get; init; }
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public required ConvexJobStatus Status { get; init; }
    /// <summary>
    /// Gets or sets the arguments.
    /// </summary>
    public JsonElement? Arguments { get; init; }
    /// <summary>
    /// Gets or sets the schedule.
    /// </summary>
    public required ConvexScheduleConfig Schedule { get; init; }
    /// <summary>
    /// Gets or sets when the job was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>
    /// Gets or sets when the job was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }
    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTimeOffset? NextExecutionTime { get; init; }
    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTimeOffset? LastExecutionTime { get; init; }
    /// <summary>
    /// Gets or sets the execution count.
    /// </summary>
    public int ExecutionCount { get; init; }
    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public ConvexJobError? LastError { get; init; }
    /// <summary>
    /// Gets or sets the last result.
    /// </summary>
    public JsonElement? LastResult { get; init; }
    /// <summary>
    /// Gets or sets job metadata values.
    /// </summary>
    public Dictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>
/// Configuration for scheduling a job.
/// </summary>
public class ConvexScheduleConfig
{
    /// <summary>
    /// Gets or sets the schedule type.
    /// </summary>
    public required ConvexScheduleType Type { get; init; }
    /// <summary>
    /// Gets or sets the scheduled time.
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; init; }
    /// <summary>
    /// Gets or sets the cron expression.
    /// </summary>
    public string? CronExpression { get; init; }
    /// <summary>
    /// Gets or sets the interval duration.
    /// </summary>
    public TimeSpan? Interval { get; init; }
    /// <summary>
    /// Gets or sets the time zone used for cron scheduling.
    /// </summary>
    public string? Timezone { get; init; }
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }
    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }
    /// <summary>
    /// Gets or sets the maximum number of executions.
    /// </summary>
    public int? MaxExecutions { get; init; }

    /// <summary>
    /// Creates a one-time schedule configuration.
    /// </summary>
    public static ConvexScheduleConfig OneTime(DateTimeOffset scheduledTime) => new()
    {
        Type = ConvexScheduleType.OneTime,
        ScheduledTime = scheduledTime
    };

    /// <summary>
    /// Creates a cron-based schedule configuration.
    /// </summary>
    public static ConvexScheduleConfig Cron(string cronExpression, string timezone = "UTC") => new()
    {
        Type = ConvexScheduleType.Cron,
        CronExpression = cronExpression,
        Timezone = timezone
    };

    /// <summary>
    /// Creates an interval-based schedule configuration.
    /// </summary>
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
    /// <summary>
    /// Runs once at a single point in time.
    /// </summary>
    OneTime,
    /// <summary>
    /// Runs on a recurring cron schedule.
    /// </summary>
    Cron,
    /// <summary>
    /// Executes on a fixed interval.
    /// </summary>
    Interval
}

/// <summary>
/// Status of a scheduled job.
/// </summary>
public enum ConvexJobStatus
{
    /// <summary>
    /// The job is scheduled but has not started.
    /// </summary>
    Pending,
    /// <summary>
    /// The job is currently running.
    /// </summary>
    Running,
    /// <summary>
    /// The job completed successfully.
    /// </summary>
    Completed,
    /// <summary>
    /// The job failed during execution.
    /// </summary>
    Failed,
    /// <summary>
    /// The job was cancelled.
    /// </summary>
    Cancelled,
    /// <summary>
    /// The job is active and eligible to run.
    /// </summary>
    Active,
    /// <summary>
    /// The job is paused.
    /// </summary>
    Paused
}

/// <summary>
/// Error information for a failed job execution.
/// </summary>
public class ConvexJobError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? Code { get; init; }
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public required string Message { get; init; }
    /// <summary>
    /// Gets or sets the server stack trace, when available.
    /// </summary>
    public string? StackTrace { get; init; }
    /// <summary>
    /// Gets or sets when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
    /// <summary>
    /// Gets or sets additional structured error details.
    /// </summary>
    public JsonElement? Details { get; init; }
}

/// <summary>
/// Exception thrown when scheduling operations fail.
/// </summary>
public class ConvexSchedulingException(SchedulingErrorType errorType, string message, string? jobId = null, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the error type.
    /// </summary>
    public SchedulingErrorType ErrorType { get; } = errorType;
    /// <summary>
    /// Gets the related job ID, when available.
    /// </summary>
    public string? JobId { get; } = jobId;
}

/// <summary>
/// Types of scheduling errors.
/// </summary>
public enum SchedulingErrorType
{
    /// <summary>
    /// The provided schedule configuration is invalid.
    /// </summary>
    InvalidSchedule,
    /// <summary>
    /// The specified job was not found.
    /// </summary>
    JobNotFound,
    /// <summary>
    /// The specified function was not found.
    /// </summary>
    FunctionNotFound,
    /// <summary>
    /// A scheduling quota limit was exceeded.
    /// </summary>
    QuotaExceeded,
    /// <summary>
    /// The cron expression is invalid.
    /// </summary>
    InvalidCronExpression,
    /// <summary>
    /// Scheduling failed for an unspecified reason.
    /// </summary>
    SchedulingFailed,
    /// <summary>
    /// The job cannot be cancelled in its current state.
    /// </summary>
    CannotCancel,
    /// <summary>
    /// The job schedule cannot be updated in its current state.
    /// </summary>
    CannotUpdate
}
