using System.Text;
using System.Text.Json;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Slices.Scheduling;

/// <summary>
/// Internal implementation of scheduling operations using Shared infrastructure.
/// </summary>
internal class SchedulingImplementation(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    public async Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
        return await ScheduleAtAsync(functionName, scheduledTime, cancellationToken);
    }

    public async Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull
    {
        var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
        return await ScheduleAtAsync(functionName, scheduledTime, args, cancellationToken);
    }

    public async Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting job scheduling: FunctionName: {FunctionName}, ScheduledTime: {ScheduledTime}, ScheduleType: OneTime",
                functionName, scheduledTime);
        }

        try
        {
            ValidateScheduleParameters(functionName, scheduledTime);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "oneTime",
                    scheduledTime = scheduledTime.ToUnixTimeMilliseconds()
                }
            };

            var argsJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Args: {Args}",
                    functionName, argsJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, ScheduledTime: {ScheduledTime}, Duration: {DurationMs}ms",
                    jobId, functionName, scheduledTime, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull
    {
        var stopwatch = Stopwatch.StartNew();
        var argsJson = _serializer.Serialize(args);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting job scheduling with args: FunctionName: {FunctionName}, ScheduledTime: {ScheduledTime}, ScheduleType: OneTime, Args: {Args}",
                functionName, scheduledTime, argsJson);
        }

        try
        {
            ValidateScheduleParameters(functionName, scheduledTime);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "oneTime",
                    scheduledTime = scheduledTime.ToUnixTimeMilliseconds()
                },
                args
            };

            var requestJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Request: {Request}",
                    functionName, requestJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, ScheduledTime: {ScheduledTime}, Duration: {DurationMs}ms",
                    jobId, functionName, scheduledTime, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting recurring job scheduling: FunctionName: {FunctionName}, CronExpression: {CronExpression}, Timezone: {Timezone}, ScheduleType: Cron",
                functionName, cronExpression, timezone);
        }

        try
        {
            ValidateCronParameters(functionName, cronExpression, timezone);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "cron",
                    cronExpression,
                    timezone
                }
            };

            var argsJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Args: {Args}",
                    functionName, argsJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Recurring job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Recurring job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Recurring job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, CronExpression: {CronExpression}, Duration: {DurationMs}ms",
                    jobId, functionName, cronExpression, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule recurring job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Recurring job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull
    {
        var stopwatch = Stopwatch.StartNew();
        var argsJson = _serializer.Serialize(args);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting recurring job scheduling with args: FunctionName: {FunctionName}, CronExpression: {CronExpression}, Timezone: {Timezone}, ScheduleType: Cron, Args: {Args}",
                functionName, cronExpression, timezone, argsJson);
        }

        try
        {
            ValidateCronParameters(functionName, cronExpression, timezone);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "cron",
                    cronExpression,
                    timezone
                },
                args
            };

            var requestJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Request: {Request}",
                    functionName, requestJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Recurring job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Recurring job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Recurring job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, CronExpression: {CronExpression}, Duration: {DurationMs}ms",
                    jobId, functionName, cronExpression, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule recurring job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Recurring job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting interval job scheduling: FunctionName: {FunctionName}, Interval: {IntervalMs}ms, StartTime: {StartTime}, EndTime: {EndTime}, ScheduleType: Interval",
                functionName, interval.TotalMilliseconds, startTime?.ToString() ?? "null", endTime?.ToString() ?? "null");
        }

        try
        {
            ValidateIntervalParameters(functionName, interval, startTime, endTime);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "interval",
                    intervalMs = (long)interval.TotalMilliseconds,
                    startTime = startTime?.ToUnixTimeMilliseconds(),
                    endTime = endTime?.ToUnixTimeMilliseconds()
                }
            };

            var argsJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Args: {Args}",
                    functionName, argsJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Interval job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Interval job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Interval job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, Interval: {IntervalMs}ms, Duration: {DurationMs}ms",
                    jobId, functionName, interval.TotalMilliseconds, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule interval job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Interval job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull
    {
        var stopwatch = Stopwatch.StartNew();
        var argsJson = _serializer.Serialize(args);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting interval job scheduling with args: FunctionName: {FunctionName}, Interval: {IntervalMs}ms, StartTime: {StartTime}, EndTime: {EndTime}, ScheduleType: Interval, Args: {Args}",
                functionName, interval.TotalMilliseconds, startTime?.ToString() ?? "null", endTime?.ToString() ?? "null", argsJson);
        }

        try
        {
            ValidateIntervalParameters(functionName, interval, startTime, endTime);

            var requestArgs = new
            {
                functionName,
                schedule = new
                {
                    type = "interval",
                    intervalMs = (long)interval.TotalMilliseconds,
                    startTime = startTime?.ToUnixTimeMilliseconds(),
                    endTime = endTime?.ToUnixTimeMilliseconds()
                },
                args
            };

            var requestJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule request: FunctionName: {FunctionName}, Request: {Request}",
                    functionName, requestJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:schedule", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobId", out var jobIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from scheduler: missing jobId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Interval job scheduling failed: Invalid response, FunctionName: {FunctionName}, Response: {Response}, Duration: {DurationMs}ms",
                        functionName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobId = jobIdElement.GetString();
            if (string.IsNullOrEmpty(jobId))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid jobId returned from scheduler");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Interval job scheduling failed: Empty jobId, FunctionName: {FunctionName}, Duration: {DurationMs}ms",
                        functionName, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Interval job scheduled successfully: JobId: {JobId}, FunctionName: {FunctionName}, Interval: {IntervalMs}ms, Duration: {DurationMs}ms",
                    jobId, functionName, interval.TotalMilliseconds, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobId;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to schedule interval job", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Interval job scheduling failed: FunctionName: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting job cancellation: JobId: {JobId}", jobId);
        }

        try
        {
            ValidateJobId(jobId);

            var requestArgs = new { jobId };
            var response = await ExecuteMutationAsync<JsonElement>("scheduler:cancel", requestArgs, cancellationToken);

            var cancelled = response.TryGetProperty("cancelled", out var cancelledElement) && cancelledElement.GetBoolean();

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Job cancellation completed: JobId: {JobId}, Cancelled: {Cancelled}, Duration: {DurationMs}ms",
                    jobId, cancelled, stopwatch.Elapsed.TotalMilliseconds);
            }

            return cancelled;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to cancel job", jobId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Job cancellation failed: JobId: {JobId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    jobId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting job retrieval: JobId: {JobId}", jobId);
        }

        try
        {
            ValidateJobId(jobId);

            var requestArgs = new { jobId };
            var response = await ExecuteQueryAsync<JsonElement>("scheduler:getJob", requestArgs, cancellationToken);

            var job = ParseScheduledJob(response);

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Job retrieved successfully: JobId: {JobId}, FunctionName: {FunctionName}, Status: {Status}, Duration: {DurationMs}ms",
                    job.Id, job.FunctionName, job.Status, stopwatch.Elapsed.TotalMilliseconds);
            }

            return job;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to get job information", jobId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Job retrieval failed: JobId: {JobId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    jobId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting job list retrieval: Status: {Status}, FunctionName: {FunctionName}, Limit: {Limit}",
                status?.ToString() ?? "all", functionName ?? "all", limit);
        }

        try
        {
            ValidateListParameters(limit);

            var requestArgs = new
            {
                status = status?.ToString().ToLowerInvariant(),
                functionName,
                limit
            };

            var response = await ExecuteQueryAsync<JsonElement>("scheduler:listJobs", requestArgs, cancellationToken);

            if (!response.TryGetProperty("jobs", out var jobsElement))
            {
                stopwatch.Stop();
                var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                    "Invalid response from listJobs: missing jobs array");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[Scheduling] Job list retrieval failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var jobs = new List<ConvexScheduledJob>();

            foreach (var jobElement in jobsElement.EnumerateArray())
            {
                var job = ParseScheduledJob(jobElement);
                jobs.Add(job);
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Job list retrieved successfully: Count: {Count}, Duration: {DurationMs}ms",
                    jobs.Count, stopwatch.Elapsed.TotalMilliseconds);
            }

            return jobs;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to list jobs", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Job list retrieval failed: Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var scheduleJson = SerializeScheduleConfig(newSchedule);
        var scheduleJsonStr = JsonSerializer.Serialize(scheduleJson);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Scheduling] Starting schedule update: JobId: {JobId}, NewSchedule: {Schedule}",
                jobId, scheduleJsonStr);
        }

        try
        {
            ValidateJobId(jobId);
            ValidateScheduleConfig(newSchedule);

            var requestArgs = new
            {
                jobId,
                schedule = scheduleJson
            };

            var requestJson = _serializer.Serialize(requestArgs);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Update schedule request: JobId: {JobId}, Request: {Request}",
                    jobId, requestJson);
            }

            var response = await ExecuteMutationAsync<JsonElement>("scheduler:updateSchedule", requestArgs, cancellationToken);

            var updated = response.TryGetProperty("updated", out var updatedElement) && updatedElement.GetBoolean();

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Scheduling] Schedule update completed: JobId: {JobId}, Updated: {Updated}, Duration: {DurationMs}ms",
                    jobId, updated, stopwatch.Elapsed.TotalMilliseconds);
            }

            return updated;
        }
        catch (Exception ex) when (ex is not ConvexSchedulingException)
        {
            stopwatch.Stop();
            var error = new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Failed to update job schedule", jobId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Scheduling] Schedule update failed: JobId: {JobId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    jobId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    #region Private Helper Methods - HTTP Execution

    private async Task<TResult> ExecuteMutationAsync<TResult>(string functionName, object args, CancellationToken cancellationToken)
    {
        var url = $"{_httpProvider.DeploymentUrl}/api/mutation";

        var requestBody = new
        {
            path = functionName,
            format = "convex_encoded_json",
            args = new[] { args }
        };

        var json = _serializer.Serialize(requestBody);
        if (json == null)
        {
            throw new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                $"Failed to serialize mutation request body for function '{functionName}'. Serializer returned null.",
                functionName);
        }
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
        // This matches convex-js behavior where HTTP 560 is treated as a valid response
        ConvexHttpConstants.EnsureConvexResponse(response);

        var responseJson = await response.ReadContentAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status))
        {
            var statusValue = status.GetString();
            if (statusValue == "success" && root.TryGetProperty("value", out var value))
            {
                var result = _serializer.Deserialize<TResult>(value.GetRawText());
                if (result == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize mutation result for function '{functionName}'");
                }
                return result;
            }
            else if (statusValue == "error")
            {
                var errorMessage = root.TryGetProperty("errorMessage", out var errMsg)
                    ? errMsg.GetString()
                    : "Unknown mutation error";
                throw new InvalidOperationException($"Mutation '{functionName}' failed: {errorMessage}");
            }
        }

        throw new InvalidOperationException($"Invalid response format from mutation '{functionName}'");
    }

    private async Task<TResult> ExecuteQueryAsync<TResult>(string functionName, object args, CancellationToken cancellationToken)
    {
        var url = $"{_httpProvider.DeploymentUrl}/api/query";

        var requestBody = new
        {
            path = functionName,
            format = "convex_encoded_json",
            args = new[] { args }
        };

        var json = _serializer.Serialize(requestBody);
        if (json == null)
        {
            throw new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                $"Failed to serialize mutation request body for function '{functionName}'. Serializer returned null.",
                functionName);
        }
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
        // This matches convex-js behavior where HTTP 560 is treated as a valid response
        ConvexHttpConstants.EnsureConvexResponse(response);

        var responseJson = await response.ReadContentAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status))
        {
            var statusValue = status.GetString();
            if (statusValue == "success" && root.TryGetProperty("value", out var value))
            {
                var result = _serializer.Deserialize<TResult>(value.GetRawText());
                if (result == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize query result for function '{functionName}'");
                }
                return result;
            }
            else if (statusValue == "error")
            {
                var errorMessage = root.TryGetProperty("errorMessage", out var errMsg)
                    ? errMsg.GetString()
                    : "Unknown query error";
                throw new InvalidOperationException($"Query '{functionName}' failed: {errorMessage}");
            }
        }

        throw new InvalidOperationException($"Invalid response format from query '{functionName}'");
    }

    #endregion

    #region Private Helper Methods - Validation

    private static void ValidateScheduleParameters(string functionName, DateTimeOffset scheduledTime)
    {
        if (string.IsNullOrEmpty(functionName))
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Function name cannot be null or empty");

        if (scheduledTime <= DateTimeOffset.UtcNow)
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Scheduled time must be in the future");
    }

    private static void ValidateCronParameters(string functionName, string cronExpression, string timezone)
    {
        if (string.IsNullOrEmpty(functionName))
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Function name cannot be null or empty");

        if (string.IsNullOrEmpty(cronExpression))
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidCronExpression,
                "Cron expression cannot be null or empty");

        if (string.IsNullOrEmpty(timezone))
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Timezone cannot be null or empty");
    }

    private static void ValidateIntervalParameters(string functionName, TimeSpan interval, DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        if (string.IsNullOrEmpty(functionName))
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Function name cannot be null or empty");

        if (interval <= TimeSpan.Zero)
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Interval must be greater than zero");

        if (interval.TotalMilliseconds < 1000) // Minimum 1 second
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Interval must be at least 1 second");

        if (endTime.HasValue && startTime.HasValue && endTime <= startTime)
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "End time must be after start time");
    }

    private static void ValidateJobId(string jobId)
    {
        if (string.IsNullOrEmpty(jobId))
            throw new ConvexSchedulingException(SchedulingErrorType.JobNotFound,
                "Job ID cannot be null or empty");
    }

    private static void ValidateListParameters(int limit)
    {
        if (limit <= 0 || limit > 1000)
            throw new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed,
                "Limit must be between 1 and 1000");
    }

    private static void ValidateScheduleConfig(ConvexScheduleConfig schedule)
    {
        if (schedule == null)
            throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                "Schedule configuration cannot be null");

        switch (schedule.Type)
        {
            case ConvexScheduleType.OneTime when !schedule.ScheduledTime.HasValue:
                throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                    "One-time schedule must have a scheduled time");

            case ConvexScheduleType.Cron when string.IsNullOrEmpty(schedule.CronExpression):
                throw new ConvexSchedulingException(SchedulingErrorType.InvalidCronExpression,
                    "Cron schedule must have a cron expression");

            case ConvexScheduleType.Interval when !schedule.Interval.HasValue:
                throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                    "Interval schedule must have an interval");
        }
    }

    #endregion

    #region Private Helper Methods - Serialization & Parsing

    private static object SerializeScheduleConfig(ConvexScheduleConfig schedule)
    {
        return schedule.Type switch
        {
            ConvexScheduleType.OneTime => new
            {
                type = "oneTime",
                scheduledTime = schedule.ScheduledTime!.Value.ToUnixTimeMilliseconds()
            },
            ConvexScheduleType.Cron => new
            {
                type = "cron",
                cronExpression = schedule.CronExpression!,
                timezone = schedule.Timezone ?? "UTC"
            },
            ConvexScheduleType.Interval => new
            {
                type = "interval",
                intervalMs = (long)schedule.Interval!.Value.TotalMilliseconds,
                startTime = schedule.StartTime?.ToUnixTimeMilliseconds(),
                endTime = schedule.EndTime?.ToUnixTimeMilliseconds()
            },
            _ => throw new ConvexSchedulingException(SchedulingErrorType.InvalidSchedule,
                $"Unknown schedule type: {schedule.Type}")
        };
    }

    private static ConvexScheduledJob ParseScheduledJob(JsonElement jobElement)
    {
        var id = jobElement.GetProperty("id").GetString()!;
        var functionName = jobElement.GetProperty("functionName").GetString()!;
        var status = Enum.Parse<ConvexJobStatus>(jobElement.GetProperty("status").GetString()!, true);

        var scheduleElement = jobElement.GetProperty("schedule");
        var scheduleType = Enum.Parse<ConvexScheduleType>(scheduleElement.GetProperty("type").GetString()!, true);

        var schedule = scheduleType switch
        {
            ConvexScheduleType.OneTime => ConvexScheduleConfig.OneTime(
                DateTimeOffset.FromUnixTimeMilliseconds(scheduleElement.GetProperty("scheduledTime").GetInt64())),
            ConvexScheduleType.Cron => ConvexScheduleConfig.Cron(
                scheduleElement.GetProperty("cronExpression").GetString()!,
                scheduleElement.TryGetProperty("timezone", out var tzElement) ? tzElement.GetString()! : "UTC"),
            ConvexScheduleType.Interval => ConvexScheduleConfig.CreateInterval(
                TimeSpan.FromMilliseconds(scheduleElement.GetProperty("intervalMs").GetInt64()),
                scheduleElement.TryGetProperty("startTime", out var startElement) ? DateTimeOffset.FromUnixTimeMilliseconds(startElement.GetInt64()) : null,
                scheduleElement.TryGetProperty("endTime", out var endElement) ? DateTimeOffset.FromUnixTimeMilliseconds(endElement.GetInt64()) : null),
            _ => throw new ConvexSchedulingException(SchedulingErrorType.SchedulingFailed, $"Unknown schedule type: {scheduleType}")
        };

        return new ConvexScheduledJob
        {
            Id = id,
            FunctionName = functionName,
            Status = status,
            Arguments = jobElement.TryGetProperty("arguments", out var argsElement) ? argsElement : null,
            Schedule = schedule,
            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(jobElement.GetProperty("createdAt").GetInt64()),
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(jobElement.GetProperty("updatedAt").GetInt64()),
            NextExecutionTime = jobElement.TryGetProperty("nextExecutionTime", out var nextElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(nextElement.GetInt64())
                : null,
            LastExecutionTime = jobElement.TryGetProperty("lastExecutionTime", out var lastElement)
                ? DateTimeOffset.FromUnixTimeMilliseconds(lastElement.GetInt64())
                : null,
            ExecutionCount = jobElement.TryGetProperty("executionCount", out var countElement) ? countElement.GetInt32() : 0,
            LastError = jobElement.TryGetProperty("lastError", out var errorElement) ? ParseJobError(errorElement) : null,
            LastResult = jobElement.TryGetProperty("lastResult", out var resultElement) ? resultElement : null,
            Metadata = jobElement.TryGetProperty("metadata", out var metaElement) ? ParseMetadata(metaElement) : null
        };
    }

    private static ConvexJobError? ParseJobError(JsonElement errorElement)
    {
        if (errorElement.ValueKind == JsonValueKind.Null)
            return null;

        return new ConvexJobError
        {
            Code = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null,
            Message = errorElement.GetProperty("message").GetString()!,
            StackTrace = errorElement.TryGetProperty("stackTrace", out var stackElement) ? stackElement.GetString() : null,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(errorElement.GetProperty("timestamp").GetInt64()),
            Details = errorElement.TryGetProperty("details", out var detailsElement) ? detailsElement : null
        };
    }

    private static Dictionary<string, JsonElement>? ParseMetadata(JsonElement metaElement)
    {
        if (metaElement.ValueKind == JsonValueKind.Null || metaElement.ValueKind == JsonValueKind.Undefined)
            return null;

        var metadata = new Dictionary<string, JsonElement>();

        foreach (var property in metaElement.EnumerateObject())
        {
            metadata[property.Name] = property.Value;
        }

        return metadata.Count > 0 ? metadata : null;
    }

    #endregion
}
