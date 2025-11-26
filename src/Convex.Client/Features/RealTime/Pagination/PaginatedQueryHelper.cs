using System.Reflection;
using System.Reactive.Linq;
using Convex.Client.Infrastructure.Internal.Threading;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.RealTime.Pagination;

/// <summary>
/// Higher-level helper class that wraps common pagination patterns for easier use.
/// Handles initial load, pagination, and subscription in one place.
/// </summary>
/// <typeparam name="T">The type of items being paginated.</typeparam>
public class PaginatedQueryHelper<T> : IDisposable
{
    private readonly IConvexClient _client;
    private readonly string _functionName;
    private readonly int _pageSize;
    private readonly object? _args;
    private readonly Func<T, string> _getId;
    private readonly Func<T, IComparable>? _getSortKey;
    private readonly Func<object, IEnumerable<T>>? _extractSubscriptionItems;
    private readonly Func<System.IObservable<object>>? _subscriptionFactory;
    private readonly Type? _subscriptionType; // For reflection fallback
    private readonly ILogger? _logger;
    private readonly bool _enableDebugLogging;
    private readonly object _lock = new();

    private IPaginator<T>? _paginator;
    private IDisposable? _subscription;
    private List<T> _currentItems = [];
    private List<int> _currentBoundaries = [];
    private bool _isInitialized;
    private bool _disposed;

    // Static cached reflection results to avoid repeated MethodInfo lookups
    private static MethodInfo? _observeWithArgsMethod; // Observe<T, TArgs>(string, TArgs)
    private static MethodInfo? _observeWithoutArgsMethod; // Observe<T>(string)
    private static readonly object _reflectionLock = new();

    /// <summary>
    /// Event raised when items are updated (from pagination or subscription).
    /// </summary>
    public event Action<IReadOnlyList<T>, IReadOnlyList<int>>? ItemsUpdated;

    /// <summary>
    /// Event raised when a new page boundary is added.
    /// </summary>
    public event Action<int>? PageBoundaryAdded;

    /// <summary>
    /// Event raised when subscription status changes (for debugging).
    /// </summary>
    public event Action<string>? SubscriptionStatusChanged;

    /// <summary>
    /// Event raised when an error occurs during pagination or subscription.
    /// </summary>
    public event Action<string>? ErrorOccurred;

    /// <summary>
    /// Gets the current items.
    /// </summary>
    public IReadOnlyList<T> CurrentItems
    {
        get
        {
            lock (_lock)
            {
                return [.. _currentItems];
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
                return [.. _currentBoundaries];
            }
        }
    }

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

    internal PaginatedQueryHelper(
        IConvexClient client,
        string functionName,
        int pageSize,
        object? args,
        Func<T, string> getId,
        Func<T, IComparable>? getSortKey,
        Func<object, IEnumerable<T>>? extractSubscriptionItems = null,
        Func<System.IObservable<object>>? subscriptionFactory = null,
        Type? subscriptionType = null,
        ILogger? logger = null,
        bool enableDebugLogging = false,
        SyncContextCapture? syncContext = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        _pageSize = pageSize;
        _args = args;
        _getId = getId ?? throw new ArgumentNullException(nameof(getId));
        _getSortKey = getSortKey;
        _extractSubscriptionItems = extractSubscriptionItems;
        _subscriptionFactory = subscriptionFactory;
        _subscriptionType = subscriptionType;
        _logger = logger;
        _enableDebugLogging = enableDebugLogging;
    }

    /// <summary>
    /// Creates a new paginated query helper builder.
    /// </summary>
    public static PaginatedQueryHelperBuilder<T> Create(IConvexClient client, string functionName) => new PaginatedQueryHelperBuilder<T>(client, functionName);

    /// <summary>
    /// Initializes the helper by loading the first page and optionally starting a subscription.
    /// </summary>
    /// <param name="enableSubscription">Whether to enable real-time subscription updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(bool enableSubscription = true, CancellationToken cancellationToken = default)
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
            var builder = _client.PaginationSlice.Query<T>(_functionName).WithPageSize(_pageSize);
            if (_args != null)
            {
                builder = builder.WithArgs(_args);
            }
            _paginator = builder.Build();

            // Subscribe to page boundary events
            _paginator.PageBoundaryAdded += OnPageBoundaryAdded;

            // Start subscription if enabled
            if (enableSubscription)
            {
                SubscriptionStatusChanged?.Invoke($"Subscription enabled, extractor: {_extractSubscriptionItems != null}, factory: {_subscriptionFactory != null}");
                if (_subscriptionFactory != null && _extractSubscriptionItems != null)
                {
                    // Use factory to create subscription (avoids reflection)
                    SubscriptionStatusChanged?.Invoke("Starting subscription with extractor using factory...");
                    try
                    {
                        var observable = _subscriptionFactory();
                        SubscriptionStatusChanged?.Invoke($"Observable created: {observable != null}");
                        if (observable == null)
                        {
                            SubscriptionStatusChanged?.Invoke("Factory returned null observable");
                            return;
                        }
                        _subscription = observable.Subscribe(
                            onNext: data =>
                            {
                                try
                                {
                                    if (data != null)
                                    {
                                        SubscriptionStatusChanged?.Invoke($"Subscription received data: {data.GetType().Name}");
                                        var items = _extractSubscriptionItems(data);
                                        var itemList = items.ToList();
                                        SubscriptionStatusChanged?.Invoke($"Extracted {itemList.Count} items from subscription");
                                        MergeSubscriptionItemsInternal(itemList);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SubscriptionStatusChanged?.Invoke($"Error processing subscription data: {ex.GetType().Name}: {ex.Message}");
                                    OnSubscriptionError(ex);
                                }
                            },
                            onError: ex =>
                            {
                                SubscriptionStatusChanged?.Invoke($"Subscription error: {ex.GetType().Name}: {ex.Message}");
                                OnSubscriptionError(ex);
                            });
                        SubscriptionStatusChanged?.Invoke("Subscription started successfully");
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Failed to create subscription: {ex.GetType().Name}: {ex.Message}";
                        SubscriptionStatusChanged?.Invoke(errorMessage);
                        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                        {
                            _logger!.LogDebug(ex, "Failed to create subscription: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
                        }
                    }
                }
                else if (_extractSubscriptionItems != null)
                {
                    // Fallback to reflection if no factory provided
                    SubscriptionStatusChanged?.Invoke("Starting subscription with extractor (using reflection fallback)...");
                    StartSubscriptionWithExtractor();
                }
                else
                {
                    SubscriptionStatusChanged?.Invoke("Starting subscription without extractor...");
#pragma warning disable CVX001, CVX002 // Direct client access needed for core pagination functionality
                    var observable = _args != null
                        ? _client.Observe<T, object>(_functionName, _args)
                        : _client.Observe<T>(_functionName);
#pragma warning restore CVX001, CVX002

                    SubscriptionStatusChanged?.Invoke("Subscribing to observable...");
                    _subscription = observable.Subscribe(
                        onNext: data =>
                        {
                            SubscriptionStatusChanged?.Invoke($"Subscription received data: {data?.GetType().Name ?? "null"}");
                            OnSubscriptionUpdate(data);
                        },
                        onError: ex =>
                        {
                            SubscriptionStatusChanged?.Invoke($"Subscription error: {ex.GetType().Name}: {ex.Message}");
                            OnSubscriptionError(ex);
                        });
                    SubscriptionStatusChanged?.Invoke("Subscription started successfully (no extractor)");
                }
            }
            else
            {
                SubscriptionStatusChanged?.Invoke("Subscription disabled");
            }

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
            throw new InvalidOperationException("PaginatedQueryHelper has not been initialized. Call InitializeAsync() first.");
        }

        var newItems = await paginator.LoadNextAsync(cancellationToken);

        // Update current items from paginator
        UpdateCurrentItems();

        return newItems;
    }

    /// <summary>
    /// Merges subscription items with the current paginated items.
    /// Useful when subscription returns a different type than pagination.
    /// </summary>
    public void MergeSubscriptionItems(IEnumerable<T> subscriptionItems)
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

        // Use the private method to do the actual merge
        MergeSubscriptionItemsInternal(subscriptionItems);
    }

    /// <summary>
    /// Resets the paginator and clears all loaded items.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _paginator?.Reset();
            _currentItems.Clear();
            _currentBoundaries.Clear();
            _currentBoundaries.Add(0); // Reset to initial boundary
        }

        var emptyArray = Array.Empty<T>();
        var initialBoundary = new[] { 0 };
        ItemsUpdated?.Invoke(emptyArray, initialBoundary);
    }

    private void OnSubscriptionUpdate(T subscriptionData)
    {
        IEnumerable<T> items;

        // Note: If we have an extractor, StartSubscriptionWithExtractor handles it directly
        // This method is only called when subscription returns T directly
        // Handle collections: if T is a collection type (like List<TItem>), extract items
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

        MergeSubscriptionItemsInternal(items);
    }

    private void OnSubscriptionError(Exception error)
    {
        var errorMessage = $"Subscription error: {error.GetType().Name}: {error.Message}";
        ErrorOccurred?.Invoke(errorMessage);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger?.LogDebug(error, "Subscription error: {ExceptionType}: {Message}", error.GetType().Name, error.Message);
        }

        // Emit current state even on error
        lock (_lock)
        {
            ItemsUpdated?.Invoke([.. _currentItems], [.. _currentBoundaries]);
        }
    }

    private void StartSubscriptionWithExtractor()
    {
        // Subscribe using the known subscription type
        // This allows us to handle wrapper types like GetMessagesResponse
        if (_subscriptionType == null || _extractSubscriptionItems == null)
        {
            SubscriptionStatusChanged?.Invoke("Cannot start subscription: subscription type or extractor is null");
            return;
        }

        // Try to create the subscription observable
        SubscriptionStatusChanged?.Invoke($"Creating subscription observable for type: {_subscriptionType.Name}");
        var observable = CreateSubscriptionObservable(_subscriptionType);

        if (observable != null)
        {
            try
            {
                SubscriptionStatusChanged?.Invoke($"Starting subscription with extractor for type: {_subscriptionType.Name}");
                _subscription = observable.Subscribe(
                    onNext: data =>
                    {
                        try
                        {
                            if (data != null)
                            {
                                SubscriptionStatusChanged?.Invoke($"Subscription received data: {data.GetType().Name}");
                                var items = _extractSubscriptionItems(data);
                                var itemList = items.ToList();
                                SubscriptionStatusChanged?.Invoke($"Extracted {itemList.Count} items from subscription");
                                MergeSubscriptionItemsInternal(itemList);
                            }
                        }
                        catch (Exception ex)
                        {
                            SubscriptionStatusChanged?.Invoke($"Error processing subscription data: {ex.GetType().Name}: {ex.Message}");
                            OnSubscriptionError(ex);
                        }
                    },
                    onError: ex =>
                    {
                        SubscriptionStatusChanged?.Invoke($"Subscription error: {ex.GetType().Name}: {ex.Message}");
                        OnSubscriptionError(ex);
                    });
                SubscriptionStatusChanged?.Invoke("Subscription started successfully");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to subscribe: {ex.GetType().Name}: {ex.Message}";
                SubscriptionStatusChanged?.Invoke(errorMessage);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug(ex, "Failed to subscribe: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
                }
                // Continue without subscription - pagination will still work
            }
        }
        else
        {
            SubscriptionStatusChanged?.Invoke($"Failed to create subscription observable for type: {_subscriptionType.Name}");
        }
    }

    private System.IObservable<object>? CreateSubscriptionObservable(Type subscriptionType)
    {
        // Use reflection to call Observe<TResponse> or Observe<TResponse, TArgs>
        SubscriptionStatusChanged?.Invoke($"Creating subscription observable for type: {subscriptionType.Name}, hasArgs: {_args != null}");

        // Get cached method info (or cache it if not already cached)
        var method = GetOrCacheObserveMethod(_args != null);

        if (method == null)
        {
            SubscriptionStatusChanged?.Invoke("Observe method not found");
            return null;
        }

        try
        {
            SubscriptionStatusChanged?.Invoke($"Making generic method for type: {subscriptionType.Name}");
            MethodInfo genericMethod;
            if (_args != null)
            {
                // For Observe<T, TArgs>, we need to specify both type arguments
                // T = subscriptionType, TArgs = _args.GetType()
                var argsType = _args.GetType();
                genericMethod = method.MakeGenericMethod(subscriptionType, argsType);
                SubscriptionStatusChanged?.Invoke($"Invoking Observe<{subscriptionType.Name}, {argsType.Name}>...");
                var result = genericMethod.Invoke(_client, new object[] { _functionName, _args });
                var observable = result as System.IObservable<object>;
                SubscriptionStatusChanged?.Invoke($"Observable created: {observable != null}");
                return observable;
            }
            else
            {
                genericMethod = method.MakeGenericMethod(subscriptionType);
                SubscriptionStatusChanged?.Invoke($"Invoking Observe<{subscriptionType.Name}>...");
                var result = genericMethod.Invoke(_client, new object[] { _functionName });
                var observable = result as System.IObservable<object>;
                SubscriptionStatusChanged?.Invoke($"Observable created: {observable != null}");
                return observable;
            }
        }
        catch (Exception ex)
        {
            SubscriptionStatusChanged?.Invoke($"Failed to invoke Observe method: {ex.GetType().Name}: {ex.Message}");
            SubscriptionStatusChanged?.Invoke($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Gets the cached Observe method, or caches it if not already cached.
    /// This avoids repeated reflection lookups for the same method signatures.
    /// </summary>
    private static MethodInfo? GetOrCacheObserveMethod(bool needsArgs)
    {
        if (needsArgs)
        {
            if (_observeWithArgsMethod != null)
                return _observeWithArgsMethod;

            lock (_reflectionLock)
            {
                if (_observeWithArgsMethod != null)
                    return _observeWithArgsMethod;

                try
                {
                    // Find the generic method definition: Observe<T, TArgs>(string, TArgs)
                    // We need to find the method with 2 generic parameters
                    var methods = typeof(IConvexClient).GetMethods()
                        .Where(m => m.Name == "Observe" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2)
                        .Where(m =>
                        {
                            var parameters = m.GetParameters();
                            return parameters.Length == 2 &&
                                   parameters[0].ParameterType == typeof(string) &&
                                   parameters[1].ParameterType.IsGenericParameter;
                        })
                        .ToList();

                    _observeWithArgsMethod = methods.FirstOrDefault();
                }
                catch { /* Silently fail - will be caught in CreateSubscriptionObservable */ }

                return _observeWithArgsMethod;
            }
        }
        else
        {
            if (_observeWithoutArgsMethod != null)
                return _observeWithoutArgsMethod;

            lock (_reflectionLock)
            {
                if (_observeWithoutArgsMethod != null)
                    return _observeWithoutArgsMethod;

                try
                {
                    // Find the generic method definition: Observe<T>(string)
                    var methods = typeof(IConvexClient).GetMethods()
                        .Where(m => m.Name == "Observe" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1)
                        .Where(m =>
                        {
                            var parameters = m.GetParameters();
                            return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                        })
                        .ToList();

                    _observeWithoutArgsMethod = methods.FirstOrDefault();
                }
                catch { /* Silently fail - will be caught in CreateSubscriptionObservable */ }

                return _observeWithoutArgsMethod;
            }
        }
    }

    private void OnPageBoundaryAdded(int boundaryIndex)
    {
        lock (_lock)
        {
            if (!_currentBoundaries.Contains(boundaryIndex))
            {
                _currentBoundaries.Add(boundaryIndex);
                _currentBoundaries.Sort();
            }
        }

        PageBoundaryAdded?.Invoke(boundaryIndex);
    }

    private void MergeSubscriptionItemsInternal(IEnumerable<T> subscriptionItems)
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

        // Merge pagination with subscription
        var merged = paginator.MergeWithSubscription(subscriptionItems, _getId, _getSortKey);

        // Update current state
        lock (_lock)
        {
            _currentItems = [.. merged.MergedItems];
            _currentBoundaries = [.. merged.AdjustedBoundaries];
        }

        // Notify listeners
        ItemsUpdated?.Invoke(merged.MergedItems, merged.AdjustedBoundaries);
    }

    private void UpdateCurrentItems()
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

        // Get current items and boundaries from paginator
        lock (_lock)
        {
            _currentItems = [.. paginator.LoadedItems];
            _currentBoundaries = [.. paginator.PageBoundaries];
        }

        // Notify listeners
        ItemsUpdated?.Invoke(_currentItems, _currentBoundaries);
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

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Builder for creating paginated query helpers.
/// </summary>
public class PaginatedQueryHelperBuilder<T>
{
    private readonly IConvexClient _client;
    private readonly string _functionName;
    private int _pageSize = 25;
    private object? _args;
    private Func<T, string>? _getId;
    private Func<T, IComparable>? _getSortKey;
    private Func<object, IEnumerable<T>>? _extractSubscriptionItems;
    private Type? _subscriptionType;
    private Func<System.IObservable<object>>? _subscriptionFactory;
    private ILogger? _logger;
    private bool _enableDebugLogging;
    private SyncContextCapture? _syncContext;
    private Action<IReadOnlyList<T>, IReadOnlyList<int>>? _onItemsUpdated;
    private Action<string>? _onError;
    private Action<int>? _onPageBoundaryAdded;
    private Action<string>? _onSubscriptionStatusChanged;

    internal PaginatedQueryHelperBuilder(IConvexClient client, string functionName)
    {
        _client = client;
        _functionName = functionName;
    }

    /// <summary>
    /// Sets the page size (number of items per page).
    /// </summary>
    public PaginatedQueryHelperBuilder<T> WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the arguments to pass to the query function.
    /// </summary>
    public PaginatedQueryHelperBuilder<T> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    /// <summary>
    /// Sets the function to extract a unique identifier from items for deduplication.
    /// </summary>
    public PaginatedQueryHelperBuilder<T> WithIdExtractor(Func<T, string> getId)
    {
        _getId = getId;
        return this;
    }

    /// <summary>
    /// Sets the function to extract a sort key for ordering merged items.
    /// </summary>
    public PaginatedQueryHelperBuilder<T> WithSortKey(Func<T, IComparable> getSortKey)
    {
        _getSortKey = getSortKey;
        return this;
    }

    /// <summary>
    /// Sets a function to extract items from the subscription response.
    /// Use this when the subscription returns a wrapper type (e.g., GetMessagesResponse)
    /// instead of the items directly.
    /// </summary>
    /// <typeparam name="TResponse">The type returned by the subscription.</typeparam>
    /// <param name="extractItems">Function to extract items from the subscription response.</param>
    public PaginatedQueryHelperBuilder<T> WithSubscriptionExtractor<TResponse>(Func<TResponse, IEnumerable<T>> extractItems)
    {
        _extractSubscriptionItems = response => extractItems((TResponse)response);
        _subscriptionType = typeof(TResponse);

        // Create factory to avoid reflection - this is type-safe at compile time
        if (_args != null)
        {
            _subscriptionFactory = () =>
            {
#pragma warning disable CVX001, CVX002 // Direct client access needed for core pagination functionality
                var observable = _client.Observe<TResponse, object>(_functionName, _args);
#pragma warning restore CVX001, CVX002
                return (System.IObservable<object>)observable;
            };
        }
        else
        {
            _subscriptionFactory = () =>
            {
#pragma warning disable CVX001, CVX002 // Direct client access needed for core pagination functionality
                var observable = _client.Observe<TResponse>(_functionName);
#pragma warning restore CVX001, CVX002
                return (System.IObservable<object>)observable;
            };
        }

        return this;
    }

    /// <summary>
    /// Sets the logger for debug logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The builder for method chaining.</returns>
    public PaginatedQueryHelperBuilder<T> WithLogger(ILogger? logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>
    /// Enables or disables debug-level logging.
    /// </summary>
    /// <param name="enabled">Whether to enable debug logging (default: true).</param>
    /// <returns>The builder for method chaining.</returns>
    public PaginatedQueryHelperBuilder<T> EnableDebugLogging(bool enabled = true)
    {
        _enableDebugLogging = enabled;
        return this;
    }

    /// <summary>
    /// Enables automatic UI thread marshalling for event handlers.
    /// Captures the current SynchronizationContext and uses it to marshal all event callbacks to the UI thread.
    /// This is especially useful for WPF, WinForms, Blazor, and other UI frameworks.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var helper = await client.CreatePaginatedQuery&lt;MessageDto&gt;("messages:get")
    ///     .WithUIThreadMarshalling()
    ///     .OnItemsUpdated((items, boundaries) => UpdateUI(items)) // Already on UI thread
    ///     .InitializeAsync();
    /// </code>
    /// </example>
    public PaginatedQueryHelperBuilder<T> WithUIThreadMarshalling()
    {
        _syncContext = new SyncContextCapture();
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when items are updated (from pagination or subscription).
    /// If WithUIThreadMarshalling() was called, this callback will be invoked on the UI thread.
    /// </summary>
    /// <param name="onItemsUpdated">Callback that receives the updated items and page boundaries.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var helper = await client.CreatePaginatedQuery&lt;MessageDto&gt;("messages:get")
    ///     .WithUIThreadMarshalling()
    ///     .OnItemsUpdated((items, boundaries) => {
    ///         Messages = items.ToList();
    ///         StateHasChanged(); // Safe to call - already on UI thread
    ///     })
    ///     .InitializeAsync();
    /// </code>
    /// </example>
    public PaginatedQueryHelperBuilder<T> OnItemsUpdated(Action<IReadOnlyList<T>, IReadOnlyList<int>> onItemsUpdated)
    {
        _onItemsUpdated = onItemsUpdated ?? throw new ArgumentNullException(nameof(onItemsUpdated));
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when an error occurs.
    /// If WithUIThreadMarshalling() was called, this callback will be invoked on the UI thread.
    /// </summary>
    /// <param name="onError">Callback that receives the error message.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var helper = await client.CreatePaginatedQuery&lt;MessageDto&gt;("messages:get")
    ///     .WithUIThreadMarshalling()
    ///     .OnError(error => ShowErrorToast(error)) // Already on UI thread
    ///     .InitializeAsync();
    /// </code>
    /// </example>
    public PaginatedQueryHelperBuilder<T> OnError(Action<string> onError)
    {
        _onError = onError ?? throw new ArgumentNullException(nameof(onError));
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when a page boundary is added.
    /// If WithUIThreadMarshalling() was called, this callback will be invoked on the UI thread.
    /// </summary>
    /// <param name="onPageBoundaryAdded">Callback that receives the boundary index.</param>
    /// <returns>The builder for method chaining.</returns>
    public PaginatedQueryHelperBuilder<T> OnPageBoundaryAdded(Action<int> onPageBoundaryAdded)
    {
        _onPageBoundaryAdded = onPageBoundaryAdded ?? throw new ArgumentNullException(nameof(onPageBoundaryAdded));
        return this;
    }

    /// <summary>
    /// Sets a callback to be invoked when subscription status changes (for debugging).
    /// If WithUIThreadMarshalling() was called, this callback will be invoked on the UI thread.
    /// </summary>
    /// <param name="onSubscriptionStatusChanged">Callback that receives the status message.</param>
    /// <returns>The builder for method chaining.</returns>
    public PaginatedQueryHelperBuilder<T> OnSubscriptionStatusChanged(Action<string> onSubscriptionStatusChanged)
    {
        _onSubscriptionStatusChanged = onSubscriptionStatusChanged ?? throw new ArgumentNullException(nameof(onSubscriptionStatusChanged));
        return this;
    }

    /// <summary>
    /// Builds and returns the paginated query helper.
    /// </summary>
    public PaginatedQueryHelper<T> Build()
    {
        if (_getId == null)
        {
            throw new InvalidOperationException("IdExtractor must be set. Call WithIdExtractor() before building.");
        }

        var helper = new PaginatedQueryHelper<T>(
            _client,
            _functionName,
            _pageSize,
            _args,
            _getId,
            _getSortKey,
            _extractSubscriptionItems,
            _subscriptionFactory,
            _subscriptionType,
            _logger,
            _enableDebugLogging,
            _syncContext);

        // Wire up event handlers if provided
        if (_onItemsUpdated != null)
        {
            helper.ItemsUpdated += (items, boundaries) =>
            {
                if (_syncContext?.HasContext == true)
                {
                    _syncContext.Post(() => _onItemsUpdated(items, boundaries));
                }
                else
                {
                    _onItemsUpdated(items, boundaries);
                }
            };
        }

        if (_onError != null)
        {
            helper.ErrorOccurred += error =>
            {
                if (_syncContext?.HasContext == true)
                {
                    _syncContext.Post(_onError, error);
                }
                else
                {
                    _onError(error);
                }
            };
        }

        if (_onPageBoundaryAdded != null)
        {
            helper.PageBoundaryAdded += boundary =>
            {
                if (_syncContext?.HasContext == true)
                {
                    _syncContext.Post(_onPageBoundaryAdded, boundary);
                }
                else
                {
                    _onPageBoundaryAdded(boundary);
                }
            };
        }

        if (_onSubscriptionStatusChanged != null)
        {
            helper.SubscriptionStatusChanged += status =>
            {
                if (_syncContext?.HasContext == true)
                {
                    _syncContext.Post(_onSubscriptionStatusChanged, status);
                }
                else
                {
                    _onSubscriptionStatusChanged(status);
                }
            };
        }

        return helper;
    }

    /// <summary>
    /// Builds, initializes, and returns the paginated query helper.
    /// This is a convenience method that combines Build() and InitializeAsync().
    /// </summary>
    /// <param name="enableSubscription">Whether to enable real-time subscription updates (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initialized paginated query helper.</returns>
    /// <example>
    /// <code>
    /// var helper = await client.CreatePaginatedQuery&lt;MessageDto&gt;("messages:get")
    ///     .WithPageSize(25)
    ///     .WithIdExtractor(msg => msg.Id)
    ///     .WithUIThreadMarshalling()
    ///     .OnItemsUpdated((items, boundaries) => UpdateUI(items))
    ///     .OnError(error => ShowError(error))
    ///     .InitializeAsync(); // Returns initialized helper
    /// </code>
    /// </example>
    public async Task<PaginatedQueryHelper<T>> InitializeAsync(bool enableSubscription = true, CancellationToken cancellationToken = default)
    {
        var helper = Build();
        await helper.InitializeAsync(enableSubscription, cancellationToken);
        return helper;
    }
}


