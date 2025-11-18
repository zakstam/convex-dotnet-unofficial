using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service interface for file upload and attachment operations.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Uploads files and returns attachment information.
    /// </summary>
    Task<List<Attachment>> UploadFilesAsync(List<PendingFile> pendingFiles);

    /// <summary>
    /// Gets the download URL for an attachment.
    /// </summary>
    Task<string> GetAttachmentUrlAsync(string storageId);

    /// <summary>
    /// Gets the download URL synchronously (from cache if available).
    /// </summary>
    string GetAttachmentUrlSync(string storageId);

    /// <summary>
    /// Loads attachment URLs for multiple messages.
    /// </summary>
    Task LoadAttachmentUrlsForMessagesAsync(List<Message> messages);

    /// <summary>
    /// Formats file size for display.
    /// </summary>
    string FormatFileSize(long bytes);
}

