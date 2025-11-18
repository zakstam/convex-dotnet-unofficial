using Convex.Client.Shared.Builders;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Resilience;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Slices.Actions;

/// <summary>
/// Fluent builder for creating and executing Convex actions.
/// This implementation uses Shared infrastructure instead of CoreOperations.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the action.</typeparam>
internal sealed class ActionBuilder<TResult>(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    string functionName,
    Func<string, string, object?, TimeSpan?, CancellationToken, Task<TResult>>? middlewareExecutor = null,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IActionBuilder<TResult>
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly string _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    private readonly Func<string, string, object?, TimeSpan?, CancellationToken, Task<TResult>>? _middlewareExecutor = middlewareExecutor;
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private object? _args;
    private TimeSpan? _timeout;
    private Action<TResult>? _onSuccess;
    private Action<Exception>? _onError;
    private RetryPolicy? _retryPolicy;

    /// <inheritdoc/>
    public IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess)
    {
        _onSuccess = onSuccess;
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> OnError(Action<Exception> onError)
    {
        _onError = onError;
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new RetryPolicyBuilder();
        configure(builder);
        _retryPolicy = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IActionBuilder<TResult> WithRetry(RetryPolicy policy)
    {
        _retryPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        return this;
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var argsJson = _args != null ? _serializer.Serialize(_args) : "null";

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Action] Starting action execution: {FunctionName}, Args: {Args}, Timeout: {Timeout}, HasRetryPolicy: {HasRetryPolicy}",
                _functionName, argsJson, _timeout?.TotalMilliseconds ?? 0, _retryPolicy != null);
        }

        // If retry policy is configured, wrap execution in retry logic
        if (_retryPolicy != null)
        {
            var attempt = 0;
            Exception? lastException = null;

            while (true)
            {
                attempt++;
                try
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging) && attempt > 1)
                    {
                        _logger!.LogDebug("[Action] Retrying action execution: {FunctionName}, Attempt: {Attempt}",
                            _functionName, attempt);
                    }

                    var result = await ExecuteActionOnceAsync(cancellationToken);

                    stopwatch.Stop();
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        var resultJson = _serializer.Serialize(result);
                        _logger!.LogDebug("[Action] Action execution completed: {FunctionName}, Duration: {DurationMs}ms, Attempt: {Attempt}, ResultType: {ResultType}, Result: {Result}",
                            _functionName, stopwatch.Elapsed.TotalMilliseconds, attempt, typeof(TResult).Name, resultJson);
                    }

                    // Invoke success callback
                    _onSuccess?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogWarning(ex, "[Action] Action execution attempt {Attempt} failed: {FunctionName}, ErrorType: {ErrorType}, Message: {Message}",
                            attempt, _functionName, ex.GetType().Name, ex.Message);
                    }

                    if (!_retryPolicy.ShouldRetry(ex))
                    {
                        stopwatch.Stop();
                        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                        {
                            _logger!.LogError(ex, "[Action] Action execution failed after {Attempt} attempts: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                                attempt, _functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
                        }
                        // Invoke error callback
                        _onError?.Invoke(ex);
                        throw;
                    }

                    var delay = _retryPolicy.CalculateDelay(attempt);
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogDebug("[Action] Waiting {DelayMs}ms before retry attempt {Attempt} for {FunctionName}",
                            delay.TotalMilliseconds, attempt + 1, _functionName);
                    }
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // No retry policy - execute directly
        try
        {
            var result = await ExecuteActionOnceAsync(cancellationToken);

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var resultJson = _serializer.Serialize(result);
                _logger!.LogDebug("[Action] Action execution completed: {FunctionName}, Duration: {DurationMs}ms, ResultType: {ResultType}, Result: {Result}",
                    _functionName, stopwatch.Elapsed.TotalMilliseconds, typeof(TResult).Name, resultJson);
            }

            // Invoke success callback
            _onSuccess?.Invoke(result);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[Action] Action execution failed: {FunctionName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    _functionName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            // Invoke error callback
            _onError?.Invoke(ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default) => await ExecutionExtensions.ExecuteWithResultAsync(() => ExecuteAsync(cancellationToken));

    /// <summary>
    /// Executes the action once (without retry logic).
    /// </summary>
    private async Task<TResult> ExecuteActionOnceAsync(CancellationToken cancellationToken)
    {
        using var timeoutWrapper = TimeoutHelper.CreateTimeoutToken(_timeout, cancellationToken);

        try
        {
            // Execute action through middleware if available, otherwise directly
            if (_middlewareExecutor != null)
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Action] Executing action through middleware: {FunctionName}", _functionName);
                }
                return await _middlewareExecutor("action", _functionName, _args, _timeout, timeoutWrapper.Token);
            }

            // Execute action directly using Shared infrastructure
            return await ExecuteDirectAsync(timeoutWrapper.Token);
        }
        catch (OperationCanceledException) when (_timeout.HasValue)
        {
            // Timeout occurred - wrap in more specific exception
            // This catches both OperationCanceledException and TaskCanceledException
            // (since TaskCanceledException inherits from OperationCanceledException)
            var timeoutEx = new TimeoutException($"Action '{_functionName}' timed out after {_timeout}");
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogWarning(timeoutEx, "[Action] Action execution timed out: {FunctionName}, Timeout: {TimeoutMs}ms",
                    _functionName, _timeout?.TotalMilliseconds ?? 0);
            }
            throw timeoutEx;
        }
    }

    /// <summary>
    /// Executes the action directly using the HTTP provider.
    /// </summary>
    private async Task<TResult> ExecuteDirectAsync(CancellationToken cancellationToken)
    {
        var argsJson = _args != null ? _serializer.Serialize(_args) : "null";

        var request = ConvexRequestBuilder.BuildActionRequest(
            _httpProvider.DeploymentUrl,
            _functionName,
            _args,
            _serializer);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Action] Action request built: {FunctionName}, URL: {Url}, Method: {Method}, RequestBody: {RequestBody}",
                _functionName, request.RequestUri, request.Method, argsJson);
        }

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Action] Action response received: {FunctionName}, StatusCode: {StatusCode}, Headers: {Headers}",
                _functionName, response.StatusCode, string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
        }

        var result = await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            _functionName,
            "action",
            _serializer,
            cancellationToken);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var resultJson = _serializer.Serialize(result);
            _logger!.LogDebug("[Action] Action response parsed: {FunctionName}, ResultType: {ResultType}, Result: {Result}",
                _functionName, typeof(TResult).Name, resultJson);
        }

        return result;
    }
}
