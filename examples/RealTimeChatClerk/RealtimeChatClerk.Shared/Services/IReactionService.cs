using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service interface for reaction operations.
/// </summary>
public interface IReactionService
{
    /// <summary>
    /// Adds a reaction to a message.
    /// </summary>
    Task AddReactionAsync(string messageId, string emoji, string username);

    /// <summary>
    /// Toggles a reaction on a message.
    /// </summary>
    Task ToggleReactionAsync(string messageId, string emoji, string username);

    /// <summary>
    /// Loads reactions for the given messages.
    /// </summary>
    Task LoadReactionsAsync(List<string> messageIds);

    /// <summary>
    /// Subscribes to reaction updates for the given messages.
    /// </summary>
    void SubscribeToReactions(List<string> messageIds, Action<Dictionary<string, List<ReactionDto>>> onUpdate, Action<string>? onError = null);

    /// <summary>
    /// Event raised when reactions are updated.
    /// </summary>
    event EventHandler? ReactionsUpdated;
}

