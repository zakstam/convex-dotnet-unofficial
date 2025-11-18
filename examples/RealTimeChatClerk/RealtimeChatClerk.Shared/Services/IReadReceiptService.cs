using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service interface for read receipt operations.
/// </summary>
public interface IReadReceiptService
{
    /// <summary>
    /// Marks a message as read by a user.
    /// </summary>
    Task MarkMessageAsReadAsync(string messageId, string username);

    /// <summary>
    /// Loads read receipts for the given messages.
    /// </summary>
    Task LoadReadReceiptsAsync(List<string> messageIds);

    /// <summary>
    /// Subscribes to read receipt updates for the given messages.
    /// </summary>
    void SubscribeToReadReceipts(List<string> messageIds, Action<Dictionary<string, List<MessageReadDto>>> onUpdate, Action<string>? onError = null);

    /// <summary>
    /// Event raised when read receipts are updated.
    /// </summary>
    event EventHandler? ReadReceiptsUpdated;

    /// <summary>
    /// Dictionary of message IDs to their read receipts.
    /// </summary>
    Dictionary<string, List<MessageReadDto>> MessageReadReceipts { get; }
}

