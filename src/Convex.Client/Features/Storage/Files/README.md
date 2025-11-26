# FileStorage Slice

## Purpose
Provides file upload, download, and management operations for Convex storage. Handles file operations including generating upload URLs, uploading files, downloading files, retrieving metadata, and deleting files.

## Owner
- **Name:** TBD
- **Contact:** TBD

## Public API

```csharp
// Entry point (implements IConvexFileStorage)
public class FileStorageSlice : IConvexFileStorage

// Generate upload URL
Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default)

// Upload file with generated URL
Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)

// Direct upload (combines URL generation and upload)
Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)

// Download file as stream
Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default)

// Get temporary download URL
Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default)

// Get file metadata
Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default)

// Delete file
Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default)
```

## Extension Methods

```csharp
// Upload from file path
Task<string> UploadFileAsync(this IConvexFileStorage fileStorage, string filePath, string? contentType = null, string? filename = null, CancellationToken cancellationToken = default)

// Download to file path
Task DownloadFileToPathAsync(this IConvexFileStorage fileStorage, string storageId, string localPath, CancellationToken cancellationToken = default)

// Upload/download text content
Task<string> UploadTextAsync(this IConvexFileStorage fileStorage, string textContent, string filename, Encoding? encoding = null, CancellationToken cancellationToken = default)
Task<string> DownloadTextAsync(this IConvexFileStorage fileStorage, string storageId, Encoding? encoding = null, CancellationToken cancellationToken = default)

// Upload/download byte arrays
Task<string> UploadBytesAsync(this IConvexFileStorage fileStorage, byte[] content, string contentType, string filename, CancellationToken cancellationToken = default)
Task<byte[]> DownloadBytesAsync(this IConvexFileStorage fileStorage, string storageId, CancellationToken cancellationToken = default)

// Check file existence
Task<bool> FileExistsAsync(this IConvexFileStorage fileStorage, string storageId, CancellationToken cancellationToken = default)
```

## Dependencies
- `Shared/Http/IHttpClientProvider` - For HTTP communication with Convex API
- `Shared/Serialization/IConvexSerializer` - For JSON serialization/deserialization
- `HttpClient` - For file upload/download operations

## Architecture

The slice follows the vertical slice pattern:

```
FileStorage/
├── FileStorageSlice.cs              ← Public entry point implementing IConvexFileStorage
├── FileStorageImplementation.cs     ← Internal implementation using Shared infrastructure
├── IConvexFileStorage.cs            ← Interface definition
├── ConvexFileStorageExtensions.cs   ← Extension methods for convenience
└── README.md                        ← This file
```

### Implementation Details

- **FileStorageSlice**: Public facade implementing IConvexFileStorage interface
- **FileStorageImplementation**: Internal implementation that uses Shared infrastructure (IHttpClientProvider, IConvexSerializer) to communicate with Convex API
- **Direct HTTP calls**: Uses `/api/action`, `/api/query`, and `/api/mutation` endpoints for storage operations
- **No CoreOperations dependency**: Fully migrated to Shared infrastructure

### Convex Functions Used

The slice calls these Convex storage functions:
- `storage:generateUploadUrl` - Action to generate temporary upload URL
- `storage:getUrl` - Action to get temporary download URL
- `storage:getMetadata` - Query to retrieve file metadata
- `storage:deleteFile` - Mutation to delete a file

## Error Handling

The slice uses `ConvexFileStorageException` with specific error types:
- `FileNotFound` - File doesn't exist in storage
- `UploadFailed` - Upload operation failed
- `DownloadFailed` - Download operation failed
- `FileTooLarge` - File exceeds size limit
- `InvalidFile` - Invalid file type or content
- `QuotaExceeded` - Storage quota exceeded
- `AccessDenied` - Permission denied
- `InvalidStorageId` - Malformed storage ID

## Usage Example

```csharp
// Access through ConvexClient
var client = new ConvexClient(deploymentUrl);

// Upload a file
using var fileStream = File.OpenRead("photo.jpg");
var storageId = await client.FileStorage.UploadFileAsync(
    fileStream,
    "image/jpeg",
    "photo.jpg");

// Download a file
var downloadStream = await client.FileStorage.DownloadFileAsync(storageId);

// Get file metadata
var metadata = await client.FileStorage.GetFileMetadataAsync(storageId);
Console.WriteLine($"File: {metadata.Filename}, Size: {metadata.Size} bytes");

// Delete file
var deleted = await client.FileStorage.DeleteFileAsync(storageId);

// Using extension methods
await client.FileStorage.UploadFileAsync("local/path/file.pdf");
await client.FileStorage.DownloadFileToPathAsync(storageId, "output/file.pdf");
var text = await client.FileStorage.DownloadTextAsync(storageId);
```

## Testing
See `tests/Slices/FileStorageTests.cs`

## Migration Status
✅ Migrated to vertical slice architecture
✅ Uses Shared infrastructure (IHttpClientProvider, IConvexSerializer)
✅ No CoreOperations dependencies
✅ Architecture tests passing
