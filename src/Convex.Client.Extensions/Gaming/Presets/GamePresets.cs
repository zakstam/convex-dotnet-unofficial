using Convex.Client.Extensions.Batching.TimeBasedBatching;
using Convex.Client.Extensions.Gaming.Sync;

namespace Convex.Client.Extensions.Gaming.Presets;

/// <summary>
/// Provides pre-configured <see cref="GameSyncOptions"/> optimized for common game types.
/// These presets balance performance, cost, and user experience based on game requirements.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cost Savings Summary:</b>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Preset</term>
/// <description>Typical Savings</description>
/// </listheader>
/// <item>
/// <term>Action Game</term>
/// <description>~90% reduction in mutations, ~67% reduction in subscription traffic</description>
/// </item>
/// <item>
/// <term>Turn-Based</term>
/// <description>No batching needed (already efficient)</description>
/// </item>
/// <item>
/// <term>Drawing</term>
/// <description>~97% reduction in mutations</description>
/// </item>
/// <item>
/// <term>Cursor Tracking</term>
/// <description>~95% reduction in mutations</description>
/// </item>
/// </list>
/// </remarks>
public static class GamePresets
{
    /// <summary>
    /// Options optimized for action/shooter games requiring responsive controls.
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: 60 samples/sec max, batched at 20Hz (50ms intervals)</item>
    /// <item>Subscriptions: 20 updates/sec</item>
    /// <item>Interpolation: 100ms delay for smooth visuals</item>
    /// <item>Prediction: Enabled for instant local response</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Reduces 60fps input to ~20 batches/sec (67% reduction),
    /// with client-side prediction eliminating perceived latency.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for action games.</returns>
    public static GameSyncOptions ForActionGame() => new()
    {
        InputBatching = BatchingOptions.Create()
            .WithSampling(16)       // ~60 samples/sec max
            .WithBatchInterval(50)  // 20 batches/sec
            .WithMaxBatchSize(30)   // ~1.5 sec of input max
            .Build(),
        SubscriptionThrottle = TimeSpan.FromMilliseconds(50), // 20 updates/sec
        InterpolationDelayMs = 100,  // 100ms behind server for smooth interpolation
        EnablePrediction = true,
        MaxExtrapolationMs = 200,
        MaxPendingInputs = 60        // 3 seconds at 20 inputs/sec
    };

    /// <summary>
    /// Options optimized for turn-based games (chess, card games, etc.).
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: No batching (send immediately on action)</item>
    /// <item>Subscriptions: Real-time updates</item>
    /// <item>Interpolation: Disabled</item>
    /// <item>Prediction: Disabled (server authoritative)</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Minimal - turn-based games naturally have low update frequency.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for turn-based games.</returns>
    public static GameSyncOptions ForTurnBased() => new()
    {
        InputBatching = null,           // No batching, send on action
        SubscriptionThrottle = null,    // Real-time updates
        InterpolationDelayMs = 0,       // No interpolation needed
        EnablePrediction = false,       // Server authoritative
        MaxExtrapolationMs = 0,
        MaxPendingInputs = 1
    };

    /// <summary>
    /// Options optimized for drawing/whiteboard applications.
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: 10ms sampling with 2px spatial filtering, batched at 2Hz (500ms)</item>
    /// <item>Subscriptions: 10 updates/sec</item>
    /// <item>Interpolation: 50ms delay (minimal latency for drawing)</item>
    /// <item>Prediction: Disabled (draw locally, sync batches)</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Reduces continuous drawing from ~100 calls/sec to ~2 calls/sec (97% reduction).
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for drawing applications.</returns>
    public static GameSyncOptions ForDrawing() => new()
    {
        InputBatching = BatchingOptions.ForDrawing(),
        SubscriptionThrottle = TimeSpan.FromMilliseconds(100), // 10 updates/sec
        InterpolationDelayMs = 50,  // Low latency for responsive drawing
        EnablePrediction = false,   // Draw locally, batch sync
        MaxExtrapolationMs = 100,
        MaxPendingInputs = 10
    };

    /// <summary>
    /// Options optimized for cursor/pointer tracking (collaborative editing, etc.).
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: 16ms sampling with 5px spatial filtering, batched at 5Hz (200ms)</item>
    /// <item>Subscriptions: 10 updates/sec</item>
    /// <item>Interpolation: 100ms delay for smooth cursor movement</item>
    /// <item>Prediction: Disabled</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Reduces continuous cursor updates from ~60 calls/sec to ~5 calls/sec (92% reduction).
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for cursor tracking.</returns>
    public static GameSyncOptions ForCursorTracking() => new()
    {
        InputBatching = BatchingOptions.ForCursorTracking(),
        SubscriptionThrottle = TimeSpan.FromMilliseconds(100), // 10 updates/sec
        InterpolationDelayMs = 100,  // Smooth cursor interpolation
        EnablePrediction = false,
        MaxExtrapolationMs = 150,
        MaxPendingInputs = 10
    };

    /// <summary>
    /// Options optimized for real-time multiplayer games with moderate update frequency.
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: 16ms sampling, batched at 10Hz (100ms intervals)</item>
    /// <item>Subscriptions: 10 updates/sec</item>
    /// <item>Interpolation: 150ms delay for smooth movement</item>
    /// <item>Prediction: Enabled</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Good balance between responsiveness and cost.
    /// Reduces mutations by ~83%, subscriptions by ~83%.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for casual multiplayer games.</returns>
    public static GameSyncOptions ForCasualMultiplayer() => new()
    {
        InputBatching = BatchingOptions.Create()
            .WithSampling(16)        // ~60 samples/sec max
            .WithBatchInterval(100)  // 10 batches/sec
            .WithMaxBatchSize(20)
            .Build(),
        SubscriptionThrottle = TimeSpan.FromMilliseconds(100), // 10 updates/sec
        InterpolationDelayMs = 150,  // Generous buffer for smooth play
        EnablePrediction = true,
        MaxExtrapolationMs = 300,
        MaxPendingInputs = 30
    };

    /// <summary>
    /// Options with minimal traffic, suitable for bandwidth-constrained scenarios.
    /// </summary>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item>Input: 50ms sampling, batched at 2Hz (500ms intervals)</item>
    /// <item>Subscriptions: 4 updates/sec</item>
    /// <item>Interpolation: 250ms delay</item>
    /// <item>Prediction: Enabled to mask latency</item>
    /// </list>
    /// <para>
    /// <b>Cost Impact:</b> Maximum cost savings (~95% reduction in traffic).
    /// Best for mobile games or low-bandwidth scenarios.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="GameSyncOptions"/> configured for minimal bandwidth usage.</returns>
    public static GameSyncOptions ForLowBandwidth() => new()
    {
        InputBatching = BatchingOptions.Create()
            .WithSampling(50)        // 20 samples/sec max
            .WithBatchInterval(500)  // 2 batches/sec
            .WithMaxBatchSize(50)
            .Build(),
        SubscriptionThrottle = TimeSpan.FromMilliseconds(250), // 4 updates/sec
        InterpolationDelayMs = 250,  // Generous buffer to hide latency
        EnablePrediction = true,
        MaxExtrapolationMs = 500,
        MaxPendingInputs = 20
    };
}
