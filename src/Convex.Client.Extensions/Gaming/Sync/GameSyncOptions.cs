using Convex.Client.Extensions.Batching.TimeBasedBatching;

namespace Convex.Client.Extensions.Gaming.Sync;

/// <summary>
/// Configuration options for game state synchronization.
/// Combines input batching, subscription throttling, and interpolation settings.
/// </summary>
/// <remarks>
/// Use <see cref="Gaming.Presets.GamePresets"/> for pre-configured options optimized for common game types.
/// </remarks>
public class GameSyncOptions
{
    /// <summary>
    /// Gets or sets the batching options for high-frequency input.
    /// Set to null to disable input batching (send immediately).
    /// </summary>
    public BatchingOptions? InputBatching { get; set; }

    /// <summary>
    /// Gets or sets the subscription throttle interval.
    /// Server updates will be sampled at this rate.
    /// Set to null to receive all updates in real-time.
    /// </summary>
    /// <remarks>
    /// Recommended values:
    /// - Action games: 50ms (20 updates/sec)
    /// - Casual games: 100ms (10 updates/sec)
    /// - Turn-based: null (real-time)
    /// </remarks>
    public TimeSpan? SubscriptionThrottle { get; set; }

    /// <summary>
    /// Gets or sets the interpolation delay in milliseconds.
    /// Higher values provide smoother interpolation but increase visual latency.
    /// Set to 0 to disable interpolation.
    /// </summary>
    /// <remarks>
    /// Recommended values:
    /// - Action games: 50-100ms
    /// - Casual games: 100-150ms
    /// - Turn-based: 0ms
    /// </remarks>
    public double InterpolationDelayMs { get; set; }

    /// <summary>
    /// Gets or sets whether client-side prediction is enabled.
    /// When enabled, local inputs are applied immediately and reconciled with server state.
    /// </summary>
    public bool EnablePrediction { get; set; }

    /// <summary>
    /// Gets or sets the maximum extrapolation time in milliseconds.
    /// If no server update arrives within this time, extrapolation stops.
    /// Only applies when interpolation is enabled.
    /// </summary>
    public double MaxExtrapolationMs { get; set; } = 250;

    /// <summary>
    /// Gets or sets the maximum number of pending inputs to keep for reconciliation.
    /// Only applies when prediction is enabled.
    /// </summary>
    public int MaxPendingInputs { get; set; } = 60;

    /// <summary>
    /// Creates a new instance with default settings (no optimizations).
    /// </summary>
    public GameSyncOptions()
    {
    }

    /// <summary>
    /// Creates a copy of the specified options.
    /// </summary>
    /// <param name="other">The options to copy.</param>
    public GameSyncOptions(GameSyncOptions other)
    {
        ArgumentNullException.ThrowIfNull(other);

        InputBatching = other.InputBatching;
        SubscriptionThrottle = other.SubscriptionThrottle;
        InterpolationDelayMs = other.InterpolationDelayMs;
        EnablePrediction = other.EnablePrediction;
        MaxExtrapolationMs = other.MaxExtrapolationMs;
        MaxPendingInputs = other.MaxPendingInputs;
    }
}
