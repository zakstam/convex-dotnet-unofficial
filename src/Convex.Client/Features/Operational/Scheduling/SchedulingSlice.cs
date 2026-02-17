using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Operational.Scheduling;

/// <summary>
/// Scheduling slice - provides delayed and recurring function execution capabilities.
/// This is a self-contained vertical slice that handles all scheduling functionality.
/// </summary>
public class SchedulingSlice(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false) : IConvexScheduler
{
    private readonly SchedulingImplementation _implementation = new SchedulingImplementation(httpProvider, serializer, logger, enableDebugLogging);

    /// <summary>
    /// Schedules the operation.
    /// </summary>
    public Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default)
        => _implementation.ScheduleAsync(functionName, delay, cancellationToken);

    /// <summary>
    /// Schedules the operation.
    /// </summary>
    public Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull
        => _implementation.ScheduleAsync(functionName, delay, args, cancellationToken);

    /// <summary>
    /// Schedules at.
    /// </summary>
    public Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default)
        => _implementation.ScheduleAtAsync(functionName, scheduledTime, cancellationToken);

    /// <summary>
    /// Schedules at.
    /// </summary>
    public Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull
        => _implementation.ScheduleAtAsync(functionName, scheduledTime, args, cancellationToken);

    /// <summary>
    /// Schedules recurring.
    /// </summary>
    public Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default)
        => _implementation.ScheduleRecurringAsync(functionName, cronExpression, timezone, cancellationToken);

    /// <summary>
    /// Schedules recurring.
    /// </summary>
    public Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull
        => _implementation.ScheduleRecurringAsync(functionName, cronExpression, args, timezone, cancellationToken);

    /// <summary>
    /// Schedules interval.
    /// </summary>
    public Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default)
        => _implementation.ScheduleIntervalAsync(functionName, interval, startTime, endTime, cancellationToken);

    /// <summary>
    /// Schedules interval.
    /// </summary>
    public Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull
        => _implementation.ScheduleIntervalAsync(functionName, interval, args, startTime, endTime, cancellationToken);

    /// <summary>
    /// Cancels the operation.
    /// </summary>
    public Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)
        => _implementation.CancelAsync(jobId, cancellationToken);

    /// <summary>
    /// Gets job.
    /// </summary>
    public Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        => _implementation.GetJobAsync(jobId, cancellationToken);

    /// <summary>
    /// Lists jobs.
    /// </summary>
    public Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default)
        => _implementation.ListJobsAsync(status, functionName, limit, cancellationToken);

    /// <summary>
    /// Updates schedule.
    /// </summary>
    public Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default)
        => _implementation.UpdateScheduleAsync(jobId, newSchedule, cancellationToken);
}
