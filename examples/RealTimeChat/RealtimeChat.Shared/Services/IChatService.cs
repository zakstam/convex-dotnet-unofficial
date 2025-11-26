using RealtimeChat.Shared.Models;
using Convex.Client.Features.RealTime.Pagination;

namespace RealtimeChat.Shared.Services;

/// <summary>
/// Service interface for chat message operations.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Loads initial messages using pagination.
    /// </summary>
    Task LoadMessagesAsync();

    /// <summary>
    /// Loads more (older) messages.
    /// </summary>
    Task LoadMoreMessagesAsync();

    /// <summary>
    /// Sends a new message.
    /// </summary>
    Task SendMessageAsync(string text, List<Attachment>? attachments = null);

    /// <summary>
    /// Sends a reply to a message.
    /// </summary>
    Task SendReplyAsync(string text, string parentMessageId, List<Attachment>? attachments = null);

    /// <summary>
    /// Edits an existing message.
    /// </summary>
    Task EditMessageAsync(string messageId, string newText);

    /// <summary>
    /// Deletes a message.
    /// </summary>
    Task DeleteMessageAsync(string messageId);

    /// <summary>
    /// Searches for messages.
    /// </summary>
    Task<List<Message>> SearchMessagesAsync(string searchText, int limit = 20);

    /// <summary>
    /// Subscribes to real-time message updates.
    /// </summary>
    void SubscribeToMessages(Action<List<Message>> onUpdate, Action<string>? onError = null);

    /// <summary>
    /// Gets grouped messages for display.
    /// </summary>
    List<MessageGroup> GetGroupedMessages(string currentUsername);

    /// <summary>
    /// Gets the message paginator (for internal use).
    /// </summary>
    IPaginator<MessageDto>? MessagePaginator { get; }

    /// <summary>
    /// Event raised when messages are updated.
    /// </summary>
    event EventHandler? MessagesUpdated;
}

