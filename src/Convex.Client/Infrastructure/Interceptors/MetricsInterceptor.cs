using System.Collections.Concurrent;
using System.Diagnostics;

namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Example interceptor that collects metrics about Convex requests.
/// Tracks request counts, durations, and error rates.
/// </summary>
public sealed class MetricsInterceptor : IConvexInterceptor
{
    private readonly ConcurrentDictionary<string, RequestMetrics> _metricsByFunction = new();
    private long _totalRequests;
    private long _totalErrors;

    /// <summary>
    /// Metrics for a specific function.
    /// </summary>
    public sealed class RequestMetrics
    {
        /// <summary>
        /// Gets or sets the function name.
        /// </summary>
        public string FunctionName { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the request count.
        /// </summary>
        public long RequestCount { get; set; }
        /// <summary>
        /// Gets or sets the error count.
        /// </summary>
        public long ErrorCount { get; set; }
        /// <summary>
        /// Gets or sets the total duration ms.
        /// </summary>
        public double TotalDurationMs { get; set; }
        /// <summary>
        /// Gets or sets the min duration ms.
        /// </summary>
        public double MinDurationMs { get; set; } = double.MaxValue;
        /// <summary>
        /// Gets or sets the max duration ms.
        /// </summary>
        public double MaxDurationMs { get; set; }

        /// <summary>
        /// Gets the average duration ms.
        /// </summary>
        public double AverageDurationMs =>
            RequestCount > 0 ? TotalDurationMs / RequestCount : 0;

        /// <summary>
        /// Gets the error rate.
        /// </summary>
        public double ErrorRate =>
            RequestCount > 0 ? (double)ErrorCount / RequestCount : 0;
    }

    /// <inheritdoc/>
    public Task<ConvexRequestContext> BeforeRequestAsync(
        ConvexRequestContext context,
        CancellationToken cancellationToken = default)
    {
        // Store stopwatch in metadata for precise duration tracking
        context.Metadata["Stopwatch"] = Stopwatch.StartNew();

        _ = Interlocked.Increment(ref _totalRequests);

        return Task.FromResult(context);
    }

    /// <inheritdoc/>
    public Task<ConvexResponseContext> AfterResponseAsync(
        ConvexResponseContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = context.Request.Metadata.TryGetValue("Stopwatch", out var sw)
            ? (Stopwatch)sw
            : null;

        var durationMs = stopwatch?.Elapsed.TotalMilliseconds ?? context.Duration.TotalMilliseconds;

        var metrics = _metricsByFunction.GetOrAdd(
            context.Request.FunctionName,
            _ => new RequestMetrics { FunctionName = context.Request.FunctionName });

        metrics.RequestCount++;
        metrics.TotalDurationMs += durationMs;
        metrics.MinDurationMs = Math.Min(metrics.MinDurationMs, durationMs);
        metrics.MaxDurationMs = Math.Max(metrics.MaxDurationMs, durationMs);

        return Task.FromResult(context);
    }

    /// <inheritdoc/>
    public Task OnErrorAsync(
        ConvexErrorContext context,
        CancellationToken cancellationToken = default)
    {
        _ = Interlocked.Increment(ref _totalErrors);

        var metrics = _metricsByFunction.GetOrAdd(
            context.Request.FunctionName,
            _ => new RequestMetrics { FunctionName = context.Request.FunctionName });

        metrics.ErrorCount++;
        metrics.RequestCount++;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total requests.
    /// </summary>
    public long TotalRequests => _totalRequests;

    /// <summary>
    /// Gets the total errors.
    /// </summary>
    public long TotalErrors => _totalErrors;

    /// <summary>
    /// Gets the total error rate.
    /// </summary>
    public double TotalErrorRate =>
        _totalRequests > 0 ? (double)_totalErrors / _totalRequests : 0;

    /// <summary>
    /// Gets the get metrics.
    /// </summary>
    /// <param name="functionName">The function name to get metrics for.</param>
    /// <returns>The metrics for the function, or null if no requests have been made.</returns>
    public RequestMetrics? GetMetrics(string functionName) => _metricsByFunction.TryGetValue(functionName, out var metrics) ? metrics : null;

    /// <summary>
    /// Gets the get all metrics.
    /// </summary>
    /// <returns>A snapshot of metrics for all functions.</returns>
    public IReadOnlyDictionary<string, RequestMetrics> GetAllMetrics() => new Dictionary<string, RequestMetrics>(_metricsByFunction);

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    public void Reset()
    {
        _metricsByFunction.Clear();
        _ = Interlocked.Exchange(ref _totalRequests, 0);
        _ = Interlocked.Exchange(ref _totalErrors, 0);
    }
}
