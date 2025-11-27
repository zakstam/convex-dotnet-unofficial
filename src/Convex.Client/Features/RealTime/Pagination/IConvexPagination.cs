using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Features.RealTime.Pagination;

/// <summary>
/// Interface for creating paginated queries.
/// Provides cursor-based pagination for loading large datasets in manageable pages.
/// Use this when you need to load data incrementally rather than all at once.
/// </summary>
/// <remarks>
/// <para>
/// Pagination is useful for:
/// <list type="bullet">
/// <item>Loading large datasets incrementally</item>
/// <item>Implementing "load more" functionality</item>
/// <item>Reducing initial load time</item>
/// <item>Handling datasets that don't fit in memory</item>
/// </list>
/// </para>
/// <para>
/// Pagination uses cursor-based pagination, which is more efficient than offset-based pagination
/// and handles concurrent data changes better.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a paginated query
/// var paginator = client.PaginationSlice
///     .Query&lt;Todo&gt;("functions/listTodos")
///     .WithPageSize(20)
///     .Build();
///
/// // Load first page
/// var firstPage = await paginator.LoadNextAsync();
/// Console.WriteLine($"Loaded {firstPage.Count} items");
///
/// // Load more pages
/// while (paginator.HasMore)
/// {
///     var nextPage = await paginator.LoadNextAsync();
///     Console.WriteLine($"Loaded {nextPage.Count} more items");
/// }
/// </code>
/// </example>
/// <seealso cref="IPaginationBuilder{T}"/>
/// <seealso cref="IPaginator{T}"/>
public interface IConvexPagination
{
    /// <summary>
    /// Creates a pagination builder for the specified query function.
    /// </summary>
    IPaginationBuilder<T> Query<T>(string functionName);
}

/// <summary>
/// Fluent builder for creating paginated queries.
/// </summary>
public interface IPaginationBuilder<T>
{
    /// <summary>
    /// Sets the page size (number of items per page).
    /// </summary>
    IPaginationBuilder<T> WithPageSize(int pageSize);

    /// <summary>
    /// Sets the arguments to pass to the Convex function.
    /// </summary>
    IPaginationBuilder<T> WithArgs<TArgs>(TArgs args) where TArgs : notnull;

    /// <summary>
    /// Sets the arguments using a builder function for type-safe construction.
    /// </summary>
    IPaginationBuilder<T> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();

    /// <summary>
    /// Builds and returns a paginator for manual page loading.
    /// </summary>
    IPaginator<T> Build();
}

/// <summary>
/// Represents a paginated query that can load pages on demand.
/// </summary>
public interface IPaginator<T>
{
    /// <summary>
    /// Gets whether there are more pages to load.
    /// </summary>
    bool HasMore { get; }

    /// <summary>
    /// Gets the total number of pages loaded so far.
    /// </summary>
    int LoadedPageCount { get; }

    /// <summary>
    /// Gets all items loaded so far across all pages.
    /// </summary>
    IReadOnlyList<T> LoadedItems { get; }

    /// <summary>
    /// Gets the list of page boundaries (indices where each new page begins in LoadedItems).
    /// The first page always starts at index 0, so boundaries represent where subsequent pages start.
    /// Example: If pages are 25 items each, boundaries would be [0, 25, 50, 75] after loading 4 pages.
    /// </summary>
    IReadOnlyList<int> PageBoundaries { get; }

    /// <summary>
    /// Event raised when a new page boundary is added (when a new page is loaded).
    /// The event argument is the index where the new page starts in LoadedItems.
    /// </summary>
    event Action<int>? PageBoundaryAdded;

    /// <summary>
    /// Gets the page index (0-based) that contains the item at the specified index in LoadedItems.
    /// </summary>
    /// <param name="itemIndex">The index of the item in LoadedItems.</param>
    /// <returns>The page index (0-based) that contains this item, or -1 if index is out of range.</returns>
    int GetPageIndex(int itemIndex);

    /// <summary>
    /// Merges paginated items with subscription updates, handling deduplication and preserving page boundaries.
    /// </summary>
    /// <param name="subscriptionItems">Items received from a real-time subscription.</param>
    /// <param name="getId">Function to extract a unique identifier from an item for deduplication.</param>
    /// <param name="getSortKey">Optional function to extract a sort key for ordering. If null, items are kept in their original order.</param>
    /// <returns>A merged result containing the combined items and adjusted page boundaries.</returns>
    MergedPaginationResult<T> MergeWithSubscription(
        IEnumerable<T> subscriptionItems,
        Func<T, string> getId,
        Func<T, IComparable>? getSortKey = null);

    /// <summary>
    /// Loads the next page of results.
    /// </summary>
    Task<IReadOnlyList<T>> LoadNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the paginator to its initial state, clearing all loaded pages.
    /// </summary>
    void Reset();

    /// <summary>
    /// Returns an async enumerable that yields all items, automatically loading pages as needed.
    /// </summary>
    IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken cancellationToken = default);
}

/// <summary>
/// The options passed to a paginated query.
/// </summary>
public class PaginationOptions
{
    [JsonPropertyName("numItems")]
    public int NumItems { get; set; }

    [JsonPropertyName("cursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cursor { get; set; }

    [JsonPropertyName("endCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndCursor { get; set; }

    [JsonPropertyName("maximumRowsRead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaximumRowsRead { get; set; }

    [JsonPropertyName("maximumBytesRead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaximumBytesRead { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Id { get; set; }

    public PaginationOptions() => NumItems = 10;

    public PaginationOptions(int numItems) => NumItems = numItems;

    public PaginationOptions(int numItems, string? cursor)
    {
        NumItems = numItems;
        Cursor = cursor;
    }
}

/// <summary>
/// JSON converter for PaginationResult that handles both "page" and "messages" fields.
/// </summary>
public class PaginationResultConverter<T> : JsonConverter<PaginationResult<T>>
{
    public override PaginationResult<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new PaginationResult<T>();
        List<T>? messages = null;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                _ = reader.Read();

                switch (propertyName)
                {
                    case "page":
                        result.Page = JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? [];
                        break;
                    case "messages":
                        messages = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                        break;
                    case "isDone":
                        result.IsDone = reader.GetBoolean();
                        break;
                    case "continueCursor":
                        result.ContinueCursor = reader.GetString();
                        break;
                    case "splitCursor":
                        result.SplitCursor = reader.GetString();
                        break;
                    case "pageStatus":
                        var statusStr = reader.GetString();
                        if (Enum.TryParse<PageStatus>(statusStr, ignoreCase: true, out var status))
                        {
                            result.PageStatus = status;
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        // Use page if it has items, otherwise use messages
        if ((result.Page == null || result.Page.Count == 0) && messages != null && messages.Count > 0)
        {
            result.Page = messages;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, PaginationResult<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("page");
        JsonSerializer.Serialize(writer, value.Page, options);
        writer.WriteBoolean("isDone", value.IsDone);
        if (value.ContinueCursor != null)
        {
            writer.WriteString("continueCursor", value.ContinueCursor);
        }
        if (value.SplitCursor != null)
        {
            writer.WriteString("splitCursor", value.SplitCursor);
        }
        if (value.PageStatus.HasValue)
        {
            writer.WriteString("pageStatus", value.PageStatus.Value.ToString());
        }
        writer.WriteEndObject();
    }
}

/// <summary>
/// The result of paginating a database query.
/// </summary>
[JsonConverter(typeof(PaginationResultConverter<>))]
public class PaginationResult<T>
{
    [JsonPropertyName("page")]
    public List<T> Page { get; set; } = [];

    [JsonPropertyName("isDone")]
    public bool IsDone { get; set; }

    [JsonPropertyName("continueCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContinueCursor { get; set; }

    [JsonPropertyName("splitCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SplitCursor { get; set; }

    [JsonPropertyName("pageStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PageStatus? PageStatus { get; set; }
}

/// <summary>
/// The status of a paginated query page.
/// </summary>
public enum PageStatus
{
    Normal,
    SplitRecommended,
    SplitRequired
}

/// <summary>
/// Result of merging paginated items with subscription updates.
/// </summary>
public class MergedPaginationResult<T>
{
    /// <summary>
    /// The merged list of items, with deduplication applied.
    /// </summary>
    public IReadOnlyList<T> MergedItems { get; set; } = [];

    /// <summary>
    /// Adjusted page boundaries for the merged items.
    /// Boundaries represent where each page starts in the merged list.
    /// </summary>
    public IReadOnlyList<int> AdjustedBoundaries { get; set; } = [];
}

/// <summary>
/// Exception thrown when pagination operations fail.
/// </summary>
public class ConvexPaginationException(string message, string? functionName = null, Exception? innerException = null) : Exception(message, innerException)
{
    public string? FunctionName { get; } = functionName;
}
