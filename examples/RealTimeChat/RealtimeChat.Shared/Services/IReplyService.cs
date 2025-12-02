using Convex.Generated;
using RealtimeChat.Shared.Models;
using Message = RealtimeChat.Shared.Models.Message;

namespace RealtimeChat.Shared.Services;

/// <summary>
/// Service interface for reply operations.
/// </summary>
public interface IReplyService
{
    /// <summary>
    /// Loads replies for a parent message.
    /// </summary>
    Task<List<Message>> LoadRepliesAsync(MessageId parentMessageId);

    /// <summary>
    /// Checks if a message has replies.
    /// </summary>
    bool HasReplies(string messageId, Dictionary<string, List<Message>> messageReplies);

    /// <summary>
    /// Gets replies for a message.
    /// </summary>
    List<Message> GetReplies(string messageId, Dictionary<string, List<Message>> messageReplies);
}

