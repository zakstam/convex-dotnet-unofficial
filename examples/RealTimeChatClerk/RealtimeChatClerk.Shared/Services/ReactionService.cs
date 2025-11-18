using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Shared.ErrorHandling;
using System.Text.Json;
using RealtimeChatClerk.Shared.Models;
using Microsoft.Extensions.Logging;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service for reaction operations.
/// </summary>
public class ReactionService : IReactionService, IDisposable
{
    private readonly IConvexClient _convexClient;
    private readonly ILogger<ReactionService>? _logger;
    private readonly SubscriptionTracker _subscriptions = new();
    private readonly string _toggleReactionFunctionName;
    private readonly string _getReactionsFunctionName;
    private IDisposable? _reactionsSubscriptionDisposable;
    private readonly HashSet<string> _pendingReactionMutations = [];

    public Dictionary<string, List<ReactionDto>> MessageReactions { get; private set; } = [];
    public event EventHandler? ReactionsUpdated;

    public ReactionService(IConvexClient convexClient, string toggleReactionFunctionName = "functions/toggleReaction", string getReactionsFunctionName = "functions/getReactions", ILogger<ReactionService>? logger = null)
    {
        _convexClient = convexClient ?? throw new ArgumentNullException(nameof(convexClient));
        _toggleReactionFunctionName = toggleReactionFunctionName;
        _getReactionsFunctionName = getReactionsFunctionName;
        _logger = logger;
    }

    public async Task AddReactionAsync(string messageId, string emoji, string username)
    {
        if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
        {
            Console.Error.WriteLine($"Invalid inputs: messageId={messageId}, emoji={emoji}, username={username}");
            return;
        }

        try
        {
            _ = await _convexClient.Mutate<object>(_toggleReactionFunctionName)
                .WithArgs(new ToggleReactionArgs
                {
                    MessageId = messageId,
                    Username = username,
                    Emoji = emoji,
                    Add = true
                })
                .OptimisticWithAutoRollback(
                    getter: () =>
                    {
                        var cloned = new Dictionary<string, List<ReactionDto>>();
                        foreach (var kvp in MessageReactions)
                        {
                            cloned[kvp.Key] = [.. kvp.Value.Select(r => new ReactionDto
                            {
                                Emoji = r.Emoji,
                                Count = r.Count,
                                Users = [.. r.Users]
                            })];
                        }
                        return cloned;
                    },
                    setter: value => { MessageReactions = value; ReactionsUpdated?.Invoke(this, EventArgs.Empty); },
                    update: reactions =>
                    {
                        var updated = new Dictionary<string, List<ReactionDto>>(reactions);

                        if (!updated.TryGetValue(messageId, out var reactionList))
                        {
                            reactionList = [];
                            updated[messageId] = reactionList;
                        }

                        var existingReaction = reactionList.FirstOrDefault(r => r.Emoji == emoji);

                        if (existingReaction != null)
                        {
                            if (!existingReaction.Users.Contains(username))
                            {
                                var updatedReaction = new ReactionDto
                                {
                                    Emoji = existingReaction.Emoji,
                                    Count = existingReaction.Count + 1,
                                    Users = [.. existingReaction.Users, username]
                                };
                                var index = reactionList.IndexOf(existingReaction);
                                reactionList[index] = updatedReaction;
                            }
                        }
                        else
                        {
                            reactionList.Add(new ReactionDto
                            {
                                Emoji = emoji,
                                Count = 1,
                                Users = [username]
                            });
                        }

                        return updated;
                    })
                .TrackPending(_pendingReactionMutations, messageId)
                .OnError(ex => Console.Error.WriteLine($"Error adding reaction: {ex.Message}"))
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            Console.Error.WriteLine($"Error adding reaction: {ex.Message}");
        }
    }

    public async Task ToggleReactionAsync(string messageId, string emoji, string username)
    {
        if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
        {
            Console.Error.WriteLine($"[ReactionService] Invalid inputs: messageId={messageId}, emoji={emoji}, username={username}");
            return;
        }

        var reactions = MessageReactions.TryGetValue(messageId, out var value) ? value : [];
        var reaction = reactions.FirstOrDefault(r => r.Emoji == emoji);
        var isReacted = reaction != null && reaction.Users != null && reaction.Users.Contains(username);
        var originalState = isReacted;

        _logger?.LogInformation("ToggleReactionAsync: messageId={MessageId}, emoji={Emoji}, username={Username}, isReacted={IsReacted}, willAdd={WillAdd}", messageId, emoji, username, isReacted, !originalState);

        try
        {
            _ = await _convexClient.Mutate<object>(_toggleReactionFunctionName)
                .WithArgs(new ToggleReactionArgs
                {
                    MessageId = messageId,
                    Username = username,
                    Emoji = emoji,
                    Add = !originalState
                })
                .OptimisticWithAutoRollback(
                    getter: () =>
                    {
                        var cloned = new Dictionary<string, List<ReactionDto>>();
                        foreach (var kvp in MessageReactions)
                        {
                            cloned[kvp.Key] = [.. kvp.Value.Select(r => new ReactionDto
                            {
                                Emoji = r.Emoji,
                                Count = r.Count,
                                Users = [.. r.Users]
                            })];
                        }
                        return cloned;
                    },
                    setter: value =>
                    {
                        _logger?.LogDebug("ToggleReactionAsync: Optimistic update setter called. MessageReactions count: {Count}", value.Count);
                        MessageReactions = value;
                        ReactionsUpdated?.Invoke(this, EventArgs.Empty);
                    },
                    update: reactionsDict =>
                    {
                        var updated = new Dictionary<string, List<ReactionDto>>(reactionsDict);

                        if (!updated.TryGetValue(messageId, out var reactionList))
                        {
                            reactionList = [];
                            updated[messageId] = reactionList;
                        }

                        var existingReaction = reactionList.FirstOrDefault(r => r.Emoji == emoji);

                        if (existingReaction != null)
                        {
                            if (isReacted)
                            {
                                if (existingReaction.Count == 1)
                                {
                                    _ = reactionList.Remove(existingReaction);
                                }
                                else
                                {
                                    var updatedReaction = new ReactionDto
                                    {
                                        Emoji = existingReaction.Emoji,
                                        Count = existingReaction.Count - 1,
                                        Users = [.. existingReaction.Users.Where(u => u != username)]
                                    };
                                    var index = reactionList.IndexOf(existingReaction);
                                    reactionList[index] = updatedReaction;
                                }
                            }
                            else
                            {
                                var updatedReaction = new ReactionDto
                                {
                                    Emoji = existingReaction.Emoji,
                                    Count = existingReaction.Count + 1,
                                    Users = [.. existingReaction.Users, username]
                                };
                                var index = reactionList.IndexOf(existingReaction);
                                reactionList[index] = updatedReaction;
                            }
                        }
                        else if (!isReacted)
                        {
                            reactionList.Add(new ReactionDto
                            {
                                Emoji = emoji,
                                Count = 1,
                                Users = [username]
                            });
                        }

                        return updated;
                    })
                .TrackPending(_pendingReactionMutations, messageId)
                .OnError(ex => _logger?.LogError(ex, "Error toggling reaction"))
                .ExecuteAsync();


            _logger?.LogInformation("ToggleReactionAsync: Mutation completed. MessageReactions count: {Count}", MessageReactions.Count);
        }
        catch (ConvexException ex)
        {
            _logger?.LogError(ex, "Error toggling reaction");
        }
    }

    public async Task LoadReactionsAsync(List<string> messageIds)
    {
        if (messageIds.Count == 0)
        {
            _logger?.LogDebug("LoadReactionsAsync: No message IDs provided");
            return;
        }

        _logger?.LogInformation("LoadReactionsAsync: Loading reactions for {Count} messages", messageIds.Count);
        try
        {
            var result = await _convexClient.Query<JsonElement>(_getReactionsFunctionName)
                .WithArgs(new GetReactionsArgs { MessageIds = messageIds })
                .ExecuteAsync();

            if (result.IsNullOrUndefined())
            {
                _logger?.LogWarning("LoadReactionsAsync: Query returned null/undefined");
                return;
            }

            var reactionsData = result.UnwrapValue();

            if (reactionsData.ValueKind == JsonValueKind.Object)
            {
                var reactions = ParseReactionsFromJson(reactionsData);
                _logger?.LogInformation("LoadReactionsAsync: Parsed {Count} message reactions", reactions.Count);

                foreach (var kvp in reactions)
                {
                    MessageReactions[kvp.Key] = kvp.Value;
                    _logger?.LogDebug("LoadReactionsAsync: Message {MessageId} has {ReactionCount} reaction types", kvp.Key, kvp.Value.Count);
                }

                var messagesToRemove = MessageReactions.Keys
                    .Where(id => !messageIds.Contains(id))
                    .ToList();
                foreach (var messageId in messagesToRemove)
                {
                    _ = MessageReactions.Remove(messageId);
                }

                _logger?.LogInformation("LoadReactionsAsync: Final MessageReactions count: {Count}", MessageReactions.Count);
                ReactionsUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _logger?.LogWarning("LoadReactionsAsync: Unexpected JSON value kind: {ValueKind}", reactionsData.ValueKind);
            }
        }
        catch (ConvexException ex)
        {
            _logger?.LogError(ex, "Error loading reactions");
        }
    }

    private bool _isFirstSubscriptionUpdate = true;

    public void SubscribeToReactions(List<string> messageIds, Action<Dictionary<string, List<ReactionDto>>> onUpdate, Action<string>? onError = null)
    {
        if (messageIds.Count == 0)
        {
            _logger?.LogDebug("SubscribeToReactions: No message IDs provided");
            return;
        }

        _logger?.LogInformation("SubscribeToReactions: Setting up subscription for {Count} messages", messageIds.Count);

        if (_reactionsSubscriptionDisposable != null)
        {
            _logger?.LogDebug("SubscribeToReactions: Disposing old subscription");
            _ = _subscriptions.Remove(_reactionsSubscriptionDisposable);
            _reactionsSubscriptionDisposable?.Dispose();
        }

        _isFirstSubscriptionUpdate = true;

        _reactionsSubscriptionDisposable = _convexClient
            .CreateResilientSubscription<JsonElement>(_getReactionsFunctionName, new GetReactionsArgs { MessageIds = messageIds })
            .Subscribe(
                result =>
                {
                    _logger?.LogInformation("=== Subscription callback FIRED (isFirstUpdate: {IsFirstUpdate}) ===", _isFirstSubscriptionUpdate);

                    if (result.IsNullOrUndefined())
                    {
                        _logger?.LogWarning("Subscription callback: Result is null/undefined");
                        return;
                    }

                    var reactionsData = result.UnwrapValue();
                    if (reactionsData.ValueKind != JsonValueKind.Object)
                    {
                        _logger?.LogWarning("Subscription callback: Unexpected JSON value kind: {ValueKind}", reactionsData.ValueKind);
                        return;
                    }

                    var reactions = ParseReactionsFromJson(reactionsData);
                    _logger?.LogInformation("Subscription callback: Parsed {Count} message reactions", reactions.Count);

                    // Always update MessageReactions from server data, regardless of pending mutations
                    // The pending mutations check was preventing updates from other tabs/clients
                    var anyChanges = false;
                    foreach (var kvp in reactions)
                    {
                        var hasChanged = !MessageReactions.ContainsKey(kvp.Key);
                        _logger?.LogDebug("Subscription callback: Processing message {MessageId}, hasChanged (new key): {HasChanged}", kvp.Key, hasChanged);

                        if (!hasChanged)
                        {
                            var existing = MessageReactions[kvp.Key];
                            var newReactions = kvp.Value;
                            _logger?.LogDebug("Subscription callback: Message {MessageId} - existing reactions: {ExistingCount}, new reactions: {NewCount}", kvp.Key, existing.Count, newReactions.Count);

                            if (existing.Count != newReactions.Count)
                            {
                                hasChanged = true;
                                _logger?.LogDebug("Subscription callback: Message {MessageId} - count mismatch detected", kvp.Key);
                            }
                            else
                            {
                                for (var i = 0; i < existing.Count; i++)
                                {
                                    var existingReaction = existing[i];
                                    var newReaction = newReactions.FirstOrDefault(r => r.Emoji == existingReaction.Emoji);

                                    if (newReaction == null ||
                                        newReaction.Count != existingReaction.Count ||
                                        !newReaction.Users.SequenceEqual(existingReaction.Users))
                                    {
                                        hasChanged = true;
                                        _logger?.LogDebug("Subscription callback: Message {MessageId} - reaction {Emoji} changed (exists: {Exists}, count: {OldCount} -> {NewCount})", kvp.Key, existingReaction.Emoji, newReaction != null, existingReaction.Count, newReaction?.Count);
                                        break;
                                    }
                                }

                                if (!hasChanged)
                                {
                                    foreach (var newReaction in newReactions)
                                    {
                                        if (!existing.Any(r => r.Emoji == newReaction.Emoji))
                                        {
                                            hasChanged = true;
                                            _logger?.LogDebug("Subscription callback: Message {MessageId} - new reaction type {Emoji} detected", kvp.Key, newReaction.Emoji);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (hasChanged)
                        {
                            _logger?.LogDebug("Subscription callback: Updating MessageReactions for {MessageId}", kvp.Key);
                            MessageReactions[kvp.Key] = kvp.Value;
                            anyChanges = true;
                        }
                        else
                        {
                            _logger?.LogDebug("Subscription callback: No changes for message {MessageId}", kvp.Key);
                        }
                    }

                    // Remove reactions for messages that are no longer in the subscription list
                    var messagesToRemove = MessageReactions.Keys
                        .Where(id => !messageIds.Contains(id))
                        .ToList();
                    if (messagesToRemove.Count > 0)
                    {
                        foreach (var messageId in messagesToRemove)
                        {
                            _ = MessageReactions.Remove(messageId);
                        }
                        anyChanges = true;
                    }

                    // Always notify on first update to ensure initial state is synced, or if there are changes
                    if (anyChanges || _isFirstSubscriptionUpdate)
                    {
                        _logger?.LogInformation("Subscription callback: Notifying update (anyChanges: {AnyChanges}, isFirstUpdate: {IsFirstUpdate})", anyChanges, _isFirstSubscriptionUpdate);
                        _logger?.LogDebug("Subscription callback: MessageReactions count: {Count}", MessageReactions.Count);
                        _logger?.LogInformation("Subscription callback: Calling onUpdate callback with {Count} message reactions", MessageReactions.Count);
                        _isFirstSubscriptionUpdate = false;
                        try
                        {
                            onUpdate(MessageReactions);
                            _logger?.LogInformation("Subscription callback: onUpdate callback completed successfully");
                        }
                        catch (ConvexException ex)
                        {
                            _logger?.LogError(ex, "Subscription callback: Error in onUpdate callback");
                        }
                        ReactionsUpdated?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        _logger?.LogDebug("Subscription callback: No changes detected, skipping update");
                    }
                },
                error =>
                {
                    var errorMessage = error is Exception ex ? ex.Message : error?.ToString() ?? "Unknown error";
                    _logger?.LogError(error is Exception ex2 ? ex2 : null, "Reactions subscription error: {ErrorMessage}", errorMessage);
                    onError?.Invoke(errorMessage);
                }
            );

        _logger?.LogInformation("SubscribeToReactions: Subscription created successfully");
        _ = _subscriptions.Add(_reactionsSubscriptionDisposable);
    }

    private Dictionary<string, List<ReactionDto>> ParseReactionsFromJson(JsonElement reactionsData)
    {
        var reactions = new Dictionary<string, List<ReactionDto>>();

        foreach (var property in reactionsData.EnumerateObject())
        {
            var messageId = property.Name;
            var reactionsList = new List<ReactionDto>();

            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var reactionElement in property.Value.EnumerateArray())
                {
                    var emojiValue = reactionElement.GetProperty("emoji").GetString() ?? "";
                    var countValue = reactionElement.GetProperty("count");
                    int count;

                    if (countValue.ValueKind == JsonValueKind.Number)
                    {
                        try
                        {
                            count = countValue.GetInt32();
                        }
                        catch
                        {
                            count = (int)countValue.GetDouble();
                        }
                    }
                    else
                    {
                        count = 0;
                    }

                    var reaction = new ReactionDto
                    {
                        Emoji = emojiValue,
                        Count = count,
                        Users = [.. reactionElement.GetProperty("users").EnumerateArray().Select(u => u.GetString() ?? "")]
                    };
                    reactionsList.Add(reaction);
                }
            }

            reactions[messageId] = reactionsList;
        }

        return reactions;
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        GC.SuppressFinalize(this);
    }
}

