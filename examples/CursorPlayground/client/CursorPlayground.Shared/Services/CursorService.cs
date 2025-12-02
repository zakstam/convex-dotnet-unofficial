using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Extensions.Gaming.Presets;
using Convex.Client.Extensions.Gaming.Sync;
using Convex.Generated;
using CursorPlayground.Shared.Generated;
using CursorPlayground.Shared.Models;

namespace CursorPlayground.Shared.Services;

#region Argument Types

/// <summary>Arguments for joining the playground.</summary>
public sealed record JoinArgs(string Name, string Emoji, string Color);

/// <summary>Arguments for user heartbeat.</summary>
public sealed record HeartbeatArgs(string UserId);

/// <summary>Arguments for creating a reaction.</summary>
public sealed record CreateReactionArgs(string UserId, string Emoji, double X, double Y);

/// <summary>Arguments for creating a click effect.</summary>
public sealed record CreateClickEffectArgs(string UserId, double X, double Y, string Color);

#endregion

/// <summary>
/// Service for interacting with the Cursor Playground backend.
/// Uses generated types from the Convex schema.
/// </summary>
public class CursorService(IConvexClient client) : IDisposable
{
    private readonly List<IDisposable> _subscriptions = [];

    // Events - using generated types from schema
    public event EventHandler<List<User>>? ActiveUsersUpdated;
    public event EventHandler<List<CursorBatch>>? CursorBatchesUpdated;
    public event EventHandler<List<Reaction>>? ReactionsUpdated;
    public event EventHandler<List<ClickEffect>>? ClickEffectsUpdated;

    /// <summary>
    /// Gets the Convex client for direct access.
    /// </summary>
    public IConvexClient Client { get; } = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>
    /// Gets the game sync options for cursor tracking.
    /// Use these settings for consistent behavior across input batching and subscription throttling.
    /// </summary>
    /// <remarks>
    /// These options provide:
    /// <list type="bullet">
    /// <item>16ms input sampling (~60fps)</item>
    /// <item>200ms batch interval (5 batches/sec)</item>
    /// <item>5px spatial filtering</item>
    /// <item>100ms interpolation delay for smooth rendering</item>
    /// <item>150ms max extrapolation for brief network gaps</item>
    /// </list>
    /// </remarks>
    public static GameSyncOptions CursorSyncOptions => GamePresets.ForCursorTracking();

    // Subscribe to active users list
    public void SubscribeToActiveUsers()
    {
        var subscription = Client
            .CreateResilientSubscription<List<User>>(ConvexFunctions.Queries.Users.ListActive)
            .Subscribe(
                users => ActiveUsersUpdated?.Invoke(this, users ?? []),
                error => Console.WriteLine($"Active users subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to cursor batches
    public void SubscribeToCursorBatches()
    {
        var subscription = Client
            .CreateResilientSubscription<List<CursorBatch>>(ConvexFunctions.Queries.CursorBatches.List)
            .Subscribe(
                batches => CursorBatchesUpdated?.Invoke(this, batches ?? []),
                error => Console.WriteLine($"Cursor batches subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    /// <summary>
    /// Creates an interpolated state for smooth cursor position rendering.
    /// Use this when you need silky-smooth cursor animations between network updates.
    /// </summary>
    /// <param name="options">Optional sync options. Defaults to <see cref="CursorSyncOptions"/>.</param>
    /// <returns>
    /// A tuple containing the <see cref="InterpolatedState{T}"/> for smooth rendering
    /// and the subscription disposable.
    /// </returns>
    /// <example>
    /// <code>
    /// // Create interpolated cursor state
    /// var (interpolated, subscription) = cursorService.CreateInterpolatedCursorState();
    ///
    /// // In your render loop (e.g., 60fps animation frame)
    /// var smoothPosition = interpolated.GetRenderState();
    /// if (smoothPosition != null)
    /// {
    ///     RenderCursor(smoothPosition.X, smoothPosition.Y);
    /// }
    ///
    /// // Cleanup when done
    /// subscription.Dispose();
    /// </code>
    /// </example>
    public InterpolatedState<CursorPosition> CreateInterpolatedCursorState(GameSyncOptions? options = null)
    {
        options ??= CursorSyncOptions;

        return new InterpolatedState<CursorPosition>
        {
            InterpolationDelayMs = options.InterpolationDelayMs,
            MaxExtrapolationMs = options.MaxExtrapolationMs
        };
    }

    // Subscribe to recent reactions
    public void SubscribeToReactions()
    {
        var subscription = Client
            .CreateResilientSubscription<List<Reaction>>(ConvexFunctions.Queries.Reactions.ListRecent)
            .Subscribe(
                reactions => ReactionsUpdated?.Invoke(this, reactions ?? []),
                error => Console.WriteLine($"Reactions subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to recent click effects
    public void SubscribeToClickEffects()
    {
        var subscription = Client
            .CreateResilientSubscription<List<ClickEffect>>(ConvexFunctions.Queries.ClickEffects.ListRecent)
            .Subscribe(
                effects => ClickEffectsUpdated?.Invoke(this, effects ?? []),
                error => Console.WriteLine($"Click effects subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Join the playground
    public async Task<string> JoinAsync(string name, string emoji, string color)
    {
        var userId = await Client
            .Mutate<string>(ConvexFunctions.Mutations.Users.Join)
            .WithArgs(new JoinArgs(name, emoji, color))
            .ExecuteAsync();

        return userId;
    }

    // Send heartbeat
    public async Task HeartbeatAsync(string userId)
    {
        _ = await Client
            .Mutate<object>(ConvexFunctions.Mutations.Users.Heartbeat)
            .WithArgs(new HeartbeatArgs(userId))
            .ExecuteAsync();
    }

    // Create reaction
    public async Task CreateReactionAsync(string userId, string emoji, double x, double y)
    {
        _ = await Client
            .Mutate<object>(ConvexFunctions.Mutations.Reactions.Create)
            .WithArgs(new CreateReactionArgs(userId, emoji, x, y))
            .ExecuteAsync();
    }

    // Create click effect (particle burst)
    public async Task CreateClickEffectAsync(string userId, double x, double y, string color)
    {
        _ = await Client
            .Mutate<object>(ConvexFunctions.Mutations.ClickEffects.Create)
            .WithArgs(new CreateClickEffectArgs(userId, x, y, color))
            .ExecuteAsync();
    }

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        GC.SuppressFinalize(this);
    }
}
