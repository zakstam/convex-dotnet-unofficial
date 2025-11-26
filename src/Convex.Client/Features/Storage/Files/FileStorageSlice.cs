using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Storage.Files;

/// <summary>
/// FileStorage slice - provides file upload/download operations for Convex storage.
/// This is a self-contained vertical slice that handles all file storage functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FileStorageSlice"/> class.
/// </remarks>
/// <param name="httpProvider">The HTTP client provider for making requests.</param>
/// <param name="serializer">The serializer for handling Convex JSON format.</param>
/// <param name="uploadHttpClient">HTTP client for upload/download operations.</param>
/// <param name="logger">Optional logger for debug logging.</param>
/// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
public class FileStorageSlice(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    HttpClient uploadHttpClient,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IConvexFileStorage
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly HttpClient _uploadHttpClient = uploadHttpClient ?? throw new ArgumentNullException(nameof(uploadHttpClient));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    /// <inheritdoc />
    public Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.GenerateUploadUrlAsync(filename, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.UploadFileAsync(uploadUrl, fileContent, contentType, filename, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.UploadFileAsync(fileContent, contentType, filename, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.DownloadFileAsync(storageId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.GetDownloadUrlAsync(storageId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.GetFileMetadataAsync(storageId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var implementation = new FileStorageImplementation(_httpProvider, _serializer, _uploadHttpClient, _logger, _enableDebugLogging);
        return implementation.DeleteFileAsync(storageId, cancellationToken);
    }
}
