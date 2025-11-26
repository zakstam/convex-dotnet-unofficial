using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.RealTime.Pagination;

/// <summary>
/// Implementation of IPaginator for manual page loading using direct HTTP calls.
/// </summary>
internal sealed class Paginator<T>(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    string functionName,
    int pageSize,
    object? args,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IPaginator<T>
{
    // Cache for property metadata to avoid repeated reflection lookups
    private static readonly ConcurrentDictionary<Type, PropertyMetadata[]> PropertyCache = new();

    private sealed class PropertyMetadata(PropertyInfo property, string jsonName)
    {
        public PropertyInfo Property { get; } = property;
        public string JsonName { get; } = jsonName;
    }
    private readonly IHttpClientProvider _httpProvider = httpProvider;
    private readonly IConvexSerializer _serializer = serializer;
    private readonly string _functionName = functionName;
    private readonly int _pageSize = pageSize;
    private readonly object? _args = args;
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private readonly object _paginationLock = new();
    private readonly List<T> _loadedItems = [];
    private readonly List<int> _pageBoundaries = [];
    private string? _continueCursor;
    private bool _hasMore = true;
    private int _loadedPageCount;

    public bool HasMore
    {
        get
        {
            lock (_paginationLock)
            {
                return _hasMore;
            }
        }
    }

    public int LoadedPageCount
    {
        get
        {
            lock (_paginationLock)
            {
                return _loadedPageCount;
            }
        }
    }

    public IReadOnlyList<T> LoadedItems
    {
        get
        {
            lock (_paginationLock)
            {
                return [.. _loadedItems];
            }
        }
    }

    public IReadOnlyList<int> PageBoundaries
    {
        get
        {
            lock (_paginationLock)
            {
                return [.. _pageBoundaries];
            }
        }
    }

    public event Action<int>? PageBoundaryAdded;

    public async Task<IReadOnlyList<T>> LoadNextAsync(CancellationToken cancellationToken = default)
    {
        // Check if there are more pages (thread-safe)
        string? cursor;
        lock (_paginationLock)
        {
            if (!_hasMore)
            {
                return [];
            }
            cursor = _continueCursor;
        }

        // Build pagination options
        var paginationOpts = new PaginationOptions(_pageSize, cursor);

        // Merge with user args if provided
        object queryArgs;
        if (_args != null)
        {
            // Combine user args with paginationOpts using reflection
            // This avoids JsonElement serialization issues
            var mergedArgs = new Dictionary<string, object?>();

            // Get cached property metadata (avoids repeated reflection lookups)
            var argsType = _args.GetType();
            var properties = PropertyCache.GetOrAdd(argsType, static type =>
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                return [.. props.Select(static prop =>
                {
                    var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                        ?? JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                    return new PropertyMetadata(prop, jsonName);
                })];
            });

            // Copy property values using cached metadata
            foreach (var propMeta in properties)
            {
                var value = propMeta.Property.GetValue(_args);
                if (value != null)
                {
                    mergedArgs[propMeta.JsonName] = value;
                }
            }

            // Add paginationOpts
            mergedArgs["paginationOpts"] = paginationOpts;

            queryArgs = mergedArgs;
        }
        else
        {
            queryArgs = new { paginationOpts };
        }

        // Execute query via direct HTTP call
        var result = await ExecuteQueryAsync(queryArgs, cancellationToken);

        // Update pagination state (thread-safe)
        var newBoundaryIndex = -1;
        var shouldRaiseEvent = false;
        lock (_paginationLock)
        {
            // Add boundary at the start of the new page (current count before adding)
            newBoundaryIndex = _loadedItems.Count;

            // Only add boundary if it doesn't already exist (avoids duplicate at index 0)
            if (!_pageBoundaries.Contains(newBoundaryIndex))
            {
                _pageBoundaries.Add(newBoundaryIndex);
                _pageBoundaries.Sort(); // Keep sorted
                shouldRaiseEvent = true;
            }

            _loadedItems.AddRange(result.Page);
            _continueCursor = result.ContinueCursor;
            _hasMore = !result.IsDone && !string.IsNullOrEmpty(result.ContinueCursor);
            _loadedPageCount++;
        }

        // Raise event outside of lock to avoid potential deadlocks
        if (shouldRaiseEvent && newBoundaryIndex >= 0)
        {
            PageBoundaryAdded?.Invoke(newBoundaryIndex);
        }

        return result.Page;
    }

    public int GetPageIndex(int itemIndex)
    {
        lock (_paginationLock)
        {
            if (itemIndex < 0 || itemIndex >= _loadedItems.Count)
            {
                return -1;
            }

            // Find which page this item belongs to by finding the last boundary <= itemIndex
            for (var i = _pageBoundaries.Count - 1; i >= 0; i--)
            {
                if (_pageBoundaries[i] <= itemIndex)
                {
                    return i;
                }
            }

            // If no boundary found, it's on the first page (index 0)
            return 0;
        }
    }

    public MergedPaginationResult<T> MergeWithSubscription(
        IEnumerable<T> subscriptionItems,
        Func<T, string> getId,
        Func<T, IComparable>? getSortKey = null)
    {
        if (subscriptionItems == null)
        {
            throw new ArgumentNullException(nameof(subscriptionItems));
        }
        if (getId == null)
        {
            throw new ArgumentNullException(nameof(getId));
        }

        // Get snapshots of current state (thread-safe)
        List<T> paginatedItems;
        List<int> originalBoundaries;
        lock (_paginationLock)
        {
            paginatedItems = [.. _loadedItems];
            originalBoundaries = [.. _pageBoundaries];
        }

        // Create dictionary for deduplication
        var itemDict = new Dictionary<string, T>();

        // Create set of subscription item IDs for efficient lookup
        var subscriptionIds = new HashSet<string>(
            subscriptionItems.Select(item => getId(item)).Where(id => !string.IsNullOrEmpty(id))!);

        // Add all paginated items first (preserve ALL loaded history)
        // BUT: if subscription is active, only keep paginated items that are either:
        // 1. In the subscription results (will be updated below), OR
        // 2. Not in the subscription's range (older messages that subscription doesn't cover)
        // For now, we'll add all paginated items, but subscription items will override/update them
        foreach (var item in paginatedItems)
        {
            var id = getId(item);
            if (!string.IsNullOrEmpty(id))
            {
                itemDict[id] = item;
            }
        }

        // Add/update with subscription items (they will update existing ones by ID or add new ones)
        // This also ensures items deleted from DB are removed if they were in subscription range
        foreach (var item in subscriptionItems)
        {
            var id = getId(item);
            if (!string.IsNullOrEmpty(id))
            {
                itemDict[id] = item; // Add or update by ID
            }
        }

        // Remove items that were in pagination but are no longer in subscription results
        // This handles the case where items are deleted from the database
        // The subscription returns the most recent N messages, so any paginated items
        // that aren't in subscription results should be removed (they were deleted)
        if (subscriptionIds.Count > 0 && getSortKey != null)
        {
            // Find the oldest and newest subscription item's sort keys to determine the subscription range
            var subscriptionItemsList = subscriptionItems.ToList();
            var subscriptionKeys = subscriptionItemsList.Select(item => getSortKey(item)).Where(k => k != null).ToList();

            if (subscriptionKeys.Count > 0)
            {
                var oldestSubscriptionKey = subscriptionKeys.Min();
                var newestSubscriptionKey = subscriptionKeys.Max();

                // Remove paginated items that:
                // 1. Are not in subscription results (deleted)
                // 2. Have a sort key within the subscription range (between oldest and newest, inclusive)
                //    OR have a sort key >= oldest (newer messages that should be in subscription)
                var itemsToRemove = new List<string>();
                foreach (var item in paginatedItems)
                {
                    var id = getId(item);
                    if (!string.IsNullOrEmpty(id) && !subscriptionIds.Contains(id))
                    {
                        var itemKey = getSortKey(item);
                        if (itemKey != null && oldestSubscriptionKey != null)
                        {
                            // If this item's sort key is >= oldest subscription key, it should be in subscription
                            // Since it's not, it was deleted
                            // Note: For descending order (newest first), larger timestamps = newer messages
                            // So items with timestamp >= oldest subscription timestamp should be in subscription
                            if (itemKey.CompareTo(oldestSubscriptionKey) >= 0)
                            {
                                itemsToRemove.Add(id);
                            }
                        }
                    }
                }

                // Remove deleted items from the dictionary
                foreach (var id in itemsToRemove)
                {
                    _ = itemDict.Remove(id);
                }
            }
        }

        // Convert to list and sort if sort key provided
        List<T> mergedItems;
        if (getSortKey != null)
        {
            mergedItems = [.. itemDict.Values.OrderBy(getSortKey)];
        }
        else
        {
            // Keep original order: paginated items first, then subscription items
            var paginatedIds = new HashSet<string>(paginatedItems.Select(getId).Where(id => !string.IsNullOrEmpty(id)));
            // subscriptionIds is already declared above

            // Items that were in pagination (preserve their order)
            var orderedPaginated = paginatedItems
                .Where(item => paginatedIds.Contains(getId(item)))
                .Select(item => (item, id: getId(item)))
                .Where(x => !string.IsNullOrEmpty(x.id))
                .ToDictionary(x => x.id!, x => x.item);

            // Items from subscription that weren't in pagination (new items)
            var newItems = subscriptionItems
                .Where(item => !paginatedIds.Contains(getId(item)))
                .ToList();

            // Build merged list: paginated items (with updates from subscription), then new items
            mergedItems = [];
            foreach (var item in paginatedItems)
            {
                var id = getId(item);
                if (!string.IsNullOrEmpty(id) && itemDict.TryGetValue(id, out var updatedItem))
                {
                    mergedItems.Add(updatedItem); // Use updated version if available
                }
            }
            mergedItems.AddRange(newItems);
        }

        // Adjust boundaries: boundaries should still point to where pages start in merged list
        // Since we're preserving paginated items and their order, boundaries remain valid
        // However, if subscription items are inserted in the middle, boundaries need adjustment
        // For simplicity, we'll keep original boundaries if items are only appended
        // If items are inserted, we'll need to recalculate boundaries
        var adjustedBoundaries = new List<int>();

        if (getSortKey != null)
        {
            // If sorted, boundaries are no longer meaningful - clear them
            // Or we could try to preserve them, but it's complex
            adjustedBoundaries.Add(0);
        }
        else
        {
            // If not sorted, paginated items are preserved in order, so boundaries remain valid
            // But we need to account for any items that were updated (same position)
            // Since we're replacing items by ID, positions should remain the same
            adjustedBoundaries.AddRange(originalBoundaries);
        }

        return new MergedPaginationResult<T>
        {
            MergedItems = mergedItems,
            AdjustedBoundaries = adjustedBoundaries
        };
    }

    public void Reset()
    {
        lock (_paginationLock)
        {
            _loadedItems.Clear();
            _pageBoundaries.Clear();
            _continueCursor = null;
            _hasMore = true;
            _loadedPageCount = 0;
        }
    }

    public async IAsyncEnumerable<T> AsAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // First yield all already loaded items (thread-safe snapshot)
        List<T> loadedSnapshot;
        lock (_paginationLock)
        {
            loadedSnapshot = [.. _loadedItems];
        }

        foreach (var item in loadedSnapshot)
        {
            yield return item;
        }

        // Continue loading and yielding while there are more pages
        bool hasMore;
        lock (_paginationLock)
        {
            hasMore = _hasMore;
        }

        while (hasMore)
        {
            var page = await LoadNextAsync(cancellationToken);
            foreach (var item in page)
            {
                yield return item;
            }

            lock (_paginationLock)
            {
                hasMore = _hasMore;
            }
        }
    }

    private async Task<PaginationResult<T>> ExecuteQueryAsync(object args, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_httpProvider.DeploymentUrl}/api/query";

            var requestBody = new
            {
                path = _functionName,
                format = "convex_encoded_json",
                args = new[] { args }
            };

            var json = _serializer.Serialize(requestBody) ?? throw new ConvexPaginationException(
                    $"Failed to serialize pagination request body for function '{_functionName}'. Serializer returned null.",
                    _functionName);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            var response = await _httpProvider.SendAsync(request, cancellationToken);

            // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
            // This matches convex-js behavior where HTTP 560 is treated as a valid response
            ConvexHttpConstants.EnsureConvexResponse(response);

            var responseJson = await response.ReadContentAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var status))
            {
                var statusValue = status.GetString();
                if (statusValue == "success" && root.TryGetProperty("value", out var value))
                {
                    var rawJson = value.GetRawText();

                    // Use custom converter to handle both "page" and "messages" fields
                    // Create serializer options that match DefaultConvexSerializer's options plus our converter
                    var optionsWithConverter = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new ConvexInt64JsonConverter(),
                            new PaginationResultConverter<T>()
                        }
                    };

                    // Deserialize using the converter
                    var result = JsonSerializer.Deserialize<PaginationResult<T>>(rawJson, optionsWithConverter) ?? throw new ConvexPaginationException(
                            $"Failed to deserialize pagination result for function '{_functionName}'. Raw JSON: {rawJson}",
                            _functionName);

                    // Log the raw JSON and deserialized result for debugging (especially on first page)
                    if (_loadedPageCount == 0 && _logger != null && ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger.LogDebug("[Paginator] Raw JSON response: {RawJson}", rawJson);
                        _logger.LogDebug("[Paginator] Deserialized result - Page.Count: {PageCount}, IsDone: {IsDone}, ContinueCursor: {ContinueCursor}",
                            result.Page.Count, result.IsDone, result.ContinueCursor ?? "null");
                        if (result.Page.Count > 0)
                        {
                            _logger.LogDebug("[Paginator] First item type: {ItemType}", result.Page[0]?.GetType().Name ?? "null");
                        }
                    }

                    return result;
                }
                else if (statusValue == "error")
                {
                    var errorMessage = root.TryGetProperty("errorMessage", out var errMsg)
                        ? errMsg.GetString()
                        : "Unknown query error";
                    throw new ConvexPaginationException(
                        $"Paginated query '{_functionName}' failed: {errorMessage}",
                        _functionName);
                }
            }

            throw new ConvexPaginationException(
                $"Invalid response format from paginated query '{_functionName}'",
                _functionName);
        }
        catch (Exception ex) when (ex is not ConvexPaginationException)
        {
            throw new ConvexPaginationException(
                $"Pagination query failed for function '{_functionName}': {ex.Message}",
                _functionName,
                ex);
        }
    }
}
