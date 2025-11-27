using Convex.Client;
using CursorPlayground.Shared.Generated;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CursorPlayground.Shared.Services;

/// <summary>
/// Service for interacting with the Cursor Playground backend.
/// Uses generated types from the Convex schema.
/// </summary>
public class CursorService : IDisposable
{
    private readonly IConvexClient _client;
    private readonly List<IDisposable> _subscriptions = new();

    // Events - using generated types from schema
    public event EventHandler<List<Users>>? ActiveUsersUpdated;
    public event EventHandler<List<CursorBatches>>? CursorBatchesUpdated;
    public event EventHandler<List<Reactions>>? ReactionsUpdated;
    public event EventHandler<List<ClickEffects>>? ClickEffectsUpdated;

    public CursorService(IConvexClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Gets the Convex client for direct access.
    /// </summary>
    public IConvexClient Client => _client;

    // Subscribe to active users list
    public void SubscribeToActiveUsers()
    {
        var subscription = _client
            .Observe<List<Users>>("functions/users:listActive")
            .Subscribe(
                users => ActiveUsersUpdated?.Invoke(this, users ?? new List<Users>()),
                error => Console.WriteLine($"Active users subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to cursor batches
    public void SubscribeToCursorBatches()
    {
        var subscription = _client
            .Observe<List<CursorBatches>>("functions/cursorBatches:list")
            .Subscribe(
                batches => CursorBatchesUpdated?.Invoke(this, batches ?? new List<CursorBatches>()),
                error => Console.WriteLine($"Cursor batches subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to recent reactions
    public void SubscribeToReactions()
    {
        var subscription = _client
            .Observe<List<Reactions>>("functions/reactions:listRecent")
            .Subscribe(
                reactions => ReactionsUpdated?.Invoke(this, reactions ?? new List<Reactions>()),
                error => Console.WriteLine($"Reactions subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Subscribe to recent click effects
    public void SubscribeToClickEffects()
    {
        var subscription = _client
            .Observe<List<ClickEffects>>("functions/clickEffects:listRecent")
            .Subscribe(
                effects => ClickEffectsUpdated?.Invoke(this, effects ?? new List<ClickEffects>()),
                error => Console.WriteLine($"Click effects subscription error: {error.Message}")
            );

        _subscriptions.Add(subscription);
    }

    // Join the playground
    public async Task<string> JoinAsync(string name, string emoji, string color)
    {
        var userId = await _client
            .Mutate<string>("functions/users:join")
            .WithArgs(new { name, emoji, color })
            .ExecuteAsync();

        return userId;
    }

    // Send heartbeat
    public async Task HeartbeatAsync(string userId)
    {
        await _client
            .Mutate<object>("functions/users:heartbeat")
            .WithArgs(new { userId })
            .ExecuteAsync();
    }

    // Create reaction
    public async Task CreateReactionAsync(string userId, string emoji, double x, double y)
    {
        await _client
            .Mutate<object>("functions/reactions:create")
            .WithArgs(new { userId, emoji, x, y })
            .ExecuteAsync();
    }

    // Create click effect (particle burst)
    public async Task CreateClickEffectAsync(string userId, double x, double y, string color)
    {
        await _client
            .Mutate<object>("functions/clickEffects:create")
            .WithArgs(new { userId, x, y, color })
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
