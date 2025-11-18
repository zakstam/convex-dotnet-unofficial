namespace RealtimeChat.Shared.Models;

/// <summary>
/// Represents a file that is pending upload.
/// </summary>
public record PendingFile(
    string Id,
    string Filename,
    string ContentType,
    long Size,
    Stream Content
);

