using System.Net.Http.Headers;
using System.Text.Json;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Slices.FileStorage;

/// <summary>
/// Internal implementation of Convex file storage operations using Shared infrastructure.
/// </summary>
internal sealed class FileStorageImplementation(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    HttpClient uploadHttpClient,
    ILogger? logger = null,
    bool enableDebugLogging = false)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly HttpClient _uploadHttpClient = uploadHttpClient ?? throw new ArgumentNullException(nameof(uploadHttpClient));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    public async Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[FileStorage] Starting upload URL generation: Filename: {Filename}", filename ?? "null");
        }

        try
        {
            // Call the Convex action to generate upload URL
            var response = await ExecuteActionAsync<JsonElement>(
                "storage:generateUploadUrl",
                filename != null ? new { filename } : null,
                cancellationToken);

            if (!response.TryGetProperty("uploadUrl", out var uploadUrlElement) ||
                !response.TryGetProperty("storageId", out var storageIdElement))
            {
                var error = new ConvexFileStorageException(FileStorageErrorType.UploadFailed,
                    "Invalid response from generateUploadUrl action: missing uploadUrl or storageId");
                stopwatch.Stop();
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] Upload URL generation failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var uploadUrl = uploadUrlElement.GetString() ?? throw new ConvexFileStorageException(FileStorageErrorType.UploadFailed, "Upload URL is null");
            var storageId = storageIdElement.GetString() ?? throw new ConvexFileStorageException(FileStorageErrorType.UploadFailed, "Storage ID is null");

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] Upload URL generated successfully: StorageId: {StorageId}, Filename: {Filename}, Duration: {DurationMs}ms",
                    storageId, filename ?? "null", stopwatch.Elapsed.TotalMilliseconds);
            }

            return new ConvexUploadUrlResponse
            {
                UploadUrl = uploadUrl,
                StorageId = storageId
            };
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            var error = new ConvexFileStorageException(FileStorageErrorType.UploadFailed, "Failed to generate upload URL", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] Upload URL generation failed: Filename: {Filename}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    filename ?? "null", stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)
    {
        if (fileContent == null)
        {
            throw new ArgumentNullException(nameof(fileContent), "File content stream cannot be null");
        }

        var stopwatch = Stopwatch.StartNew();
        long? fileSize = null;

        try
        {
            // Try to get file size if stream supports it
            if (fileContent.CanSeek)
            {
                fileSize = fileContent.Length;
            }

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] Starting file upload: UploadUrl: {UploadUrl}, Filename: {Filename}, ContentType: {ContentType}, Size: {Size}",
                    uploadUrl, filename ?? "null", contentType, fileSize?.ToString() ?? "unknown");
            }

            // Prepare the file content for upload
            using var content = new StreamContent(fileContent);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            if (filename != null)
            {
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = filename
                };
            }

            // Upload the file to the generated URL
            var response = await _uploadHttpClient.PostAsync(uploadUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.ReadContentAsStringAsync(cancellationToken);
                var errorType = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.RequestEntityTooLarge => FileStorageErrorType.FileTooLarge,
                    System.Net.HttpStatusCode.UnsupportedMediaType => FileStorageErrorType.InvalidFile,
                    System.Net.HttpStatusCode.Forbidden => FileStorageErrorType.AccessDenied,
                    System.Net.HttpStatusCode.InsufficientStorage => FileStorageErrorType.QuotaExceeded,
                    _ => FileStorageErrorType.UploadFailed
                };

                stopwatch.Stop();
                var error = new ConvexFileStorageException(errorType, $"File upload failed: {response.StatusCode} - {errorContent}");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] File upload failed: UploadUrl: {UploadUrl}, StatusCode: {StatusCode}, ErrorContent: {ErrorContent}, Duration: {DurationMs}ms",
                        uploadUrl, response.StatusCode, errorContent, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            // Extract storage ID from response
            var responseContent = await response.ReadContentAsStringAsync(cancellationToken);
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (!responseJson.TryGetProperty("storageId", out var storageIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexFileStorageException(FileStorageErrorType.UploadFailed,
                    "Upload response missing storageId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] File upload failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        responseContent, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var storageId = storageIdElement.GetString();
            if (string.IsNullOrEmpty(storageId))
            {
                stopwatch.Stop();
                var error = new ConvexFileStorageException(FileStorageErrorType.UploadFailed,
                    "Upload response contains empty storageId");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] File upload failed: Empty storageId, Response: {Response}, Duration: {DurationMs}ms",
                        responseContent, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] File upload completed successfully: StorageId: {StorageId}, Filename: {Filename}, Size: {Size}, Duration: {DurationMs}ms",
                    storageId, filename ?? "null", fileSize?.ToString() ?? "unknown", stopwatch.Elapsed.TotalMilliseconds);
            }

            return storageId;
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            var error = new ConvexFileStorageException(FileStorageErrorType.UploadFailed, "Unexpected error during file upload", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] File upload failed: UploadUrl: {UploadUrl}, Filename: {Filename}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    uploadUrl, filename ?? "null", stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)
    {
        var uploadUrlResponse = await GenerateUploadUrlAsync(filename, cancellationToken);
        return await UploadFileAsync(uploadUrlResponse.UploadUrl, fileContent, contentType, filename, cancellationToken);
    }

    public async Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[FileStorage] Starting file download: StorageId: {StorageId}", storageId);
        }

        try
        {
            ValidateStorageId(storageId);

            var downloadUrl = await GetDownloadUrlAsync(storageId, cancellationToken);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] Download URL retrieved: StorageId: {StorageId}, DownloadUrl: {DownloadUrl}", storageId, downloadUrl);
            }

            var response = await _uploadHttpClient.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorType = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => FileStorageErrorType.FileNotFound,
                    System.Net.HttpStatusCode.Forbidden => FileStorageErrorType.AccessDenied,
                    _ => FileStorageErrorType.DownloadFailed
                };

                stopwatch.Stop();
                var error = new ConvexFileStorageException(errorType, $"File download failed: {response.StatusCode}", storageId);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] File download failed: StorageId: {StorageId}, StatusCode: {StatusCode}, Duration: {DurationMs}ms",
                        storageId, response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var contentLength = response.Content?.Headers.ContentLength;
            stopwatch.Stop();

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] File download completed successfully: StorageId: {StorageId}, Size: {Size}, Duration: {DurationMs}ms",
                    storageId, contentLength?.ToString() ?? "unknown", stopwatch.Elapsed.TotalMilliseconds);
            }

            return await response.ReadContentAsStreamAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            var error = new ConvexFileStorageException(FileStorageErrorType.DownloadFailed, "Unexpected error during file download", storageId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] File download failed: StorageId: {StorageId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    storageId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[FileStorage] Starting download URL retrieval: StorageId: {StorageId}", storageId);
        }

        try
        {
            ValidateStorageId(storageId);

            var response = await ExecuteActionAsync<JsonElement>("storage:getUrl", new { storageId }, cancellationToken);

            if (!response.TryGetProperty("url", out var urlElement))
            {
                stopwatch.Stop();
                var error = new ConvexFileStorageException(FileStorageErrorType.FileNotFound,
                    "File not found or invalid storage ID", storageId);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] Download URL retrieval failed: Invalid response, StorageId: {StorageId}, Response: {Response}, Duration: {DurationMs}ms",
                        storageId, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var url = urlElement.GetString();
            if (string.IsNullOrEmpty(url))
            {
                stopwatch.Stop();
                var error = new ConvexFileStorageException(FileStorageErrorType.FileNotFound,
                    "Empty download URL returned", storageId);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] Download URL retrieval failed: Empty URL, StorageId: {StorageId}, Duration: {DurationMs}ms",
                        storageId, stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] Download URL retrieved successfully: StorageId: {StorageId}, Duration: {DurationMs}ms",
                    storageId, stopwatch.Elapsed.TotalMilliseconds);
            }

            return url;
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            var error = new ConvexFileStorageException(FileStorageErrorType.DownloadFailed, "Failed to get download URL", storageId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] Download URL retrieval failed: StorageId: {StorageId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    storageId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[FileStorage] Starting file metadata retrieval: StorageId: {StorageId}", storageId);
        }

        try
        {
            ValidateStorageId(storageId);

            var response = await ExecuteQueryAsync<JsonElement>("storage:getMetadata", new { storageId }, cancellationToken);

            if (!response.TryGetProperty("storageId", out var storageIdElement))
            {
                stopwatch.Stop();
                var error = new ConvexFileStorageException(FileStorageErrorType.FileNotFound,
                    "File not found or invalid storage ID", storageId);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[FileStorage] File metadata retrieval failed: Invalid response, StorageId: {StorageId}, Response: {Response}, Duration: {DurationMs}ms",
                        storageId, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var metadata = new ConvexFileMetadata
            {
                StorageId = storageIdElement.GetString() ?? storageId,
                Filename = response.TryGetProperty("filename", out var filenameElement) ? filenameElement.GetString() : null,
                ContentType = response.TryGetProperty("contentType", out var contentTypeElement) ? contentTypeElement.GetString() : null,
                Size = response.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0,
                UploadedAt = response.TryGetProperty("uploadedAt", out var uploadedAtElement)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(uploadedAtElement.GetInt64())
                    : DateTimeOffset.MinValue,
                Sha256 = response.TryGetProperty("sha256", out var sha256Element) ? sha256Element.GetString() : null
            };

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] File metadata retrieved successfully: StorageId: {StorageId}, Filename: {Filename}, ContentType: {ContentType}, Size: {Size}, Duration: {DurationMs}ms",
                    metadata.StorageId, metadata.Filename ?? "null", metadata.ContentType ?? "null", metadata.Size, stopwatch.Elapsed.TotalMilliseconds);
            }

            return metadata;
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            var error = new ConvexFileStorageException(FileStorageErrorType.DownloadFailed, "Failed to get file metadata", storageId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] File metadata retrieval failed: StorageId: {StorageId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    storageId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[FileStorage] Starting file deletion: StorageId: {StorageId}", storageId);
        }

        try
        {
            ValidateStorageId(storageId);

            var response = await ExecuteMutationAsync<JsonElement>("storage:deleteFile", new { storageId }, cancellationToken);

            var deleted = response.TryGetProperty("deleted", out var deletedElement) && deletedElement.GetBoolean();

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] File deletion completed: StorageId: {StorageId}, Deleted: {Deleted}, Duration: {DurationMs}ms",
                    storageId, deleted, stopwatch.Elapsed.TotalMilliseconds);
            }

            return deleted;
        }
        catch (ConvexFileStorageException ex) when (ex.ErrorType == FileStorageErrorType.FileNotFound)
        {
            stopwatch.Stop();
            // File not found is a valid case - return false instead of throwing
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[FileStorage] File deletion: File not found (returning false): StorageId: {StorageId}, Duration: {DurationMs}ms",
                    storageId, stopwatch.Elapsed.TotalMilliseconds);
            }
            return false;
        }
        catch (Exception ex) when (ex is not ConvexFileStorageException)
        {
            stopwatch.Stop();
            // Wrap unexpected exceptions in ConvexFileStorageException and throw
            var error = new ConvexFileStorageException(FileStorageErrorType.DownloadFailed,
                "Failed to delete file", storageId, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[FileStorage] File deletion failed: StorageId: {StorageId}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    storageId, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    private async Task<TResult> ExecuteActionAsync<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildActionRequest(
            _httpProvider.DeploymentUrl,
            functionName,
            args,
            _serializer);

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            functionName,
            "action",
            _serializer,
            cancellationToken);
    }

    private async Task<TResult> ExecuteQueryAsync<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildQueryRequest(
            _httpProvider.DeploymentUrl,
            functionName,
            args,
            _serializer);

        // Note: FileStorage uses status/value wrapper format for queries
        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            functionName,
            "query",
            _serializer,
            cancellationToken);
    }

    private async Task<TResult> ExecuteMutationAsync<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildMutationRequest(
            _httpProvider.DeploymentUrl,
            functionName,
            args,
            _serializer);

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            functionName,
            "mutation",
            _serializer,
            cancellationToken);
    }

    private static void ValidateStorageId(string storageId)
    {
        if (string.IsNullOrEmpty(storageId))
        {
            throw new ConvexFileStorageException(FileStorageErrorType.InvalidStorageId, "Storage ID cannot be null or empty");
        }

        // Basic validation - Convex storage IDs typically start with "kg" or similar prefixes
        if (storageId.Length < 10)
        {
            throw new ConvexFileStorageException(FileStorageErrorType.InvalidStorageId, "Invalid storage ID format", storageId);
        }
    }
}
