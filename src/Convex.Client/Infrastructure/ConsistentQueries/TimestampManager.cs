using System.Text.Json;
using System.Text.Json.Serialization;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Http;

namespace Convex.Client.Infrastructure.ConsistentQueries;

/// <summary>
/// Manages timestamps for consistent query execution.
/// Timestamps allow multiple queries to be executed at the same point in time,
/// providing snapshot isolation.
/// </summary>
/// <remarks>
/// Creates a new TimestampManager.
/// </remarks>
public class TimestampManager(HttpClient httpClient, string deploymentUrl)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly string _deploymentUrl = deploymentUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(deploymentUrl));
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _timestampValidity = TimeSpan.FromSeconds(30);

    private string? _cachedTimestamp;
    private DateTime? _timestampAcquiredAt;
    private Task<string>? _pendingTimestampRequest;

    /// <summary>
    /// Gets the currently cached timestamp, if any.
    /// </summary>
    public string? CachedTimestamp => _cachedTimestamp;

    /// <summary>
    /// Gets whether a valid timestamp is currently cached.
    /// </summary>
    public bool HasValidTimestamp
    {
        get
        {
            if (_cachedTimestamp == null || _timestampAcquiredAt == null)
                return false;

            return DateTime.UtcNow - _timestampAcquiredAt.Value < _timestampValidity;
        }
    }

    /// <summary>
    /// Gets a timestamp for consistent query execution.
    /// If a valid timestamp is already cached, returns it immediately.
    /// Otherwise, fetches a new timestamp from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A timestamp string that can be used for consistent queries.</returns>
    public async Task<string> GetTimestampAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Return cached timestamp if still valid
            if (HasValidTimestamp)
            {
                return _cachedTimestamp!;
            }

            // If there's already a pending request, wait for it
            if (_pendingTimestampRequest != null)
            {
                try
                {
                    return await _pendingTimestampRequest;
                }
                catch
                {
                    // If the pending request failed, we'll create a new one below
                    _pendingTimestampRequest = null;
                }
            }

            // Create a new timestamp request
            _pendingTimestampRequest = FetchTimestampAsync(cancellationToken);

            try
            {
                var timestamp = await _pendingTimestampRequest;
                _cachedTimestamp = timestamp;
                _timestampAcquiredAt = DateTime.UtcNow;
                return timestamp;
            }
            finally
            {
                _pendingTimestampRequest = null;
            }
        }
        finally
        {
            _ = _lock.Release();
        }
    }

    /// <summary>
    /// Clears the cached timestamp, forcing the next consistent query to fetch a new one.
    /// </summary>
    public async Task ClearTimestampAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cachedTimestamp = null;
            _timestampAcquiredAt = null;
        }
        finally
        {
            _ = _lock.Release();
        }
    }

    private async Task<string> FetchTimestampAsync(CancellationToken cancellationToken)
    {
        var url = $"{_deploymentUrl}/api/query_ts";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
        // This matches convex-js behavior where HTTP 560 is treated as a valid response
        if (!ConvexHttpConstants.ShouldProcessResponse(response))
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConvexException($"Failed to fetch timestamp: {errorText}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseData = JsonSerializer.Deserialize<TimestampResponse>(responseContent);

        if (responseData == null || string.IsNullOrEmpty(responseData.Ts))
        {
            throw new ConvexException("Invalid timestamp response from server");
        }

        return responseData.Ts;
    }

    private class TimestampResponse
    {
        [JsonPropertyName("ts")]
        public string Ts { get; set; } = string.Empty;
    }
}

/// <summary>
/// Options for executing consistent queries.
/// </summary>
public class ConsistentQueryOptions
{
    /// <summary>
    /// Whether to force fetching a new timestamp even if one is cached.
    /// </summary>
    public bool ForceNewTimestamp { get; set; }

    /// <summary>
    /// The maximum age of a cached timestamp before it's considered stale.
    /// Default is 30 seconds (Convex's backend limit).
    /// </summary>
    public TimeSpan? TimestampValidity { get; set; }
}

