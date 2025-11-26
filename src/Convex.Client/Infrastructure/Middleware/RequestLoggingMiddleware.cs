namespace Convex.Client.Infrastructure.Middleware;

/// <summary>
/// Middleware that logs and tracks statistics for all Convex requests.
/// Tracks request count, response times, and provides detailed statistics.
/// </summary>
/// <remarks>
/// Creates a new RequestLoggingMiddleware instance.
/// </remarks>
/// <param name="enabled">Whether request logging is enabled (default: true).</param>
public sealed class RequestLoggingMiddleware(bool enabled = true) : IConvexMiddleware
{
    private readonly bool _enabled = enabled;
    private int _requestCount;
    private readonly List<double> _responseTimes = [];
    private readonly object _lock = new();

    /// <inheritdoc/>
    public async Task<ConvexResponse> InvokeAsync(
        ConvexRequest request,
        ConvexRequestDelegate next)
    {
        if (!_enabled)
        {
            return await next(request);
        }

        var startTime = DateTimeOffset.UtcNow;
        var requestId = Interlocked.Increment(ref _requestCount);

        try
        {
            var response = await next(request);
            var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            lock (_lock)
            {
                _responseTimes.Add(elapsed);
                // Keep only last 100 response times
                if (_responseTimes.Count > 100)
                {
                    _responseTimes.RemoveAt(0);
                }
            }

            return response;
        }
        catch (Exception)
        {
            var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            lock (_lock)
            {
                _responseTimes.Add(elapsed);
                if (_responseTimes.Count > 100)
                {
                    _responseTimes.RemoveAt(0);
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Gets statistics about requests processed by this middleware.
    /// </summary>
    /// <returns>A tuple containing (requestCount, avgResponseTime, minResponseTime, maxResponseTime).</returns>
    public (int requestCount, double avgResponseTimeMs, double minResponseTimeMs, double maxResponseTimeMs) GetStats()
    {
        lock (_lock)
        {
            if (_responseTimes.Count == 0)
            {
                return (_requestCount, 0, 0, 0);
            }

            var avg = _responseTimes.Average();
            var min = _responseTimes.Min();
            var max = _responseTimes.Max();

            return (_requestCount, avg, min, max);
        }
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void ResetStats()
    {
        lock (_lock)
        {
            _requestCount = 0;
            _responseTimes.Clear();
        }
    }
}
