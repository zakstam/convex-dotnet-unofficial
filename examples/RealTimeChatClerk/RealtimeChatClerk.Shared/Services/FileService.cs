using Convex.Client;
using Convex.Client.Infrastructure.ErrorHandling;
using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service for file upload and attachment operations.
/// </summary>
public class FileService(IConvexClient convexClient) : IFileService
{
    private readonly IConvexClient _convexClient = convexClient ?? throw new ArgumentNullException(nameof(convexClient));
    private readonly Dictionary<string, string> _attachmentUrlCache = [];

    public async Task<List<Attachment>> UploadFilesAsync(List<PendingFile> pendingFiles)
    {
        if (pendingFiles.Count == 0)
        {
            return [];
        }

        try
        {
            var attachments = new List<Attachment>();

            foreach (var pendingFile in pendingFiles)
            {
                try
                {
                    if (pendingFile.Content.CanSeek && pendingFile.Content.Position > 0)
                    {
                        pendingFile.Content.Position = 0;
                    }

                    var storageId = await _convexClient.Files.UploadFileAsync(
                        pendingFile.Content,
                        pendingFile.ContentType,
                        pendingFile.Filename
                    );

                    attachments.Add(new Attachment(
                        StorageId: storageId,
                        Filename: pendingFile.Filename,
                        ContentType: pendingFile.ContentType,
                        Size: pendingFile.Size
                    ));
                }
                catch (ConvexException ex)
                {
                    Console.Error.WriteLine($"Error uploading file {pendingFile.Filename}: {ex.Message}");
                }
            }

            foreach (var file in pendingFiles)
            {
                file.Content.Dispose();
            }

            return attachments;
        }
        catch (ConvexException ex)
        {
            Console.Error.WriteLine($"Error in UploadFilesAsync: {ex.Message}");
            return [];
        }
    }

    public async Task<string> GetAttachmentUrlAsync(string storageId)
    {
        if (string.IsNullOrEmpty(storageId))
        {
            return "";
        }

        if (_attachmentUrlCache.TryGetValue(storageId, out var cachedUrl) && !string.IsNullOrEmpty(cachedUrl))
        {
            return cachedUrl;
        }

        try
        {
            var url = await _convexClient.Files.GetDownloadUrlAsync(storageId);

            if (!string.IsNullOrEmpty(url))
            {
                _attachmentUrlCache[storageId] = url;
            }

            return url;
        }
        catch (ConvexException ex)
        {
            Console.Error.WriteLine($"Error getting download URL for {storageId}: {ex.Message}");
            return "";
        }
    }

    public string GetAttachmentUrlSync(string storageId) => _attachmentUrlCache.TryGetValue(storageId, out var url) ? url : "";

    public async Task LoadAttachmentUrlsForMessagesAsync(List<Message> messages)
    {
        if (messages == null)
        {
            return;
        }

        var storageIds = messages
            .Where(m => m.Attachments != null && m.Attachments.Count > 0)
            .SelectMany(m => m.Attachments!)
            .Select(a => a.StorageId)
            .Where(id => !string.IsNullOrEmpty(id) && !_attachmentUrlCache.ContainsKey(id))
            .Distinct()
            .ToList();

        if (storageIds.Count == 0)
        {
            return;
        }

        var tasks = storageIds.Select(async storageId =>
        {
            try
            {
                var url = await _convexClient.Files.GetDownloadUrlAsync(storageId);
                if (!string.IsNullOrEmpty(url))
                {
                    _attachmentUrlCache[storageId] = url;
                }
            }
            catch (ConvexException ex)
            {
                Console.Error.WriteLine($"Error loading URL for {storageId}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }

    public string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

