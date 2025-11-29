namespace Convex.Client.Features.Storage.Files;

/// <summary>
/// Interface for Convex file storage operations.
/// Provides upload, download, and file management capabilities for storing files in Convex.
/// Files are stored securely and can be accessed via storage IDs or temporary URLs.
/// </summary>
/// <remarks>
/// <para>
/// File storage operations include:
/// <list type="bullet">
/// <item>Uploading files (images, documents, etc.)</item>
/// <item>Downloading files by storage ID</item>
/// <item>Getting temporary download URLs for browser access</item>
/// <item>Retrieving file metadata (size, content type, etc.)</item>
/// <item>Deleting files</item>
/// </list>
/// </para>
/// <para>
/// Files are uploaded to Convex storage and assigned a unique storage ID that can be stored
/// in your database and used to retrieve the file later.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Upload a file
/// using var fileStream = File.OpenRead("image.jpg");
/// var storageId = await client.Files.UploadFileAsync(
///     fileStream,
///     contentType: "image/jpeg",
///     filename: "image.jpg"
/// );
///
/// // Get download URL for browser display
/// var downloadUrl = await client.Files.GetDownloadUrlAsync(storageId);
/// Console.WriteLine($"File URL: {downloadUrl}");
///
/// // Download file
/// var downloadedStream = await client.Files.DownloadFileAsync(storageId);
/// </code>
/// </example>
/// <seealso cref="FileStorageSlice"/>
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
    /// This is the recommended method for most use cases as it handles both URL generation and upload in one call.
    /// </summary>
    /// <param name="fileContent">The file content to upload as a stream. The stream will be read from the current position.</param>
    /// <param name="contentType">The MIME type of the file (e.g., "image/jpeg", "application/pdf", "text/plain").</param>
    /// <param name="filename">Optional filename for the file. Used for metadata and download suggestions.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with the storage ID of the uploaded file. Store this ID in your database to retrieve the file later.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileContent"/> is null or <paramref name="contentType"/> is null or empty.</exception>
    /// <exception cref="ConvexFileStorageException">Thrown when the upload fails (network error, quota exceeded, etc.).</exception>
    /// <remarks>
    /// <para>
    /// This method automatically generates an upload URL and uploads the file in one operation.
    /// For more control over the upload process, use <see cref="GenerateUploadUrlAsync(string?, CancellationToken)"/>
    /// and <see cref="UploadFileAsync(string, Stream, string, string?, CancellationToken)"/> separately.
    /// </para>
    /// <para>
    /// The returned storage ID is a unique identifier that can be stored in your database
    /// and used to retrieve the file later using <see cref="DownloadFileAsync(string, CancellationToken)"/>
    /// or <see cref="GetDownloadUrlAsync(string, CancellationToken)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload an image file
    /// using var imageStream = File.OpenRead("profile.jpg");
    /// var storageId = await client.Files.UploadFileAsync(
    ///     imageStream,
    ///     contentType: "image/jpeg",
    ///     filename: "profile.jpg"
    /// );
    ///
    /// // Store the storage ID in your database
    /// await client.Mutate&lt;User&gt;("functions/updateUser")
    ///     .WithArgs(new { userId = "user123", profilePictureId = storageId })
    ///     .ExecuteAsync();
    ///
    /// // Upload from memory
    /// var textBytes = Encoding.UTF8.GetBytes("Hello, World!");
    /// using var textStream = new MemoryStream(textBytes);
    /// var textStorageId = await client.Files.UploadFileAsync(
    ///     textStream,
    ///     contentType: "text/plain",
    ///     filename: "hello.txt"
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="GenerateUploadUrlAsync(string?, CancellationToken)"/>
    /// <seealso cref="DownloadFileAsync(string, CancellationToken)"/>
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
    /// The URL can be used directly in browsers or HTML img/src tags for displaying files.
    /// URLs are temporary and may expire after a period of time.
    /// </summary>
    /// <param name="storageId">The storage ID of the file to get a download URL for. Must not be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with a temporary URL that can be used to download or display the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="storageId"/> is null or empty.</exception>
    /// <exception cref="ConvexFileStorageException">Thrown when the file is not found or access is denied.</exception>
    /// <remarks>
    /// <para>
    /// This method is useful when you want to display files in a browser (e.g., images in HTML)
    /// or provide download links to users. The URL is temporary and may expire.
    /// </para>
    /// <para>
    /// For server-side file processing, use <see cref="DownloadFileAsync(string, CancellationToken)"/> instead
    /// to get a stream directly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get download URL for displaying in browser
    /// var storageId = "storage123"; // Retrieved from database
    /// var downloadUrl = await client.Files.GetDownloadUrlAsync(storageId);
    ///
    /// // Use in HTML
    /// Console.WriteLine($"&lt;img src=\"{downloadUrl}\" /&gt;");
    ///
    /// // Or return in API response
    /// return new { imageUrl = downloadUrl };
    /// </code>
    /// </example>
    /// <seealso cref="DownloadFileAsync(string, CancellationToken)"/>
    /// <seealso cref="GetFileMetadataAsync(string, CancellationToken)"/>
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
