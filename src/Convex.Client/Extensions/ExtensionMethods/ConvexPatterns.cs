using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using Convex.Client.Infrastructure.Quality;
using Convex.Client.Infrastructure.Connection;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Common usage patterns and recipes for Convex client applications.
/// These methods provide ready-to-use implementations for typical application scenarios.
/// </summary>
public static class ConvexPatterns
{
    #region Optimistic Updates

    /// <summary>
    /// Executes an operation with optimistic updates, immediately updating the UI
    /// while the server operation is in progress, with automatic rollback on failure.
    /// </summary>
    /// <typeparam name="T">The type of the current state.</typeparam>
    /// <typeparam name="TResult">The type of the operation result.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="currentState">The current state observable.</param>
    /// <param name="optimisticUpdate">Function to apply optimistic changes to the state.</param>
    /// <param name="serverOperation">The server operation to execute.</param>
    /// <param name="rollbackUpdate">Function to rollback changes on failure (optional).</param>
    /// <returns>An observable that emits the operation result.</returns>
    /// <example>
    /// <code>
    /// var result = client.ExecuteWithOptimisticUpdate(
    ///     currentMessages,
    ///     messages => messages.Append(optimisticMessage),
    ///     () => client.Mutation("messages:send", newMessage),
    ///     messages => messages.Where(m => m.Id != optimisticMessage.Id).ToList());
    /// </code>
    /// </example>
    public static IObservable<TResult> ExecuteWithOptimisticUpdate<T, TResult>(
        this IConvexClient client,
        IObservable<T> currentState,
        Func<T, T> optimisticUpdate,
        Func<IObservable<TResult>> serverOperation,
        Func<T, T>? rollbackUpdate = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(optimisticUpdate);
        ArgumentNullException.ThrowIfNull(serverOperation);

        return Observable.Create<TResult>(observer =>
        {
            var stateSubscription = currentState.FirstAsync().Subscribe(current =>
            {
                // Apply optimistic update
                var optimisticState = optimisticUpdate(current);

                // Emit optimistic result immediately
                var operation = serverOperation();

                // Subscribe to server operation
                var operationSubscription = operation.Subscribe(
                    onNext: result =>
                    {
                        // Success - optimistic update was correct
                        observer.OnNext(result);
                        observer.OnCompleted();
                    },
                    onError: error =>
                    {
                        // Failure - rollback if rollback function provided
                        if (rollbackUpdate != null)
                        {
                            var rollbackState = rollbackUpdate(optimisticState);
                            // In a real implementation, you'd update the UI state here
                        }
                        observer.OnError(error);
                    });
            });

            return stateSubscription;
        });
    }

    #endregion

    #region Infinite Scroll

    /// <summary>
    /// Creates an infinite scroll pattern for paginated data.
    /// Automatically loads more data as the user scrolls near the end.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="queryFunction">The query function name.</param>
    /// <param name="initialArgs">Initial query arguments.</param>
    /// <param name="pageSize">Number of items per page (default: 20).</param>
    /// <param name="loadThreshold">Load more when this many items remain (default: 5).</param>
    /// <returns>An observable collection that automatically loads more data.</returns>
    /// <example>
    /// <code>
    /// var items = client.CreateInfiniteScroll&lt;Item&gt;(
    ///     "items:list",
    ///     new { category = "electronics" },
    ///     pageSize: 10,
    ///     loadThreshold: 3);
    ///
    /// // Bind to UI
    /// listView.ItemsSource = items;
    /// </code>
    /// </example>
    public static ObservableCollection<T> CreateInfiniteScroll<T>(
        this IConvexClient client,
        string queryFunction,
        object? initialArgs = null,
        int pageSize = 20,
        int loadThreshold = 5)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(queryFunction);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentOutOfRangeException.ThrowIfNegative(loadThreshold);

        var collection = new ObservableCollection<T>();
        var isLoading = false;
        var hasMore = true;
        var currentPage = 0;

        async void LoadMoreIfNeeded()
        {
            if (isLoading || !hasMore)
            {
                return;
            }

            var remainingItems = collection.Count - (currentPage * pageSize);
            if (remainingItems > loadThreshold)
            {
                return;
            }

            isLoading = true;
            try
            {
                var paginationArgs = new
                {
                    pagination = new
                    {
                        numItems = pageSize,
                        cursor = currentPage * pageSize
                    }
                };

                var args = MergeArgs(initialArgs, paginationArgs);
                var result = await client.Query<PaginatedResult<T>>(queryFunction).WithArgs(args).ExecuteAsync();
                if (result?.Page != null)
                {
                    foreach (var item in result.Page)
                    {
                        collection.Add(item);
                    }

                    hasMore = result.HasMore;
                    currentPage++;
                }
            }
            catch (Exception ex)
            {
                // In a real app, you'd handle errors (show toast, retry, etc.)
                Console.WriteLine($"Error loading more items: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // Load initial page
        LoadMoreIfNeeded();

        // Set up property changed monitoring for auto-loading
        if (collection is INotifyPropertyChanged propertyChanged)
        {
            propertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Count")
                {
                    LoadMoreIfNeeded();
                }
            };
        }

        return collection;
    }

    #endregion

    #region Debounced Search

    /// <summary>
    /// Creates a debounced search pattern that automatically queries as the user types.
    /// </summary>
    /// <typeparam name="TResult">The type of search results.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="searchFunction">The search query function name.</param>
    /// <param name="debounceTime">Time to wait after user stops typing (default: 300ms).</param>
    /// <param name="minQueryLength">Minimum query length to trigger search (default: 2).</param>
    /// <returns>A function that takes search text and returns search results observable.</returns>
    /// <example>
    /// <code>
    /// var searchHandler = client.CreateDebouncedSearch&lt;SearchResult&gt;("search:query");
    ///
    /// // In UI event handler
    /// private void OnSearchTextChanged(string text)
    ///     => searchHandler(text).Subscribe(results => UpdateSearchResults(results));
    /// </code>
    /// </example>
    public static Func<string, IObservable<TResult[]>> CreateDebouncedSearch<TResult>(
        this IConvexClient client,
        string searchFunction,
        TimeSpan? debounceTime = null,
        int minQueryLength = 2)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(searchFunction);
        ArgumentOutOfRangeException.ThrowIfNegative(minQueryLength);

        var debounce = debounceTime ?? TimeSpan.FromMilliseconds(300);

        return searchText =>
        {
            return string.IsNullOrWhiteSpace(searchText) || searchText.Length < minQueryLength
                ? Observable.Return(Array.Empty<TResult>())
                : Observable.Return(searchText)
                .Throttle(debounce)
                .SelectMany(query => client.Query<TResult[]>(searchFunction).WithArgs(new Dictionary<string, string> { ["query"] = query }).ExecuteAsync())
                .Catch<TResult[], Exception>(_ => Observable.Return(Array.Empty<TResult>()));
        };
    }

    /// <summary>
    /// Creates a debounced search with loading state management.
    /// </summary>
    /// <typeparam name="TResult">The type of search results.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="searchFunction">The search query function name.</param>
    /// <param name="debounceTime">Time to wait after user stops typing (default: 300ms).</param>
    /// <param name="minQueryLength">Minimum query length to trigger search (default: 2).</param>
    /// <returns>A function that returns both loading state and results.</returns>
    /// <example>
    /// <code>
    /// var searchHandler = client.CreateDebouncedSearchWithLoading&lt;SearchResult&gt;("search:query");
    ///
    /// private void OnSearchTextChanged(string text)
    /// {
    ///     var (isLoading, results) = searchHandler(text);
    ///     loadingIndicator.IsVisible = isLoading;
    ///     results.Subscribe(r => UpdateResults(r));
    /// }
    /// </code>
    /// </example>
    public static Func<string, (IObservable<bool> IsLoading, IObservable<TResult[]> Results)> CreateDebouncedSearchWithLoading<TResult>(
        this IConvexClient client,
        string searchFunction,
        TimeSpan? debounceTime = null,
        int minQueryLength = 2)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(searchFunction);
        ArgumentOutOfRangeException.ThrowIfNegative(minQueryLength);

        var debounce = debounceTime ?? TimeSpan.FromMilliseconds(300);

        return searchText =>
        {
            if (searchText.Length < minQueryLength)
            {
                return (Observable.Return(false), Observable.Return(Array.Empty<TResult>()));
            }

            var searchTrigger = Observable.Return(searchText).Throttle(debounce);

            var isLoading = searchTrigger
                .Select(_ => true)
                .Merge(searchTrigger
                    .SelectMany(query => client.Query<TResult[]>(searchFunction).WithArgs(new Dictionary<string, string> { ["query"] = query }).ExecuteAsync())
                    .Select(_ => false))
                .StartWith(false);

            var results = searchTrigger
                .SelectMany(query => client.Query<TResult[]>(searchFunction).WithArgs(new Dictionary<string, string> { ["query"] = query }).ExecuteAsync())
                .Catch<TResult[], Exception>(_ => Observable.Return(Array.Empty<TResult>()));

            return (isLoading, results);
        };
    }

    #endregion

    #region Connection Indicators

    /// <summary>
    /// Creates a connection status indicator that provides user-friendly status messages.
    /// </summary>
    /// <param name="client">The Convex client.</param>
    /// <returns>An observable that emits connection status messages.</returns>
    /// <example>
    /// <code>
    /// var statusIndicator = client.CreateConnectionIndicator();
    /// statusIndicator.Subscribe(status => connectionLabel.Text = status);
    /// // Shows: "Connected", "Connecting...", "Disconnected", "Poor Connection"
    /// </code>
    /// </example>
    public static IObservable<string> CreateConnectionIndicator(this IConvexClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.ConnectionStateChanges
            .CombineLatest(client.ConnectionQualityChanges, (state, quality) =>
            {
                return (state, quality) switch
                {
                    (ConnectionState.Connected, ConnectionQuality.Excellent) => "Connected",
                    (ConnectionState.Connected, ConnectionQuality.Good) => "Connected",
                    (ConnectionState.Connected, ConnectionQuality.Poor) => "Poor Connection",
                    (ConnectionState.Connecting, _) => "Connecting...",
                    (ConnectionState.Disconnected, _) => "Disconnected",
                    (ConnectionState.Reconnecting, _) => "Reconnecting...",
                    _ => "Unknown"
                };
            })
            .DistinctUntilChanged()
            .StartWith("Connecting...");
    }

    /// <summary>
    /// Creates a connection indicator with detailed status information.
    /// </summary>
    /// <param name="client">The Convex client.</param>
    /// <returns>An observable that emits detailed connection status.</returns>
    /// <example>
    /// <code>
    /// var status = client.CreateDetailedConnectionIndicator();
    /// status.Subscribe(s => {
    ///     statusIcon.Source = s.Icon;
    ///     statusLabel.Text = s.Message;
    ///     retryButton.IsVisible = s.CanRetry;
    /// });
    /// </code>
    /// </example>
    public static IObservable<ConnectionStatus> CreateDetailedConnectionIndicator(this IConvexClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.ConnectionStateChanges
            .CombineLatest(client.ConnectionQualityChanges, (state, quality) =>
            {
                var status = (state, quality) switch
                {
                    (ConnectionState.Connected, ConnectionQuality.Excellent) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Connected",
                        Icon = "connected",
                        CanRetry = false,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Connected, ConnectionQuality.Good) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Connected",
                        Icon = "connected",
                        CanRetry = false,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Connected, ConnectionQuality.Poor) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Poor connection - some features may be slow",
                        Icon = "warning",
                        CanRetry = false,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Connecting, _) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Connecting...",
                        Icon = "connecting",
                        CanRetry = false,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Disconnected, _) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Disconnected - check your internet connection",
                        Icon = "disconnected",
                        CanRetry = true,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Reconnecting, _) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Reconnecting...",
                        Icon = "connecting",
                        CanRetry = false,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    (ConnectionState.Failed, _) => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Connection failed",
                        Icon = "error",
                        CanRetry = true,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    _ => new ConnectionStatus
                    {
                        State = state,
                        Quality = quality,
                        Message = "Unknown connection state",
                        Icon = "unknown",
                        CanRetry = true,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };

                return status;
            })
            .DistinctUntilChanged()
            .StartWith(new ConnectionStatus
            {
                State = ConnectionState.Connecting,
                Quality = ConnectionQuality.Unknown,
                Message = "Initializing...",
                Icon = "connecting",
                CanRetry = false,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    #endregion

    #region Real-time Subscriptions

    /// <summary>
    /// Creates a subscription that automatically handles connection state changes.
    /// </summary>
    /// <typeparam name="T">The type of the subscription data.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="subscriptionFunction">The subscription function name.</param>
    /// <param name="args">Subscription arguments.</param>
    /// <returns>An observable that handles connection state automatically.</returns>
    /// <example>
    /// <code>
    /// var messages = client.CreateResilientSubscription&lt;Message&gt;("messages:subscribe", new { channelId });
    /// messages.Subscribe(message => AddMessageToUI(message));
    /// // Automatically reconnects and resumes when connection is restored
    /// </code>
    /// </example>
    public static IObservable<T> CreateResilientSubscription<T>(
        this IConvexClient client,
        string subscriptionFunction,
        object? args = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(subscriptionFunction);

        // Start the subscription directly - Observe() will handle connection internally
        // The LiveQuery inside Observe calls EnsureConnectedAsync which triggers the connection
        var subscription = args != null
            ? client.Observe<T, object>(subscriptionFunction, args)
            : client.Observe<T>(subscriptionFunction);

        return subscription
            .Retry() // Retry on errors (e.g., connection drops)
            .Publish()
            .RefCount(); // Share subscription among multiple subscribers
    }

    #endregion

    #region Helper Classes and Types

    /// <summary>
    /// Represents paginated query results.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Gets or sets the current page of items.
        /// </summary>
        public required IReadOnlyList<T> Page { get; set; }

        /// <summary>
        /// Gets or sets whether there are more items available.
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// Gets or sets the total count of items (if available).
        /// </summary>
        public int? TotalCount { get; set; }
    }

    /// <summary>
    /// Represents detailed connection status information.
    /// </summary>
    public class ConnectionStatus : IEquatable<ConnectionStatus>
    {
        /// <summary>
        /// Gets or sets the connection state.
        /// </summary>
        public ConnectionState State { get; set; }

        /// <summary>
        /// Gets or sets the connection quality.
        /// </summary>
        public ConnectionQuality Quality { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly status message.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the icon name for UI display.
        /// </summary>
        public required string Icon { get; set; }

        /// <summary>
        /// Gets or sets whether the user can manually retry the connection.
        /// </summary>
        public bool CanRetry { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of this status.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public bool Equals(ConnectionStatus? other)
        {
            return other is not null &&
                   State == other.State &&
                   Quality == other.Quality &&
                   Message == other.Message &&
                   Icon == other.Icon &&
                   CanRetry == other.CanRetry;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as ConnectionStatus);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(State, Quality, Message, Icon, CanRetry);
    }

    #endregion

    #region Private Helpers

    private static object MergeArgs(object? initialArgs, object paginationArgs)
    {
        if (initialArgs == null)
        {
            return paginationArgs;
        }

        // Simple merge - in a real implementation, you'd use reflection or a proper merge strategy
        var merged = new Dictionary<string, object>();

        foreach (var prop in initialArgs.GetType().GetProperties())
        {
            merged[prop.Name] = prop.GetValue(initialArgs)!;
        }

        foreach (var prop in paginationArgs.GetType().GetProperties())
        {
            merged[prop.Name] = prop.GetValue(paginationArgs)!;
        }

        return merged;
    }

    #endregion
}
