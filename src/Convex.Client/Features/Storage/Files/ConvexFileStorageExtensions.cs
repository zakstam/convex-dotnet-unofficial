using System.Text;

namespace Convex.Client.Features.Storage.Files;

/// <summary>
/// Extension methods for convenient file storage operations.
/// </summary>
public static class ConvexFileStorageExtensions
{
    /// <summary>
    /// Uploads a file from a local file path.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="filePath">The path to the local file.</param>
    /// <param name="contentType">The MIME type of the file. If null, attempts to detect from file extension.</param>
    /// <param name="filename">Custom filename to use. If null, uses the original filename.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The storage ID of the uploaded file.</returns>
    public static async Task<string> UploadFileAsync(this IConvexFileStorage fileStorage, string filePath, string? contentType = null, string? filename = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var actualFilename = filename ?? Path.GetFileName(filePath);
        var actualContentType = contentType ?? GetContentTypeFromExtension(Path.GetExtension(filePath));

        using var fileStream = File.OpenRead(filePath);
        return await fileStorage.UploadFileAsync(fileStream, actualContentType, actualFilename, cancellationToken);
    }

    /// <summary>
    /// Downloads a file and saves it to a local path.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="storageId">The storage ID of the file to download.</param>
    /// <param name="localPath">The local path where to save the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public static async Task DownloadFileToPathAsync(this IConvexFileStorage fileStorage, string storageId, string localPath, CancellationToken cancellationToken = default)
    {
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        using var downloadStream = await fileStorage.DownloadFileAsync(storageId, cancellationToken);
        using var fileStream = File.Create(localPath);
        await downloadStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <summary>
    /// Uploads text content as a file.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="textContent">The text content to upload.</param>
    /// <param name="filename">The filename for the uploaded content.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The storage ID of the uploaded file.</returns>
    public static async Task<string> UploadTextAsync(this IConvexFileStorage fileStorage, string textContent, string filename, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(textContent);

        using var memoryStream = new MemoryStream(bytes);
        return await fileStorage.UploadFileAsync(memoryStream, "text/plain", filename, cancellationToken);
    }

    /// <summary>
    /// Downloads a file as text content.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="storageId">The storage ID of the file to download.</param>
    /// <param name="encoding">The text encoding to use for reading. Defaults to UTF-8.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The text content of the file.</returns>
    public static async Task<string> DownloadTextAsync(this IConvexFileStorage fileStorage, string storageId, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        encoding ??= Encoding.UTF8;

        using var downloadStream = await fileStorage.DownloadFileAsync(storageId, cancellationToken);
        using var reader = new StreamReader(downloadStream, encoding);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Uploads byte array content as a file.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="content">The byte array content to upload.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="filename">The filename for the uploaded content.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The storage ID of the uploaded file.</returns>
    public static async Task<string> UploadBytesAsync(this IConvexFileStorage fileStorage, byte[] content, string contentType, string filename, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream(content);
        return await fileStorage.UploadFileAsync(memoryStream, contentType, filename, cancellationToken);
    }

    /// <summary>
    /// Downloads a file as a byte array.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="storageId">The storage ID of the file to download.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The byte array content of the file.</returns>
    public static async Task<byte[]> DownloadBytesAsync(this IConvexFileStorage fileStorage, string storageId, CancellationToken cancellationToken = default)
    {
        using var downloadStream = await fileStorage.DownloadFileAsync(storageId, cancellationToken);
        using var memoryStream = new MemoryStream();
        await downloadStream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="fileStorage">The file storage instance.</param>
    /// <param name="storageId">The storage ID of the file to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public static async Task<bool> FileExistsAsync(this IConvexFileStorage fileStorage, string storageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await fileStorage.GetFileMetadataAsync(storageId, cancellationToken);
            return true;
        }
        catch (ConvexFileStorageException ex) when (ex.ErrorType == FileStorageErrorType.FileNotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the content type for a file extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without the dot).</param>
    /// <returns>The MIME content type, or "application/octet-stream" if unknown.</returns>
    private static string GetContentTypeFromExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "application/octet-stream";

        extension = extension.TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            // Text files
            "txt" => "text/plain",
            "html" or "htm" => "text/html",
            "css" => "text/css",
            "js" => "application/javascript",
            "json" => "application/json",
            "xml" => "application/xml",
            "csv" => "text/csv",
            "md" => "text/markdown",

            // Images
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "ico" => "image/x-icon",

            // Documents
            "pdf" => "application/pdf",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

            // Archives
            "zip" => "application/zip",
            "tar" => "application/x-tar",
            "gz" => "application/gzip",
            "7z" => "application/x-7z-compressed",

            // Audio/Video
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "ogg" => "audio/ogg",
            "mp4" => "video/mp4",
            "avi" => "video/x-msvideo",
            "mov" => "video/quicktime",

            // Default
            _ => "application/octet-stream"
        };
    }
}
