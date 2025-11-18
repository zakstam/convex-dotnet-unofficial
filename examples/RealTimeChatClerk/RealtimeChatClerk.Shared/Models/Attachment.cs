namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Domain model for a file attachment.
/// </summary>
public record Attachment(
    string StorageId,
    string Filename,
    string ContentType,
    long Size
);

