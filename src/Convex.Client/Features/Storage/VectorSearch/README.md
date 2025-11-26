# VectorSearch Slice

## Purpose
Provides AI-powered similarity search, vector indexing, and embedding operations for Convex. Enables semantic search, recommendation systems, and AI-powered applications.

## Owner
- **Name:** TBD
- **Contact:** TBD

## Public API

```csharp
// Entry point
public class VectorSearchSlice : IConvexVectorSearch

// Vector similarity search
Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default)
Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default)

// Text-based search (auto-embedding)
Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default)
Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default)

// Embedding creation
Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)
Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)

// Index management
Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default)
Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default)
```

## Dependencies
- `Shared/Http/IHttpClientProvider` - For HTTP communication with Convex API
- `Shared/Serialization/IConvexSerializer` - For JSON serialization/deserialization

## Architecture

The slice follows the vertical slice pattern:

```
VectorSearch/
├── VectorSearchSlice.cs             ← Public entry point implementing IConvexVectorSearch
├── VectorSearchImplementation.cs    ← Internal implementation using Shared infrastructure
├── IConvexVectorSearch.cs           ← Interface definition and types
└── README.md                         ← This file
```

### Implementation Details

- **VectorSearchSlice**: Public facade implementing IConvexVectorSearch interface
- **VectorSearchImplementation**: Internal implementation that uses Shared infrastructure (IHttpClientProvider, IConvexSerializer)
- **Direct HTTP calls**: Uses `/api/query` and `/api/action` endpoints for vector operations
- **No CoreOperations dependency**: Fully migrated to Shared infrastructure

### Convex Functions Used

The slice calls these Convex functions:
- `vectorSearch:search` - Query for similarity search
- `vectorSearch:getIndexInfo` - Query for index information
- `vectorSearch:listIndices` - Query to list all indices
- `ai:createEmbedding` - Action to create text embeddings
- `ai:createEmbeddings` - Action to create batch embeddings

## Error Handling

The slice uses `ConvexVectorSearchException` with specific error types:
- `IndexNotFound` - Vector index doesn't exist
- `InvalidDimensions` - Vector dimensions don't match index
- `InvalidFilter` - Invalid filter expression
- `EmbeddingFailed` - Embedding generation failed
- `SearchFailed` - Search query failed
- `InvalidModel` - Invalid embedding model
- `RateLimitExceeded` - Embedding service rate limit hit
- `QuotaExceeded` - Vector search quota exceeded

## Usage Example

```csharp
// Access through ConvexClient
var client = new ConvexClient(deploymentUrl);

// Vector search
var vector = new float[] { 0.1f, 0.2f, 0.3f /* ... */ };
var results = await client.VectorSearchSlice.SearchAsync<Document>("embeddings", vector, limit: 10);

foreach (var result in results)
{
    Console.WriteLine($"ID: {result.Id}, Score: {result.Score}");
    Console.WriteLine($"Content: {result.Data.Content}");
}

// Text-based search (automatically creates embedding)
var textResults = await client.VectorSearchSlice.SearchByTextAsync<Document>(
    "embeddings",
    "What is machine learning?",
    limit: 5);

// Create embeddings
var embedding = await client.VectorSearchSlice.CreateEmbeddingAsync(
    "Sample text to embed",
    "text-embedding-ada-002");

// Batch embeddings
var embeddings = await client.VectorSearchSlice.CreateEmbeddingsAsync(
    new[] { "Text 1", "Text 2", "Text 3" },
    "text-embedding-ada-002");

// Search with filter
var filteredResults = await client.VectorSearchSlice.SearchAsync<Document, DocumentFilter>(
    "embeddings",
    vector,
    10,
    new DocumentFilter { Category = "technology" });

// Get index info
var indexInfo = await client.VectorSearchSlice.GetIndexInfoAsync("embeddings");
Console.WriteLine($"Index: {indexInfo.Name}, Dimension: {indexInfo.Dimension}, Metric: {indexInfo.Metric}");

// List all indices
var indices = await client.VectorSearchSlice.ListIndicesAsync();
foreach (var index in indices)
{
    Console.WriteLine($"{index.Name}: {index.VectorCount} vectors");
}
```

## Testing
See `tests/Slices/VectorSearchTests.cs`

## Migration Status
✅ Migrated to vertical slice architecture
✅ Uses Shared infrastructure (IHttpClientProvider, IConvexSerializer)
✅ No CoreOperations dependencies
✅ Architecture tests passing
