using Convex.Client.Shared.Builders;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Resilience;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Slices.Queries;

/// <summary>
/// Fluent builder for creating and executing Convex queries.
/// This implementation uses Shared infrastructure instead of CoreOperations.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query.</typeparam>
internal sealed class QueryBuilder<TResult>(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    string functionName,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IQueryBuilder<TResult>
{
    /// <summary>
    /// Maximum safe retry count to prevent infinite loops.
    /// </summary>
    private const int MaxSafeRetries = 1000;

    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly string _functionName = functionName ?? throw new ArgumentException("Function name cannot be null.", nameof(functionName));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private object? _args;
    private TimeSpan? _timeout;
    private Action<Exception>? _onError;
    private RetryPolicy? _retryPolicy;

    /// <inheritdoc/>
    public IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> WithTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be negative.");
        }
        if (timeout == TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be zero. Omit WithTimeout() to use default timeout, or specify a positive timeout value.");
        }
        _timeout = timeout;
        return this;
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> IncludeMetadata() =>
        // TODO: Implement metadata support - planned for future version
        // Will require updating HTTP request to request metadata
        // and returning a richer result type with execution details
        throw new NotImplementedException("Metadata support is not yet implemented. Use standard queries without metadata for now.");

    /// <inheritdoc/>
    [Obsolete("This API is experimental: it may change or disappear. Use standard queries instead.")]
    public IQueryBuilder<TResult> UseConsistency(long timestamp)
    {
        if (timestamp < 0)
            throw new ArgumentException("Timestamp must be non-negative.", nameof(timestamp));

        // TODO: Implement consistent query support - planned for future version
        // Will require adding timestamp to HTTP request headers or query params
        throw new NotImplementedException("Consistent queries are not yet implemented. This API is experimental.");
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> Cached(TimeSpan cacheDuration)
    {
        if (cacheDuration <= TimeSpan.Zero)
            throw new ArgumentException("Cache duration must be positive.", nameof(cacheDuration));

        // TODO: Implement caching - planned for future version
        // Will require coordination with ConvexClient's QueryCache
        // This is a signal to the facade - caching should be handled at ConvexClient level
        throw new NotImplementedException("Query caching is not yet implemented. Use the ConvexClient caching slice instead.");
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> OnError(Action<Exception> onError)
    {
        _onError = onError;
        return this;
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var builder = new RetryPolicyBuilder();
        configure(builder);
        var policy = builder.Build();
        
        // Validate MaxRetries to prevent infinite loops
        if (policy.MaxRetries > MaxSafeRetries)
        {
            throw new ArgumentException(
                $"MaxRetries cannot exceed {MaxSafeRetries} to prevent infinite loops. " +
                $"Requested value: {policy.MaxRetries}.",
                nameof(configure));
        }
        
        _retryPolicy = policy;
        return this;
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> WithRetry(RetryPolicy policy)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));
        
        // Validate MaxRetries to prevent infinite loops
        if (policy.MaxRetries > MaxSafeRetries)
        {
            throw new ArgumentException(
                $"MaxRetries cannot exceed {MaxSafeRetries} to prevent infinite loops. " +
                $"Requested value: {policy.MaxRetries}.",
                nameof(policy));
        }
        
        _retryPolicy = policy;
        return this;
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_retryPolicy != null)
        {
            return await ExecuteWithRetryAsync(cancellationToken);
        }

        try
        {
            return await ExecuteQueryOnceAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Query] Query execution failed: {FunctionName}, ErrorType: {ErrorType}, Message: {Message}",
                    _functionName, ex.GetType().Name, ex.Message);
            }
            SafeInvokeErrorCallback(ex);
            throw;
        }
    }

    /// <summary>
    /// Safely invokes the error callback, catching any exceptions to prevent masking the original error.
    /// </summary>
    private void SafeInvokeErrorCallback(Exception ex)
    {
        if (_onError != null)
        {
            try
            {
                _onError(ex);
            }
            catch (Exception callbackEx)
            {
                // Log the callback exception but don't mask the original exception
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(callbackEx, "[Query] Error callback threw an exception for {FunctionName}: {CallbackErrorType}, {CallbackMessage}",
                        _functionName, callbackEx.GetType().Name, callbackEx.Message);
                }
            }
        }
    }

    /// <summary>
    /// Executes the query with retry policy.
    /// </summary>
    private async Task<TResult> ExecuteWithRetryAsync(CancellationToken cancellationToken)
    {
        // Safety check: Ensure MaxRetries is within safe bounds (defense in depth)
        if (_retryPolicy!.MaxRetries > MaxSafeRetries)
        {
            throw new InvalidOperationException(
                $"Retry policy has MaxRetries ({_retryPolicy.MaxRetries}) exceeding safe limit ({MaxSafeRetries}). " +
                "This should have been caught during policy configuration.");
        }

        var attempt = 0;
        Exception? lastException = null;

        while (true)
        {
            attempt++;
            try
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging) && attempt > 1)
                {
                    _logger!.LogDebug("[Query] Retrying query execution: {FunctionName}, Attempt: {Attempt}",
                        _functionName, attempt);
                }

                return await ExecuteQueryOnceAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning(ex, "[Query] Query execution attempt {Attempt} failed: {FunctionName}, ErrorType: {ErrorType}",
                        attempt, _functionName, ex.GetType().Name);
                }

                // Check if cancellation was requested FIRST - don't retry if cancelled
                if (cancellationToken.IsCancellationRequested)
                {
                    var cancelEx = new OperationCanceledException(
                        $"Query '{_functionName}' was canceled during retry attempt {attempt}.",
                        ex,
                        cancellationToken);
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogWarning(cancelEx, "[Query] Query execution canceled during retry: {FunctionName}, Attempt: {Attempt}",
                            _functionName, attempt);
                    }
                    SafeInvokeErrorCallback(cancelEx);
                    throw cancelEx;
                }

                // Check if we've exceeded the maximum number of retries
                // attempt includes the initial attempt, so if MaxRetries is 2, we allow: initial + 2 retries = 3 total attempts
                if (attempt > _retryPolicy.MaxRetries)
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Query] Query execution failed after {Attempt} attempts (max retries exceeded): {FunctionName}",
                            attempt, _functionName);
                    }
                    SafeInvokeErrorCallback(ex);
                    throw;
                }

                // Check if exception type should trigger a retry
                if (!_retryPolicy!.ShouldRetry(ex))
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Query] Query execution failed after {Attempt} attempts: {FunctionName}",
                            attempt, _functionName);
                    }
                    SafeInvokeErrorCallback(ex);
                    throw;
                }

                var delay = _retryPolicy.CalculateDelay(attempt);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Query] Waiting {DelayMs}ms before retry attempt {Attempt} for {FunctionName}",
                        delay.TotalMilliseconds, attempt + 1, _functionName);
                }

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException delayCancelEx) when (delayCancelEx.CancellationToken == cancellationToken)
                {
                    // Cancellation occurred during delay - wrap with context and rethrow
                    var cancelEx = new OperationCanceledException(
                        $"Query '{_functionName}' was canceled during retry delay (attempt {attempt}).",
                        ex,
                        cancellationToken);
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogWarning(cancelEx, "[Query] Query execution canceled during retry delay: {FunctionName}, Attempt: {Attempt}",
                            _functionName, attempt);
                    }
                    SafeInvokeErrorCallback(cancelEx);
                    throw cancelEx;
                }
            }
        }
    }

    /// <summary>
    /// Executes the query once (without retry logic) with timeout handling.
    /// </summary>
    private async Task<TResult> ExecuteQueryOnceAsync(CancellationToken cancellationToken)
    {
        using var timeoutWrapper = TimeoutHelper.CreateTimeoutToken(_timeout, cancellationToken);

        try
        {
            return await ExecuteDirectAsync(timeoutWrapper.Token);
        }
        catch (OperationCanceledException ex)
        {
            // Check if timeout was configured and triggered
            // If timeout was configured, prefer TimeoutException over OperationCanceledException
            // This handles cases where timeout CTS cancels, even if user cancellation also occurred
            if (_timeout.HasValue && timeoutWrapper.WasTimeout)
            {
                // Timeout occurred - wrap in more specific exception
                var timeoutEx = new TimeoutException(
                    $"Query '{_functionName}' timed out after {_timeout}.",
                    ex);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning(timeoutEx, "[Query] Query execution timed out: {FunctionName}, Timeout: {TimeoutMs}ms",
                        _functionName, _timeout.Value.TotalMilliseconds);
                }
                throw timeoutEx;
            }
            
            // User cancellation occurred (or cancellation without timeout configured)
            // Preserve the original exception with context
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogWarning(ex, "[Query] Query execution was canceled: {FunctionName}",
                    _functionName);
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default) => await ExecutionExtensions.ExecuteWithResultAsync(() => ExecuteAsync(cancellationToken));

    /// <summary>
    /// Executes the query directly using the HTTP provider.
    /// </summary>
    private async Task<TResult> ExecuteDirectAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var argsJson = _args != null ? _serializer.Serialize(_args) : "null";

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Query] Starting query execution: {FunctionName}, Args: {Args}, Timeout: {Timeout}",
                _functionName, argsJson, _timeout?.TotalMilliseconds ?? 0);
        }

        try
        {
            var request = ConvexRequestBuilder.BuildQueryRequest(
                _httpProvider.DeploymentUrl,
                _functionName,
                _args,
                _serializer);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Query] Query request built: {FunctionName}, URL: {Url}, Method: {Method}, RequestBody: {RequestBody}",
                    _functionName, request.RequestUri, request.Method, argsJson);
            }

            var response = await _httpProvider.SendAsync(request, cancellationToken);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Query] Query response received: {FunctionName}, StatusCode: {StatusCode}, Headers: {Headers}",
                    _functionName, response.StatusCode, string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
            }

            // Use ConvexResponseParser to handle standard Convex response format (status/value/errorMessage wrapper)
            // This also handles STATUS_CODE_UDF_FAILED (560) as valid response with error data
            var result = await ConvexResponseParser.ParseResponseAsync<TResult>(
                response,
                _functionName,
                "query",
                _serializer,
                cancellationToken);

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Query] Query execution completed: {FunctionName}, Duration: {DurationMs}ms, ResultType: {ResultType}",
                    _functionName, stopwatch.Elapsed.TotalMilliseconds, typeof(TResult).Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Query] Query execution failed: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    _functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw;
        }
    }
}
