using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Generated;
using CursorPlayground.Shared.Generated;

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
