using System.Collections.Concurrent;
using System.Diagnostics;

namespace Convex.Client.Shared.Interceptors;

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
        public string FunctionName { get; init; } = string.Empty;
        public long RequestCount { get; set; }
        public long ErrorCount { get; set; }
        public double TotalDurationMs { get; set; }
        public double MinDurationMs { get; set; } = double.MaxValue;
        public double MaxDurationMs { get; set; }

        public double AverageDurationMs =>
            RequestCount > 0 ? TotalDurationMs / RequestCount : 0;

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
    /// Gets the total number of requests processed.
    /// </summary>
    public long TotalRequests => _totalRequests;

    /// <summary>
    /// Gets the total number of errors encountered.
    /// </summary>
    public long TotalErrors => _totalErrors;

    /// <summary>
    /// Gets the overall error rate across all functions.
    /// </summary>
    public double TotalErrorRate =>
        _totalRequests > 0 ? (double)_totalErrors / _totalRequests : 0;

    /// <summary>
    /// Gets metrics for a specific function.
    /// </summary>
    /// <param name="functionName">The function name to get metrics for.</param>
    /// <returns>The metrics for the function, or null if no requests have been made.</returns>
    public RequestMetrics? GetMetrics(string functionName) => _metricsByFunction.TryGetValue(functionName, out var metrics) ? metrics : null;

    /// <summary>
    /// Gets metrics for all functions.
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
