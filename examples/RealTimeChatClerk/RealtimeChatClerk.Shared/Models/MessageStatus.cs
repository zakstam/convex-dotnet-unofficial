namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Represents the status of a message.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message was sent successfully.
    /// </summary>
    Sent,

    /// <summary>
    /// Message was delivered (exists in database).
    /// </summary>
    Delivered,

    /// <summary>
    /// Message was read by at least one user.
    /// </summary>
    Read
}

