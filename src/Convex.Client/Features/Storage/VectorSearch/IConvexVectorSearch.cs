using System.Text.Json;

namespace Convex.Client.Features.Storage.VectorSearch;

/// <summary>
/// Interface for Convex vector search operations.
/// Provides similarity search, vector indexing, and AI-powered search capabilities.
/// Use this for semantic search, recommendations, and finding similar content based on embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Vector search allows you to find similar items based on their vector embeddings rather than exact matches.
/// This is useful for:
/// <list type="bullet">
/// <item>Semantic search (finding documents similar in meaning)</item>
/// <item>Recommendation systems</item>
/// <item>Content similarity matching</item>
/// <item>AI-powered search features</item>
/// </list>
/// </para>
/// <para>
/// Before using vector search, you need to:
/// <list type="number">
/// <item>Create vector embeddings for your data</item>
/// <item>Store embeddings in your Convex database</item>
/// <item>Create a vector index in your Convex backend</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Search by text (automatically creates embedding)
/// var results = await client.VectorSearchSlice.SearchByTextAsync&lt;Product&gt;(
///     indexName: "product_embeddings",
///     text: "laptop computer",
///     limit: 10
/// );
///
/// foreach (var result in results)
/// {
///     Console.WriteLine($"{result.Data.Name} (similarity: {result.Score:F3})");
/// }
///
/// // Search with custom vector
/// var queryVector = await client.VectorSearchSlice.CreateEmbeddingAsync("laptop computer");
/// var vectorResults = await client.VectorSearchSlice.SearchAsync&lt;Product&gt;(
///     indexName: "product_embeddings",
///     vector: queryVector,
///     limit: 10
/// );
/// </code>
/// </example>
/// <seealso cref="VectorSearchSlice"/>
public interface IConvexVectorSearch
{
    /// <summary>
    /// Performs a similarity search using a vector query without a filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search using a vector query with a strongly-typed filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;

    /// <summary>
    /// Performs a similarity search using text that will be converted to a vector without a filter.
    /// This is the easiest way to perform semantic search - just provide text and the system will
    /// automatically create an embedding and search for similar items.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the vector index. This should match the document type in your Convex database.</typeparam>
    /// <param name="indexName">The name of the vector index to search (e.g., "product_embeddings"). Must match an index defined in your Convex backend.</param>
    /// <param name="text">The text to search for. This will be converted to a vector embedding using the specified model.</param>
    /// <param name="embeddingModel">The embedding model to use for converting text to vectors. Default is "text-embedding-ada-002".</param>
    /// <param name="limit">Maximum number of results to return. Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes with a collection of search results ordered by similarity score (highest first).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="indexName"/> or <paramref name="text"/> is null or empty.</exception>
    /// <exception cref="ConvexVectorSearchException">Thrown when the search fails (index not found, invalid model, etc.).</exception>
    /// <remarks>
    /// <para>
    /// This method automatically creates an embedding from the text and then searches for similar vectors.
    /// It's equivalent to calling <see cref="CreateEmbeddingAsync(string, string, CancellationToken)"/> followed by
    /// <see cref="SearchAsync{T}(string, float[], int, CancellationToken)"/>, but more convenient.
    /// </para>
    /// <para>
    /// Results are ordered by similarity score (cosine similarity, euclidean distance, or dot product depending on index configuration).
    /// Higher scores indicate greater similarity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Search for similar products by description
    /// var results = await client.VectorSearchSlice.SearchByTextAsync&lt;Product&gt;(
    ///     indexName: "product_embeddings",
    ///     text: "wireless headphones with noise cancellation",
    ///     limit: 5
    /// );
    ///
    /// foreach (var result in results)
    /// {
    ///     Console.WriteLine($"{result.Data.Name}: {result.Score:F3}");
    /// }
    ///
    /// // Search documents by query
    /// var documentResults = await client.VectorSearchSlice.SearchByTextAsync&lt;Document&gt;(
    ///     indexName: "document_embeddings",
    ///     text: "machine learning algorithms",
    ///     embeddingModel: "text-embedding-ada-002",
    ///     limit: 10
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="SearchByTextAsync{TResult, TFilter}(string, string, string, int, TFilter, CancellationToken)"/>
    /// <seealso cref="CreateEmbeddingAsync(string, string, CancellationToken)"/>
    Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search using text that will be converted to a vector with a strongly-typed filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;

    /// <summary>
    /// Creates a text embedding using the specified model.
    /// </summary>
    Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates embeddings for multiple texts in a single batch operation.
    /// </summary>
    Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a vector index.
    /// </summary>
    Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available vector indices.
    /// </summary>
    Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a result from a vector similarity search.
/// </summary>
public class VectorSearchResult<T>
{
    public required string Id { get; init; }
    public required float Score { get; init; }
    public required T Data { get; init; }
    public float[]? Vector { get; init; }
    public Dictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>
/// Information about a vector index.
/// </summary>
public class VectorIndexInfo
{
    public required string Name { get; init; }
    public required int Dimension { get; init; }
    public required VectorDistanceMetric Metric { get; init; }
    public long VectorCount { get; init; }
    public required string Table { get; init; }
    public required string VectorField { get; init; }
    public string? FilterField { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Distance metrics used for vector similarity calculations.
/// </summary>
public enum VectorDistanceMetric
{
    Cosine,
    Euclidean,
    DotProduct
}

/// <summary>
/// Exception thrown when vector search operations fail.
/// </summary>
public class ConvexVectorSearchException(VectorSearchErrorType errorType, string message, string? indexName = null, Exception? innerException = null) : Exception(message, innerException)
{
    public VectorSearchErrorType ErrorType { get; } = errorType;
    public string? IndexName { get; } = indexName;
}

/// <summary>
/// Types of vector search errors.
/// </summary>
public enum VectorSearchErrorType
{
    IndexNotFound,
    InvalidDimensions,
    InvalidFilter,
    EmbeddingFailed,
    SearchFailed,
    InvalidModel,
    RateLimitExceeded,
    QuotaExceeded
}
