namespace Convex.Client.Infrastructure.Internal.Connection;

/// <summary>
/// Defines the reconnection strategy for WebSocket connections.
/// Supports exponential backoff with jitter.
/// </summary>
public sealed class ReconnectionPolicy
{
#if NETSTANDARD2_1
    private static readonly Random _random = new Random();
#endif
    private int _attemptCount;

    /// <summary>
    /// Gets or sets the max attempts.
    /// Set to -1 for unlimited attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the from seconds.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the from seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether use jitter.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets the attempt count.
    /// </summary>
    public int AttemptCount => _attemptCount;

    /// <summary>
    /// Determines whether a reconnection should be attempted.
    /// </summary>
    /// <returns>True if reconnection should be attempted, false otherwise.</returns>
    public bool ShouldRetry()
    {
        if (MaxAttempts == -1)
        {
            return true; // Unlimited attempts
        }

        return _attemptCount < MaxAttempts;
    }

    /// <summary>
    /// Gets next delay.
    /// </summary>
    /// <returns>The delay duration.</returns>
    public TimeSpan GetNextDelay()
    {
        var delay = BaseDelay;

        if (UseExponentialBackoff && _attemptCount > 0)
        {
            var multiplier = Math.Pow(2, _attemptCount);
            delay = TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * multiplier);
        }

        // Cap at MaxDelay
        if (delay > MaxDelay)
        {
            delay = MaxDelay;
        }

        // Add jitter (±20%)
        if (UseJitter)
        {
            var jitterRange = delay.TotalMilliseconds * 0.2;
#if NETSTANDARD2_1
            var jitter = ((_random.NextDouble() * 2) - 1) * jitterRange; // -20% to +20%
#else
            var jitter = ((Random.Shared.NextDouble() * 2) - 1) * jitterRange; // -20% to +20%
#endif
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        _attemptCount++;
        return delay;
    }

    /// <summary>
    /// Resets the attempt counter after a successful connection.
    /// </summary>
    public void Reset() => _attemptCount = 0;

    /// <summary>
    /// Gets the default.
    /// </summary>
    public static ReconnectionPolicy Default() => new()
    {
        MaxAttempts = 5,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        UseExponentialBackoff = true,
        UseJitter = true
    };

    /// <summary>
    /// Gets the unlimited.
    /// </summary>
    public static ReconnectionPolicy Unlimited() => new()
    {
        MaxAttempts = -1,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        UseExponentialBackoff = true,
        UseJitter = true
    };

    /// <summary>
    /// Gets the none.
    /// </summary>
    public static ReconnectionPolicy None() => new()
    {
        MaxAttempts = 0
    };
}
