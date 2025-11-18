namespace Convex.Client.Slices.FileStorage;

/// <summary>
/// Interface for Convex file storage operations.
/// Provides upload, download, and file management capabilities.
/// </summary>
public interface IConvexFileStorage
{
    /// <summary>
    /// Generates a temporary URL for uploading a file to Convex storage.
    /// </summary>
    /// <param name="filename">Optional filename for the uploaded file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A upload URL response containing the upload URL and storage ID.</returns>
    Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to Convex storage using a generated upload URL.
    /// </summary>
    /// <param name="uploadUrl">The upload URL obtained from GenerateUploadUrlAsync.</param>
    /// <param name="fileContent">The file content to upload.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="filename">Optional filename for the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The storage ID of the uploaded file.</returns>
    Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file directly to Convex storage (combines URL generation and upload).
    /// </summary>
    /// <param name="fileContent">The file content to upload.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="filename">Optional filename for the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The storage ID of the uploaded file.</returns>
    Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from Convex storage.
    /// </summary>
    /// <param name="storageId">The storage ID of the file to download.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A stream containing the file content.</returns>
    Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary download URL for a file in Convex storage.
    /// </summary>
    /// <param name="storageId">The storage ID of the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A temporary URL that can be used to download the file.</returns>
    Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information about a stored file.
    /// </summary>
    /// <param name="storageId">The storage ID of the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>File metadata including size, content type, and filename.</returns>
    Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from Convex storage.
    /// </summary>
    /// <param name="storageId">The storage ID of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the file was successfully deleted.</returns>
    Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from generating an upload URL.
/// </summary>
public class ConvexUploadUrlResponse
{
    /// <summary>
    /// The temporary URL to use for uploading the file.
    /// </summary>
    public required string UploadUrl { get; init; }

    /// <summary>
    /// The storage ID that will be assigned to the file after upload.
    /// </summary>
    public required string StorageId { get; init; }
}

/// <summary>
/// Metadata information about a file in Convex storage.
/// </summary>
public class ConvexFileMetadata
{
    /// <summary>
    /// The storage ID of the file.
    /// </summary>
    public required string StorageId { get; init; }

    /// <summary>
    /// The filename, if provided during upload.
    /// </summary>
    public string? Filename { get; init; }

    /// <summary>
    /// The MIME type of the file.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// The timestamp when the file was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; init; }

    /// <summary>
    /// The SHA-256 hash of the file content.
    /// </summary>
    public string? Sha256 { get; init; }
}

/// <summary>
/// Exception thrown when file storage operations fail.
/// </summary>
public class ConvexFileStorageException(FileStorageErrorType errorType, string message, string? storageId = null, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// The type of file storage error that occurred.
    /// </summary>
    public FileStorageErrorType ErrorType { get; } = errorType;

    /// <summary>
    /// The storage ID related to the error, if applicable.
    /// </summary>
    public string? StorageId { get; } = storageId;
}

/// <summary>
/// Types of file storage errors.
/// </summary>
public enum FileStorageErrorType
{
    /// <summary>
    /// File not found in storage.
    /// </summary>
    FileNotFound,

    /// <summary>
    /// Upload failed due to network or server error.
    /// </summary>
    UploadFailed,

    /// <summary>
    /// Download failed due to network or server error.
    /// </summary>
    DownloadFailed,

    /// <summary>
    /// File size exceeds maximum allowed size.
    /// </summary>
    FileTooLarge,

    /// <summary>
    /// Invalid file type or content.
    /// </summary>
    InvalidFile,

    /// <summary>
    /// Storage quota exceeded.
    /// </summary>
    QuotaExceeded,

    /// <summary>
    /// Access denied to the file.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Invalid storage ID format.
    /// </summary>
    InvalidStorageId
}
