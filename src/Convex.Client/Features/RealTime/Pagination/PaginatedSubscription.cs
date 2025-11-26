using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Convex.Client.Features.RealTime.Pagination;

/// <summary>
/// Combines pagination with real-time subscriptions, automatically merging paginated history with subscription updates.
/// Handles deduplication, ordering, and page boundary tracking internally.
/// </summary>
/// <typeparam name="T">The type of items being paginated and subscribed to.</typeparam>
public class PaginatedSubscription<T> : IDisposable
{
    private readonly IConvexClient _client;
    private readonly string _functionName;
    private readonly int _pageSize;
    private readonly object? _subscriptionArgs;
    private readonly Func<T, string> _getId;
    private readonly Func<T, IComparable>? _getSortKey;
    private readonly Subject<PaginatedSubscriptionUpdate<T>> _updates = new();
    private readonly object _lock = new();

    private IPaginator<T>? _paginator;
    private IDisposable? _subscription;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Observable stream of merged pagination and subscription updates.
    /// </summary>
    public IObservable<PaginatedSubscriptionUpdate<T>> Updates => _updates.AsObservable();

    /// <summary>
    /// Gets whether there are more pages to load.
    /// </summary>
    public bool HasMore
    {
        get
        {
            lock (_lock)
            {
                return _paginator?.HasMore ?? false;
            }
        }
    }

    /// <summary>
    /// Gets the current merged items.
    /// </summary>
    public IReadOnlyList<T> CurrentItems
    {
        get
        {
            lock (_lock)
            {
                if (_paginator == null)
                {
                    return [];
                }
                return _paginator.LoadedItems;
            }
        }
    }

    /// <summary>
    /// Gets the current page boundaries.
    /// </summary>
    public IReadOnlyList<int> PageBoundaries
    {
        get
        {
            lock (_lock)
            {
                if (_paginator == null)
                {
                    return [];
                }
                return _paginator.PageBoundaries;
            }
        }
    }

    /// <summary>
    /// Event raised when a new page boundary is added.
    /// </summary>
    public event Action<int>? PageBoundaryAdded;

    internal PaginatedSubscription(
        IConvexClient client,
        string functionName,
        int pageSize,
        object? subscriptionArgs,
        Func<T, string> getId,
        Func<T, IComparable>? getSortKey)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        _pageSize = pageSize;
        _subscriptionArgs = subscriptionArgs;
        _getId = getId ?? throw new ArgumentNullException(nameof(getId));
        _getSortKey = getSortKey;
    }

    /// <summary>
    /// Creates a new paginated subscription builder.
    /// </summary>
    public static PaginatedSubscriptionBuilder<T> Create(IConvexClient client, string functionName) => new PaginatedSubscriptionBuilder<T>(client, functionName);

    /// <summary>
    /// Initializes the paginated subscription by loading the first page and starting the subscription.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_isInitialized)
            {
                return;
            }

            // Create paginator
            _paginator = _client.PaginationSlice
                .Query<T>(_functionName)
                .WithPageSize(_pageSize)
                .WithArgs(_subscriptionArgs ?? new { })
                .Build();

            // Subscribe to page boundary events
            _paginator.PageBoundaryAdded += OnPageBoundaryAdded;

            // Start subscription
#pragma warning disable CVX001, CVX002 // Direct client access needed for core pagination functionality
            var observable = _subscriptionArgs != null
                ? _client.Observe<T, object>(_functionName, _subscriptionArgs)
                : _client.Observe<T>(_functionName);
#pragma warning restore CVX001, CVX002

            _subscription = observable.Subscribe(
                onNext: OnSubscriptionUpdate,
                onError: OnSubscriptionError);

            _isInitialized = true;
        }

        // Load first page
        _ = await LoadNextAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the next page of results.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadNextAsync(CancellationToken cancellationToken = default)
    {
        IPaginator<T>? paginator;
        lock (_lock)
        {
            paginator = _paginator;
        }

        if (paginator == null)
        {
            throw new InvalidOperationException("PaginatedSubscription has not been initialized. Call InitializeAsync() first.");
        }

        var newItems = await paginator.LoadNextAsync(cancellationToken);

        // Merge with current subscription state and emit update
        EmitMergedUpdate();

        return newItems;
    }

    private void OnSubscriptionUpdate(T subscriptionData)
    {
        // Handle collections: if T is a collection type (like List<TItem>), extract items
        // Otherwise, treat as single item
        IEnumerable<T> items;

        // Check if T implements IEnumerable<T> (for collection types)
        // Note: This works when T is a collection type like List<MessageDto>
        if (subscriptionData is System.Collections.IEnumerable enumerable && subscriptionData is not string)
        {
            // Try to cast to IEnumerable<T>
            if (subscriptionData is IEnumerable<T> typedCollection)
            {
                items = typedCollection;
            }
            else
            {
                // Fallback: treat as single item
                items = new[] { subscriptionData };
            }
        }
        else
        {
            // Single item
            items = new[] { subscriptionData };
        }

        EmitMergedUpdate(items);
    }

    private void OnSubscriptionError(Exception error)
    {
        _updates.OnNext(new PaginatedSubscriptionUpdate<T>
        {
            Items = CurrentItems,
            PageBoundaries = PageBoundaries,
            Error = error
        });
    }

    private void OnPageBoundaryAdded(int boundaryIndex) => PageBoundaryAdded?.Invoke(boundaryIndex);

    private void EmitMergedUpdate(IEnumerable<T>? subscriptionItems = null)
    {
        IPaginator<T>? paginator;
        lock (_lock)
        {
            paginator = _paginator;
        }

        if (paginator == null)
        {
            return;
        }

        // If no subscription items provided, use empty list (just pagination update)
        var itemsToMerge = subscriptionItems ?? [];

        // Merge pagination with subscription
        var merged = paginator.MergeWithSubscription(itemsToMerge, _getId, _getSortKey);

        // Emit update
        _updates.OnNext(new PaginatedSubscriptionUpdate<T>
        {
            Items = merged.MergedItems,
            PageBoundaries = merged.AdjustedBoundaries
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _subscription?.Dispose();
            _subscription = null;

            if (_paginator != null)
            {
                _paginator.PageBoundaryAdded -= OnPageBoundaryAdded;
            }

            _updates.OnCompleted();
            _updates.Dispose();

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Update event from a paginated subscription, containing merged items and boundaries.
/// </summary>
public class PaginatedSubscriptionUpdate<T>
{
    /// <summary>
    /// The merged items from pagination and subscription.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// The current page boundaries.
    /// </summary>
    public IReadOnlyList<int> PageBoundaries { get; set; } = [];

    /// <summary>
    /// Any error that occurred during the update.
    /// </summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// Builder for creating paginated subscriptions.
/// </summary>
public class PaginatedSubscriptionBuilder<T>
{
    private readonly IConvexClient _client;
    private readonly string _functionName;
    private int _pageSize = 25;
    private object? _subscriptionArgs;
    private Func<T, string>? _getId;
    private Func<T, IComparable>? _getSortKey;

    internal PaginatedSubscriptionBuilder(IConvexClient client, string functionName)
    {
        _client = client;
        _functionName = functionName;
    }

    /// <summary>
    /// Sets the page size (number of items per page).
    /// </summary>
    public PaginatedSubscriptionBuilder<T> WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the arguments to pass to the subscription function.
    /// </summary>
    public PaginatedSubscriptionBuilder<T> WithSubscriptionArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _subscriptionArgs = args;
        return this;
    }

    /// <summary>
    /// Sets the function to extract a unique identifier from items for deduplication.
    /// </summary>
    public PaginatedSubscriptionBuilder<T> WithIdExtractor(Func<T, string> getId)
    {
        _getId = getId;
        return this;
    }

    /// <summary>
    /// Sets the function to extract a sort key for ordering merged items.
    /// </summary>
    public PaginatedSubscriptionBuilder<T> WithSortKey(Func<T, IComparable> getSortKey)
    {
        _getSortKey = getSortKey;
        return this;
    }

    /// <summary>
    /// Builds and returns the paginated subscription.
    /// </summary>
    public PaginatedSubscription<T> Build()
    {
        if (_getId == null)
        {
            throw new InvalidOperationException("IdExtractor must be set. Call WithIdExtractor() before building.");
        }

        return new PaginatedSubscription<T>(
            _client,
            _functionName,
            _pageSize,
            _subscriptionArgs,
            _getId,
            _getSortKey);
    }
}

