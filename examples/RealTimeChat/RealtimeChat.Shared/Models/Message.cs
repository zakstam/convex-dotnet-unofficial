namespace RealtimeChat.Shared.Models;

/// <summary>
/// Domain model for a chat message.
/// This is a domain model used for business logic, separate from the DTO used for serialization.
/// </summary>
public record Message(
    string Id,
    string Username,
    string Text,
    long Timestamp,
    long? EditedAt,
    string? ParentMessageId = null,
    List<Attachment>? Attachments = null
);

