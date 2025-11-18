# Convex .NET Client API Specification

This document provides a complete specification of all public APIs in the Convex .NET client library.

## Table of Contents

1. [Core Client](#core-client)
2. [Queries](#queries)
3. [Mutations](#mutations)
4. [Actions](#actions)
5. [Subscriptions](#subscriptions)
6. [Pagination](#pagination)
7. [Caching](#caching)
8. [Authentication](#authentication)
9. [File Storage](#file-storage)
10. [Vector Search](#vector-search)
11. [Scheduling](#scheduling)
12. [HTTP Actions](#http-actions)
13. [Health & Diagnostics](#health--diagnostics)
14. [Resilience](#resilience)
15. [Middleware & Interceptors](#middleware--interceptors)
16. [Error Handling](#error-handling)
17. [Connection Management](#connection-management)
18. [Validation](#validation)
19. [Attributes](#attributes)
20. [Shared Types](#shared-types)

---

## Core Client

### IConvexClient

Main interface for interacting with Convex backend.

**Namespace:** `Convex.Client`

**Properties:**

- `string DeploymentUrl { get; }` - Gets the Convex deployment URL
- `TimeSpan Timeout { get; set; }` - Gets or sets the default timeout for HTTP operations
- `ConnectionState ConnectionState { get; }` - Gets the current WebSocket connection state
- `IObservable<ConnectionState> ConnectionStateChanges { get; }` - Observable stream of connection state changes
- `IObservable<ConnectionQuality> ConnectionQualityChanges { get; }` - Observable stream of connection quality changes
- `IConvexPagination PaginationSlice { get; }` - Gets the pagination slice for cursor-based pagination

**Methods:**

- `IQueryBuilder<TResult> Query<TResult>(string functionName)` - Creates a fluent query builder
- `IBatchQueryBuilder Batch()` - Creates a batch query builder for executing multiple queries
- `IMutationBuilder<TResult> Mutate<TResult>(string functionName)` - Creates a fluent mutation builder
- `IActionBuilder<TResult> Action<TResult>(string functionName)` - Creates a fluent action builder
- `IObservable<T> Observe<T>(string functionName)` - Creates a real-time observable stream of a Convex query
- `IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull` - Creates a real-time observable stream with arguments
- `T? GetCachedValue<T>(string functionName)` - Gets a cached value from an active subscription
- `bool TryGetCachedValue<T>(string functionName, out T? value)` - Tries to get a cached value
- `void DefineQueryDependency(string mutationName, params string[] invalidates)` - Defines query dependencies for automatic cache invalidation
- `Task InvalidateQueryAsync(string queryName)` - Manually invalidates a specific query from the cache
- `Task InvalidateQueriesAsync(string pattern)` - Manually invalidates all queries matching a pattern

### ConvexClient

Unified Convex client implementation.

**Namespace:** `Convex.Client`

**Constructors:**

- `ConvexClient(string deploymentUrl)` - Creates a new ConvexClient with the specified deployment URL
- `ConvexClient(string deploymentUrl, ConvexClientOptions? options)` - Creates a new ConvexClient with options

**Properties:**

- `string DeploymentUrl { get; }` - Gets the Convex deployment URL
- `TimeSpan Timeout { get; set; }` - Gets or sets the default timeout for HTTP operations
- `ConnectionState ConnectionState { get; }` - Gets the current WebSocket connection state
- `IObservable<ConnectionState> ConnectionStateChanges { get; }` - Observable stream of connection state changes
- `IObservable<ConnectionQuality> ConnectionQualityChanges { get; }` - Observable stream of connection quality changes
- `IObservable<AuthenticationState> AuthenticationStateChanges { get; }` - Observable stream of authentication state changes
- `Exception? PreConnectError { get; }` - Gets the error that occurred during PreConnect, if any
- `FileStorageSlice FileStorageSlice { get; }` - Gets the FileStorage slice
- `VectorSearchSlice VectorSearchSlice { get; }` - Gets the VectorSearch slice
- `HttpActionsSlice HttpActionsSlice { get; }` - Gets the HTTP actions slice
- `SchedulingSlice SchedulingSlice { get; }` - Gets the scheduling slice
- `IConvexPagination PaginationSlice { get; }` - Gets the pagination slice
- `CachingSlice CachingSlice { get; }` - Gets the caching slice
- `AuthenticationSlice AuthenticationSlice { get; }` - Gets the Authentication slice
- `HealthSlice HealthSlice { get; }` - Gets the Health slice
- `DiagnosticsSlice DiagnosticsSlice { get; }` - Gets the Diagnostics slice
- `ResilienceSlice ResilienceSlice { get; }` - Gets the Resilience slice
- `TimestampManager TimestampManager { get; }` - Gets the timestamp manager

**Methods:**

- `Task EnsureConnectedAsync(CancellationToken cancellationToken = default)` - Ensures the WebSocket connection is established
- `Task<ConvexHealthCheck> GetHealthAsync()` - Gets the current health status
- `Task<ConnectionQualityInfo> GetConnectionQualityAsync()` - Gets the current connection quality assessment
- `void Dispose()` - Disposes the client and releases resources

### ConvexClientBuilder

Fluent builder for creating and configuring ConvexClient instances.

**Namespace:** `Convex.Client`

**Methods:**

- `ConvexClientBuilder UseDeployment(string deploymentUrl)` - Sets the Convex deployment URL
- `ConvexClientBuilder WithHttpClient(HttpClient httpClient)` - Sets the HttpClient to use
- `ConvexClientBuilder WithTimeout(TimeSpan timeout)` - Sets the default timeout for HTTP operations
- `ConvexClientBuilder WithAutoReconnect(int maxAttempts = 5, int delayMs = 1000)` - Configures automatic reconnection
- `ConvexClientBuilder WithReconnectionPolicy(ReconnectionPolicy policy)` - Configures the reconnection policy
- `ConvexClientBuilder WithSyncContext(SynchronizationContext? synchronizationContext)` - Sets the SynchronizationContext for event marshalling
- `ConvexClientBuilder WithLogging(ILogger logger)` - Sets the logger for structured logging
- `ConvexClientBuilder EnableDebugLogging(bool enabled = true)` - Enables or disables debug-level logging
- `ConvexClientBuilder PreConnect()` - Enables pre-connection of the WebSocket
- `ConvexClientBuilder UseMiddleware(IConvexMiddleware middleware)` - Adds a middleware instance
- `ConvexClientBuilder UseMiddleware<TMiddleware>() where TMiddleware : IConvexMiddleware, new()` - Adds a middleware type
- `ConvexClientBuilder UseMiddleware<TMiddleware>(Func<TMiddleware> factory) where TMiddleware : IConvexMiddleware` - Adds a middleware with factory
- `ConvexClientBuilder Use(Func<ConvexRequest, ConvexRequestDelegate, Task<ConvexResponse>> middleware)` - Adds inline middleware
- `ConvexClientBuilder WithSchemaValidation(Action<SchemaValidationOptions> configure)` - Configures schema validation
- `ConvexClientBuilder WithStrictSchemaValidation()` - Enables strict schema validation
- `ConvexClientBuilder WithRequestLogging(bool enabled = true)` - Adds request logging middleware
- `ConvexClientBuilder WithDevelopmentDefaults()` - Applies development-friendly default settings
- `ConvexClientBuilder WithProductionDefaults()` - Applies production-friendly default settings
- `ConvexClient Build()` - Builds the ConvexClient
- `Task<ConvexClient> BuildAsync(CancellationToken cancellationToken = default)` - Builds the ConvexClient asynchronously

### ConvexClientOptions

Configuration options for ConvexClient.

**Namespace:** `Convex.Client`

**Properties:**

- `string? DeploymentUrl { get; set; }` - Gets or sets the Convex deployment URL
- `string? AdminKey { get; set; }` - Gets or sets the admin authentication key
- `HttpClient? HttpClient { get; set; }` - Gets or sets the HttpClient to use
- `TimeSpan DefaultTimeout { get; set; }` - Gets or sets the default timeout (default: 30 seconds)
- `ReconnectionPolicy? ReconnectionPolicy { get; set; }` - Gets or sets the reconnection policy
- `SynchronizationContext? SynchronizationContext { get; set; }` - Gets or sets the SynchronizationContext
- `ILogger? Logger { get; set; }` - Gets or sets the logger
- `bool EnableDebugLogging { get; set; }` - Gets or sets whether to enable debug logging (default: false)
- `bool AutoConnect { get; set; }` - Gets or sets whether to automatically connect (default: true)
- `bool PreConnect { get; set; }` - Gets or sets whether to pre-connect (default: false)
- `bool EnableQualityMonitoring { get; set; }` - Gets or sets whether to enable quality monitoring (default: true)
- `TimeSpan QualityCheckInterval { get; set; }` - Gets or sets the quality check interval (default: 10 seconds)
- `List<IConvexInterceptor> Interceptors { get; set; }` - Gets or sets the list of interceptors
- `SchemaValidationOptions? SchemaValidation { get; set; }` - Gets or sets schema validation options
- `ISchemaValidator? SchemaValidator { get; set; }` - Gets or sets the schema validator

**Methods:**

- `void Validate()` - Validates the options and throws if invalid

---

## Queries

### IQueryBuilder<TResult>

Fluent builder for creating and configuring Convex queries.

**Namespace:** `Convex.Client.Slices.Queries`

**Methods:**

- `IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull` - Sets the arguments to pass to the function
- `IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()` - Sets arguments using a builder function
- `IQueryBuilder<TResult> WithTimeout(TimeSpan timeout)` - Sets a timeout for the query execution
- `IQueryBuilder<TResult> IncludeMetadata()` - Includes metadata in the result
- `IQueryBuilder<TResult> UseConsistency(long timestamp)` - Executes with consistent read semantics (experimental, obsolete)
- `IQueryBuilder<TResult> Cached(TimeSpan cacheDuration)` - Caches the query result for the specified duration. Subsequent calls within the cache window will return the cached value without making a network request. The cache automatically expires after the specified duration.
- `IQueryBuilder<TResult> OnError(Action<Exception> onError)` - Registers an error callback
- `IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)` - Configures a retry policy
- `IQueryBuilder<TResult> WithRetry(RetryPolicy policy)` - Uses a predefined retry policy
- `Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)` - Executes the query and returns the result
- `Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)` - Executes and returns a Result object

### IBatchQueryBuilder

Builder for executing multiple queries in a single batch request.

**Namespace:** `Convex.Client.Slices.Queries`

**Methods:**

- `IBatchQueryBuilder Query<T>(string functionName)` - Adds a query without arguments
- `IBatchQueryBuilder Query<T, TArgs>(string functionName, TArgs args) where TArgs : notnull` - Adds a query with arguments
- `Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default)` - Executes 2 queries and returns tuple
- `Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default)` - Executes 3 queries
- `Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default)` - Executes 4 queries
- `Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default)` - Executes 5 queries
- `Task<(T1, T2, T3, T4, T5, T6)> ExecuteAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellationToken = default)` - Executes 6 queries
- `Task<(T1, T2, T3, T4, T5, T6, T7)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(CancellationToken cancellationToken = default)` - Executes 7 queries
- `Task<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7, T8>(CancellationToken cancellationToken = default)` - Executes 8 queries
- `Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default)` - Executes all queries and returns array
- `Task<Dictionary<string, object>> ExecuteAsDictionaryAsync(CancellationToken cancellationToken = default)` - Executes all queries and returns dictionary keyed by function name

---

## Mutations

### IMutationBuilder<TResult>

Fluent builder for creating and configuring Convex mutations.

**Namespace:** `Convex.Client.Slices.Mutations`

**Methods:**

- `IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull` - Sets the arguments
- `IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()` - Sets arguments using builder
- `IMutationBuilder<TResult> WithTimeout(TimeSpan timeout)` - Sets a timeout
- `IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate)` - Enables optimistic update
- `IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply)` - Enables optimistic update with value
- `IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(Func<TState> getter, Action<TState> setter, Func<TState, TState> update)` - Enables optimistic update with automatic rollback
- `IMutationBuilder<TResult> WithRollback(Action rollback)` - Registers a rollback action
- `IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess)` - Registers a success callback
- `IMutationBuilder<TResult> OnError(Action<Exception> onError)` - Registers an error callback
- `IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception` - Specifies rollback only for specific exceptions
- `IMutationBuilder<TResult> SkipQueue()` - Bypasses the mutation queue
- `IMutationBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)` - Configures a retry policy
- `IMutationBuilder<TResult> WithRetry(RetryPolicy policy)` - Uses a predefined retry policy
- `Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)` - Executes the mutation
- `Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)` - Executes and returns Result
- `IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<IOptimisticLocalStore, TArgs> updateFn) where TArgs : notnull` - Enables query-focused optimistic updates
- `IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn)` - Optimistically updates cached query result
- `IMutationBuilder<TResult> TrackPending(ISet<string> tracker, string key)` - Tracks pending mutation in collection
- `IMutationBuilder<TResult> WithCleanup(Action cleanup)` - Registers a cleanup action

---

## Actions

### IActionBuilder<TResult>

Fluent builder for creating and configuring Convex actions.

**Namespace:** `Convex.Client.Slices.Actions`

**Methods:**

- `IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull` - Sets the arguments
- `IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()` - Sets arguments using builder
- `IActionBuilder<TResult> WithTimeout(TimeSpan timeout)` - Sets a timeout
- `IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess)` - Registers a success callback
- `IActionBuilder<TResult> OnError(Action<Exception> onError)` - Registers an error callback
- `IActionBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)` - Configures a retry policy
- `IActionBuilder<TResult> WithRetry(RetryPolicy policy)` - Uses a predefined retry policy
- `Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)` - Executes the action
- `Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)` - Executes and returns Result

---

## Subscriptions

### ObservableConvexList<T>

Thread-safe observable collection for use with Convex subscriptions.

**Namespace:** `Convex.Client.Slices.Subscriptions`

**Constructors:**

- `ObservableConvexList(SynchronizationContext? synchronizationContext = null)` - Creates a new observable list
- `ObservableConvexList(IEnumerable<T> items, SynchronizationContext? synchronizationContext = null)` - Creates with initial items

**Properties:**

- `int Count { get; }` - Gets the number of items
- `bool IsReadOnly { get; }` - Gets whether the collection is read-only
- `T this[int index] { get; set; }` - Gets or sets the item at the specified index

**Events:**

- `event NotifyCollectionChangedEventHandler? CollectionChanged` - Occurs when the collection changes
- `event PropertyChangedEventHandler? PropertyChanged` - Occurs when a property value changes

**Methods:**

- `void Add(T item)` - Adds an item
- `void Clear()` - Removes all items
- `bool Contains(T item)` - Determines whether the collection contains an item
- `void CopyTo(T[] array, int arrayIndex)` - Copies elements to an array
- `IEnumerator<T> GetEnumerator()` - Returns an enumerator
- `int IndexOf(T item)` - Determines the index of an item
- `void Insert(int index, T item)` - Inserts an item at the specified index
- `bool Remove(T item)` - Removes the first occurrence of an item
- `void RemoveAt(int index)` - Removes the item at the specified index
- `void AddRange(IEnumerable<T> items)` - Adds multiple items
- `void ReplaceAll(IEnumerable<T> items)` - Replaces all items
- `int RemoveAll(Predicate<T> predicate)` - Removes all items matching a predicate
- `IDisposable BindToObservable(IObservable<IEnumerable<T>> observable)` - Binds to an observable stream
- `void Dispose()` - Disposes the collection

---

## Pagination

### IConvexPagination

Interface for creating paginated queries.

**Namespace:** `Convex.Client.Slices.Pagination`

**Methods:**

- `IPaginationBuilder<T> Query<T>(string functionName)` - Creates a pagination builder

### IPaginationBuilder<T>

Fluent builder for creating paginated queries.

**Namespace:** `Convex.Client.Slices.Pagination`

**Methods:**

- `IPaginationBuilder<T> WithPageSize(int pageSize)` - Sets the page size
- `IPaginationBuilder<T> WithArgs<TArgs>(TArgs args) where TArgs : notnull` - Sets the arguments
- `IPaginationBuilder<T> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()` - Sets arguments using builder
- `IPaginator<T> Build()` - Builds and returns a paginator

### IPaginator<T>

Represents a paginated query that can load pages on demand.

**Namespace:** `Convex.Client.Slices.Pagination`

**Properties:**

- `bool HasMore { get; }` - Gets whether there are more pages
- `int LoadedPageCount { get; }` - Gets the total number of pages loaded
- `IReadOnlyList<T> LoadedItems { get; }` - Gets all items loaded so far
- `IReadOnlyList<int> PageBoundaries { get; }` - Gets the list of page boundaries

**Events:**

- `event Action<int>? PageBoundaryAdded` - Raised when a new page boundary is added

**Methods:**

- `int GetPageIndex(int itemIndex)` - Gets the page index for an item
- `MergedPaginationResult<T> MergeWithSubscription(IEnumerable<T> subscriptionItems, Func<T, string> getId, Func<T, IComparable>? getSortKey = null)` - Merges with subscription updates
- `Task<IReadOnlyList<T>> LoadNextAsync(CancellationToken cancellationToken = default)` - Loads the next page
- `void Reset()` - Resets the paginator
- `IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken cancellationToken = default)` - Returns async enumerable

### PaginationOptions

Options passed to a paginated query.

**Namespace:** `Convex.Client.Slices.Pagination`

**Properties:**

- `int NumItems { get; set; }` - Number of items per page
- `string? Cursor { get; set; }` - Cursor for pagination
- `string? EndCursor { get; set; }` - End cursor
- `int? MaximumRowsRead { get; set; }` - Maximum rows to read
- `long? MaximumBytesRead { get; set; }` - Maximum bytes to read
- `int? Id { get; set; }` - Query ID

### PaginationResult<T>

Result of paginating a database query.

**Namespace:** `Convex.Client.Slices.Pagination`

**Properties:**

- `List<T> Page { get; set; }` - The page of results
- `bool IsDone { get; set; }` - Whether pagination is complete
- `string? ContinueCursor { get; set; }` - Cursor for next page
- `string? SplitCursor { get; set; }` - Split cursor
- `PageStatus? PageStatus { get; set; }` - Status of the page

### PageStatus

Status of a paginated query page.

**Namespace:** `Convex.Client.Slices.Pagination`

**Values:**

- `Normal` - Normal page
- `SplitRecommended` - Split recommended
- `SplitRequired` - Split required

### MergedPaginationResult<T>

Result of merging paginated items with subscription updates.

**Namespace:** `Convex.Client.Slices.Pagination`

**Properties:**

- `IReadOnlyList<T> MergedItems { get; set; }` - The merged list of items
- `IReadOnlyList<int> AdjustedBoundaries { get; set; }` - Adjusted page boundaries

### ConvexPaginationException

Exception thrown when pagination operations fail.

**Namespace:** `Convex.Client.Slices.Pagination`

**Properties:**

- `string? FunctionName { get; }` - The function name

---

## Caching

### IConvexCache

Represents a cache for storing query results.

**Namespace:** `Convex.Client.Shared.Caching`

**Properties:**

- `int Count { get; }` - Gets the number of cached results
- `IEnumerable<string> Keys { get; }` - Gets all cached query names

**Methods:**

- `bool TryGet<T>(string queryName, out T? value)` - Tries to get a cached result. Automatically removes expired entries.
- `void Set<T>(string queryName, T value)` - Sets a query result in the cache without expiration
- `void Set<T>(string queryName, T value, TimeSpan? ttl)` - Sets a query result in the cache with an optional time-to-live (TTL). The entry will automatically expire after the specified duration.
- `bool TryUpdate<T>(string queryName, Func<T, T> updateFn)` - Updates a cached result. Returns false if the entry doesn't exist, has expired, or type mismatch.
- `bool Remove(string queryName)` - Removes a query result
- `int RemovePattern(string pattern)` - Removes all matching results
- `void Clear()` - Clears all cached results

### CachingSlice

Caching slice providing in-memory caching for query results.

**Namespace:** `Convex.Client.Slices.Caching`

**Properties:**

- `int Count { get; }` - Gets the number of cached results
- `IEnumerable<string> Keys { get; }` - Gets all cached query names

**Methods:**

- `bool TryGet<T>(string queryName, out T? value)` - Tries to get a cached result. Automatically removes expired entries.
- `void Set<T>(string queryName, T value)` - Sets a query result without expiration
- `void Set<T>(string queryName, T value, TimeSpan? ttl)` - Sets a query result with an optional time-to-live (TTL). The entry will automatically expire after the specified duration.
- `bool TryUpdate<T>(string queryName, Func<T, T> updateFn)` - Updates a cached result. Returns false if the entry doesn't exist, has expired, or type mismatch.
- `bool Remove(string queryName)` - Removes a query result
- `int RemovePattern(string pattern)` - Removes all matching results
- `void Clear()` - Clears all cached results

### ConvexCacheException

Exception thrown when cache operations fail.

**Namespace:** `Convex.Client.Shared.Caching`

**Properties:**

- `string? QueryName { get; }` - The query name

---

## Authentication

### IConvexAuthentication

Manages authentication state and token handling.

**Namespace:** `Convex.Client.Slices.Authentication`

**Properties:**

- `AuthenticationState AuthenticationState { get; }` - Gets the current authentication state
- `string? CurrentAuthToken { get; }` - Gets the current authentication token

**Events:**

- `event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged` - Fired when authentication state changes

**Methods:**

- `Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default)` - Sets the authentication token
- `Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default)` - Sets the admin authentication key
- `Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default)` - Sets an authentication token provider
- `Task ClearAuthAsync(CancellationToken cancellationToken = default)` - Clears all authentication information
- `Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default)` - Gets the current authentication token
- `Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default)` - Gets authentication headers

### AuthenticationSlice

Authentication slice implementation.

**Namespace:** `Convex.Client.Slices.Authentication`

**Properties:**

- `AuthenticationState AuthenticationState { get; }` - Gets the current authentication state
- `string? CurrentAuthToken { get; }` - Gets the current authentication token

**Events:**

- `event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged` - Fired when authentication state changes

**Methods:**

- `Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default)` - Sets the authentication token
- `Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default)` - Sets the admin authentication key
- `Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default)` - Sets an authentication token provider
- `Task ClearAuthAsync(CancellationToken cancellationToken = default)` - Clears all authentication information
- `Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default)` - Gets the current authentication token
- `Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default)` - Gets authentication headers

### AuthenticationState

Enumeration of authentication states.

**Namespace:** `Convex.Client.Slices.Authentication`

**Values:**

- `Unauthenticated` - No authentication is configured
- `Authenticated` - Authentication is configured and valid
- `AuthenticationFailed` - Authentication failed or token is invalid
- `TokenExpired` - Authentication token has expired

### AuthenticationStateChangedEventArgs

Event arguments for authentication state changes.

**Namespace:** `Convex.Client.Slices.Authentication`

**Properties:**

- `AuthenticationState State { get; }` - The new authentication state
- `string? ErrorMessage { get; }` - Optional error message if authentication failed

### IAuthTokenProvider

Interface for providing authentication tokens.

**Namespace:** `Convex.Client.Shared.Common`

**Methods:**

- `Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)` - Gets the current authentication token

---

## File Storage

### IConvexFileStorage

Interface for Convex file storage operations.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Methods:**

- `Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default)` - Generates a temporary upload URL
- `Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)` - Uploads a file using upload URL
- `Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)` - Uploads a file directly
- `Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default)` - Downloads a file
- `Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default)` - Gets a temporary download URL
- `Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default)` - Gets file metadata
- `Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default)` - Deletes a file

### FileStorageSlice

FileStorage slice implementation.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Methods:**

- `Task<ConvexUploadUrlResponse> GenerateUploadUrlAsync(string? filename = null, CancellationToken cancellationToken = default)` - Generates upload URL
- `Task<string> UploadFileAsync(string uploadUrl, Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)` - Uploads file
- `Task<string> UploadFileAsync(Stream fileContent, string contentType, string? filename = null, CancellationToken cancellationToken = default)` - Uploads file directly
- `Task<Stream> DownloadFileAsync(string storageId, CancellationToken cancellationToken = default)` - Downloads file
- `Task<string> GetDownloadUrlAsync(string storageId, CancellationToken cancellationToken = default)` - Gets download URL
- `Task<ConvexFileMetadata> GetFileMetadataAsync(string storageId, CancellationToken cancellationToken = default)` - Gets metadata
- `Task<bool> DeleteFileAsync(string storageId, CancellationToken cancellationToken = default)` - Deletes file

### ConvexUploadUrlResponse

Response from generating an upload URL.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Properties:**

- `string UploadUrl { get; init; }` - The temporary upload URL
- `string StorageId { get; init; }` - The storage ID

### ConvexFileMetadata

Metadata information about a file.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Properties:**

- `string StorageId { get; init; }` - The storage ID
- `string? Filename { get; init; }` - The filename
- `string? ContentType { get; init; }` - The MIME type
- `long Size { get; init; }` - The size in bytes
- `DateTimeOffset UploadedAt { get; init; }` - Upload timestamp
- `string? Sha256 { get; init; }` - SHA-256 hash

### FileStorageErrorType

Types of file storage errors.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Values:**

- `FileNotFound` - File not found
- `UploadFailed` - Upload failed
- `DownloadFailed` - Download failed
- `FileTooLarge` - File too large
- `InvalidFile` - Invalid file type
- `QuotaExceeded` - Storage quota exceeded
- `AccessDenied` - Access denied
- `InvalidStorageId` - Invalid storage ID

### ConvexFileStorageException

Exception thrown when file storage operations fail.

**Namespace:** `Convex.Client.Slices.FileStorage`

**Properties:**

- `FileStorageErrorType ErrorType { get; }` - The error type
- `string? StorageId { get; }` - The storage ID

---

## Vector Search

### IConvexVectorSearch

Interface for Convex vector search operations.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Methods:**

- `Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default)` - Performs similarity search
- `Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull` - Performs similarity search with filter
- `Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default)` - Performs text-based search
- `Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull` - Performs text-based search with filter
- `Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)` - Creates a text embedding
- `Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)` - Creates embeddings for multiple texts
- `Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default)` - Gets index information
- `Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default)` - Lists all indices

### VectorSearchSlice

VectorSearch slice implementation.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Methods:**

- `Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default)` - Performs similarity search
- `Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull` - Performs similarity search with filter
- `Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default)` - Performs text-based search
- `Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull` - Performs text-based search with filter
- `Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)` - Creates embedding
- `Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)` - Creates embeddings
- `Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default)` - Gets index info
- `Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default)` - Lists indices

### VectorSearchResult<T>

Result from a vector similarity search.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Properties:**

- `string Id { get; init; }` - The result ID
- `float Score { get; init; }` - The similarity score
- `T Data { get; init; }` - The result data
- `float[]? Vector { get; init; }` - The vector
- `Dictionary<string, JsonElement>? Metadata { get; init; }` - Additional metadata

### VectorIndexInfo

Information about a vector index.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Properties:**

- `string Name { get; init; }` - The index name
- `int Dimension { get; init; }` - The vector dimension
- `VectorDistanceMetric Metric { get; init; }` - The distance metric
- `long VectorCount { get; init; }` - The number of vectors
- `string Table { get; init; }` - The table name
- `string VectorField { get; init; }` - The vector field name
- `string? FilterField { get; init; }` - The filter field name
- `DateTimeOffset CreatedAt { get; init; }` - Creation timestamp
- `DateTimeOffset UpdatedAt { get; init; }` - Update timestamp

### VectorDistanceMetric

Distance metrics for vector similarity.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Values:**

- `Cosine` - Cosine similarity
- `Euclidean` - Euclidean distance
- `DotProduct` - Dot product

### VectorSearchErrorType

Types of vector search errors.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Values:**

- `IndexNotFound` - Index not found
- `InvalidDimensions` - Invalid dimensions
- `InvalidFilter` - Invalid filter
- `EmbeddingFailed` - Embedding creation failed
- `SearchFailed` - Search failed
- `InvalidModel` - Invalid model
- `RateLimitExceeded` - Rate limit exceeded
- `QuotaExceeded` - Quota exceeded

### ConvexVectorSearchException

Exception thrown when vector search operations fail.

**Namespace:** `Convex.Client.Slices.VectorSearch`

**Properties:**

- `VectorSearchErrorType ErrorType { get; }` - The error type
- `string? IndexName { get; }` - The index name

---

## Scheduling

### IConvexScheduler

Interface for Convex scheduling operations.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Methods:**

- `Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default)` - Schedules a function with delay
- `Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules with arguments
- `Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default)` - Schedules at specific time
- `Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules at time with arguments
- `Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default)` - Schedules recurring with cron
- `Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules recurring with cron and arguments
- `Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default)` - Schedules at intervals
- `Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules intervals with arguments
- `Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)` - Cancels a scheduled job
- `Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default)` - Gets job information
- `Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default)` - Lists scheduled jobs
- `Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default)` - Updates job schedule

### SchedulingSlice

Scheduling slice implementation.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Methods:**

- `Task<string> ScheduleAsync(string functionName, TimeSpan delay, CancellationToken cancellationToken = default)` - Schedules function
- `Task<string> ScheduleAsync<TArgs>(string functionName, TimeSpan delay, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules with args
- `Task<string> ScheduleAtAsync(string functionName, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default)` - Schedules at time
- `Task<string> ScheduleAtAsync<TArgs>(string functionName, DateTimeOffset scheduledTime, TArgs args, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules at time with args
- `Task<string> ScheduleRecurringAsync(string functionName, string cronExpression, string timezone = "UTC", CancellationToken cancellationToken = default)` - Schedules recurring
- `Task<string> ScheduleRecurringAsync<TArgs>(string functionName, string cronExpression, TArgs args, string timezone = "UTC", CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules recurring with args
- `Task<string> ScheduleIntervalAsync(string functionName, TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default)` - Schedules intervals
- `Task<string> ScheduleIntervalAsync<TArgs>(string functionName, TimeSpan interval, TArgs args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, CancellationToken cancellationToken = default) where TArgs : notnull` - Schedules intervals with args
- `Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken = default)` - Cancels job
- `Task<ConvexScheduledJob> GetJobAsync(string jobId, CancellationToken cancellationToken = default)` - Gets job
- `Task<IEnumerable<ConvexScheduledJob>> ListJobsAsync(ConvexJobStatus? status = null, string? functionName = null, int limit = 100, CancellationToken cancellationToken = default)` - Lists jobs
- `Task<bool> UpdateScheduleAsync(string jobId, ConvexScheduleConfig newSchedule, CancellationToken cancellationToken = default)` - Updates schedule

### ConvexScheduledJob

Information about a scheduled job.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Properties:**

- `string Id { get; init; }` - The job ID
- `string FunctionName { get; init; }` - The function name
- `ConvexJobStatus Status { get; init; }` - The job status
- `JsonElement? Arguments { get; init; }` - The job arguments
- `ConvexScheduleConfig Schedule { get; init; }` - The schedule configuration
- `DateTimeOffset CreatedAt { get; init; }` - Creation timestamp
- `DateTimeOffset UpdatedAt { get; init; }` - Update timestamp
- `DateTimeOffset? NextExecutionTime { get; init; }` - Next execution time
- `DateTimeOffset? LastExecutionTime { get; init; }` - Last execution time
- `int ExecutionCount { get; init; }` - Number of executions
- `ConvexJobError? LastError { get; init; }` - Last error if any
- `JsonElement? LastResult { get; init; }` - Last result
- `Dictionary<string, JsonElement>? Metadata { get; init; }` - Additional metadata

### ConvexScheduleConfig

Configuration for scheduling a job.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Properties:**

- `ConvexScheduleType Type { get; init; }` - The schedule type
- `DateTimeOffset? ScheduledTime { get; init; }` - Scheduled time for one-time jobs
- `string? CronExpression { get; init; }` - Cron expression for recurring jobs
- `TimeSpan? Interval { get; init; }` - Interval for interval-based jobs
- `string? Timezone { get; init; }` - Timezone
- `DateTimeOffset? StartTime { get; init; }` - Start time
- `DateTimeOffset? EndTime { get; init; }` - End time
- `int? MaxExecutions { get; init; }` - Maximum executions

**Static Methods:**

- `static ConvexScheduleConfig OneTime(DateTimeOffset scheduledTime)` - Creates one-time schedule
- `static ConvexScheduleConfig Cron(string cronExpression, string timezone = "UTC")` - Creates cron schedule
- `static ConvexScheduleConfig CreateInterval(TimeSpan interval, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)` - Creates interval schedule

### ConvexScheduleType

Types of schedules.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Values:**

- `OneTime` - One-time execution
- `Cron` - Cron-based recurring schedule
- `Interval` - Interval-based schedule

### ConvexJobStatus

Status of a scheduled job.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Values:**

- `Pending` - Job is pending
- `Running` - Job is running
- `Completed` - Job completed
- `Failed` - Job failed
- `Cancelled` - Job cancelled
- `Active` - Job is active
- `Paused` - Job is paused

### ConvexJobError

Error information for a failed job execution.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Properties:**

- `string? Code { get; init; }` - Error code
- `string Message { get; init; }` - Error message
- `string? StackTrace { get; init; }` - Stack trace
- `DateTimeOffset Timestamp { get; init; }` - Error timestamp
- `JsonElement? Details { get; init; }` - Additional details

### SchedulingErrorType

Types of scheduling errors.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Values:**

- `InvalidSchedule` - Invalid schedule
- `JobNotFound` - Job not found
- `FunctionNotFound` - Function not found
- `QuotaExceeded` - Quota exceeded
- `InvalidCronExpression` - Invalid cron expression
- `SchedulingFailed` - Scheduling failed
- `CannotCancel` - Cannot cancel
- `CannotUpdate` - Cannot update

### ConvexSchedulingException

Exception thrown when scheduling operations fail.

**Namespace:** `Convex.Client.Slices.Scheduling`

**Properties:**

- `SchedulingErrorType ErrorType { get; }` - The error type
- `string? JobId { get; }` - The job ID

---

## HTTP Actions

### IConvexHttpActions

Interface for Convex HTTP Actions.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Methods:**

- `Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls GET endpoint
- `Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls POST endpoint without body
- `Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls POST endpoint with body
- `Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls PUT endpoint without body
- `Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls PUT endpoint with body
- `Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls DELETE endpoint
- `Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls PATCH endpoint without body
- `Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls PATCH endpoint with body
- `Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls with custom method
- `Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls with custom method and body
- `Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Uploads file
- `Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull` - Calls webhook

### HttpActionsSlice

HTTP Actions slice implementation.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Methods:**

- `Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls GET
- `Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls POST
- `Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls POST with body
- `Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls PUT
- `Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls PUT with body
- `Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls DELETE
- `Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls PATCH
- `Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls PATCH with body
- `Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Calls with method
- `Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull` - Calls with method and body
- `Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)` - Uploads file
- `Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull` - Calls webhook

### ConvexHttpActionResponse<T>

Response from a Convex HTTP action.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Properties:**

- `HttpStatusCode StatusCode { get; init; }` - The HTTP status code
- `bool IsSuccess { get; }` - Whether the request was successful
- `T? Body { get; init; }` - The response body
- `string? RawBody { get; init; }` - The raw response body
- `Dictionary<string, string> Headers { get; init; }` - The response headers
- `string? ContentType { get; init; }` - The content type
- `double ResponseTimeMs { get; init; }` - The response time in milliseconds
- `ConvexHttpActionError? Error { get; init; }` - Error information if failed

### ConvexHttpActionError

Error information for failed HTTP action requests.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Properties:**

- `string? Code { get; init; }` - Error code
- `string Message { get; init; }` - Error message
- `JsonElement? Details { get; init; }` - Additional details
- `string? StackTrace { get; init; }` - Stack trace

### HttpActionErrorType

Types of HTTP action errors.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Values:**

- `ActionNotFound` - Action endpoint not found
- `InvalidRequest` - Invalid request format
- `AuthenticationRequired` - Authentication required
- `AccessDenied` - Access denied
- `RateLimitExceeded` - Rate limit exceeded
- `PayloadTooLarge` - Payload too large
- `UnsupportedMediaType` - Unsupported media type
- `InternalError` - Internal server error
- `NetworkError` - Network error
- `Timeout` - Request timeout
- `Unknown` - Unknown error

### ConvexHttpActionException

Exception thrown when HTTP action operations fail.

**Namespace:** `Convex.Client.Slices.HttpActions`

**Properties:**

- `HttpStatusCode? StatusCode { get; }` - The HTTP status code
- `string? ActionPath { get; }` - The action path
- `HttpActionErrorType ErrorType { get; }` - The error type

---

## Health & Diagnostics

### IConvexHealth

Provides connection health monitoring and metrics tracking.

**Namespace:** `Convex.Client.Slices.Health`

**Methods:**

- `void RecordMessageReceived()` - Records a message received
- `void RecordMessageSent()` - Records a message sent
- `void RecordLatency(double latencyMs)` - Records latency
- `void RecordReconnection()` - Records a reconnection
- `void RecordConnectionEstablished()` - Records connection established
- `void RecordError(Exception error)` - Records an error
- `double? GetAverageLatency()` - Gets average latency
- `long GetMessagesReceived()` - Gets messages received count
- `long GetMessagesSent()` - Gets messages sent count
- `int GetReconnectionCount()` - Gets reconnection count
- `TimeSpan? GetTimeSinceLastMessage()` - Gets time since last message
- `TimeSpan? GetConnectionUptime()` - Gets connection uptime
- `IReadOnlyList<Exception> GetRecentErrors()` - Gets recent errors
- `void Reset()` - Resets metrics
- `ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions)` - Creates health check

### HealthSlice

Health slice implementation.

**Namespace:** `Convex.Client.Slices.Health`

**Methods:**

- `void RecordMessageReceived()` - Records message received
- `void RecordMessageSent()` - Records message sent
- `void RecordLatency(double latencyMs)` - Records latency
- `void RecordReconnection()` - Records reconnection
- `void RecordConnectionEstablished()` - Records connection established
- `void RecordError(Exception error)` - Records error
- `double? GetAverageLatency()` - Gets average latency
- `long GetMessagesReceived()` - Gets messages received
- `long GetMessagesSent()` - Gets messages sent
- `int GetReconnectionCount()` - Gets reconnection count
- `TimeSpan? GetTimeSinceLastMessage()` - Gets time since last message
- `TimeSpan? GetConnectionUptime()` - Gets connection uptime
- `IReadOnlyList<Exception> GetRecentErrors()` - Gets recent errors
- `void Reset()` - Resets metrics
- `ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions)` - Creates health check

### IConvexDiagnostics

Provides performance tracking and diagnostics.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `IPerformanceTracker Performance { get; }` - Gets the performance tracker
- `IDisconnectTracker Disconnects { get; }` - Gets the disconnect tracker

### DiagnosticsSlice

Diagnostics slice implementation.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `IPerformanceTracker Performance { get; }` - Gets performance tracker
- `IDisconnectTracker Disconnects { get; }` - Gets disconnect tracker

### IPerformanceTracker

Tracks performance marks and measures.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `IReadOnlyList<PerformanceEntry> Entries { get; }` - Gets all performance entries

**Methods:**

- `PerformanceMark Mark(string markName, JsonElement? detail = null)` - Creates a performance mark
- `PerformanceMeasure Measure(string measureName, string? startMark = null, string? endMark = null)` - Creates a performance measure
- `IReadOnlyList<PerformanceEntry> GetEntriesByName(string name)` - Gets entries by name
- `IReadOnlyList<PerformanceEntry> GetEntriesByType(string type)` - Gets entries by type
- `void ClearMarks()` - Clears all marks
- `void ClearMeasures()` - Clears all measures
- `void Clear()` - Clears all marks and measures
- `PerformanceSummary GetSummary()` - Gets a summary

### IDisconnectTracker

Tracks disconnection events and provides statistics.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `bool IsDisconnected { get; }` - Gets whether currently disconnected
- `TimeSpan? CurrentDisconnectDuration { get; }` - Gets current disconnect duration
- `bool IsLongDisconnect { get; }` - Gets whether current disconnection is long
- `IReadOnlyList<DisconnectEvent> DisconnectHistory { get; }` - Gets disconnect history

**Methods:**

- `void RecordDisconnect()` - Records a disconnection
- `void RecordReconnect()` - Records a reconnection
- `DisconnectStats GetStats()` - Gets statistics
- `void Clear()` - Clears disconnect history

### PerformanceEntry

Base class for performance entries.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `string Name { get; init; }` - Entry name
- `string EntryType { get; init; }` - Entry type
- `TimeSpan Timestamp { get; init; }` - Timestamp
- `TimeSpan Duration { get; init; }` - Duration

### PerformanceMark

Represents a point in time for performance measurement.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `JsonElement? Detail { get; init; }` - Additional detail

### PerformanceMeasure

Represents a duration between two marks.

**Namespace:** `Convex.Client.Slices.Diagnostics`

### PerformanceSummary

Summary of all performance measurements.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `int TotalMarks { get; init; }` - Total marks
- `int TotalMeasures { get; init; }` - Total measures
- `List<PerformanceMeasureStats> MeasureStats { get; init; }` - Measure statistics

### PerformanceMeasureStats

Statistics for a specific measure name.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `string Name { get; init; }` - Measure name
- `int Count { get; init; }` - Count of measurements
- `TimeSpan TotalDuration { get; init; }` - Total duration
- `TimeSpan AverageDuration { get; init; }` - Average duration
- `TimeSpan MinDuration { get; init; }` - Minimum duration
- `TimeSpan MaxDuration { get; init; }` - Maximum duration

### DisconnectEvent

Represents a disconnection event.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `DateTimeOffset DisconnectedAt { get; init; }` - Disconnection timestamp
- `DateTimeOffset ReconnectedAt { get; init; }` - Reconnection timestamp
- `TimeSpan Duration { get; init; }` - Disconnection duration
- `bool WasLongDisconnect { get; init; }` - Whether it was a long disconnect

### DisconnectStats

Statistics about disconnect events.

**Namespace:** `Convex.Client.Slices.Diagnostics`

**Properties:**

- `int TotalDisconnects { get; init; }` - Total disconnects
- `int LongDisconnects { get; init; }` - Long disconnects
- `TimeSpan AverageDisconnectDuration { get; init; }` - Average duration
- `TimeSpan LongestDisconnect { get; init; }` - Longest disconnect
- `TimeSpan ShortestDisconnect { get; init; }` - Shortest disconnect
- `double LongDisconnectRate { get; }` - Long disconnect rate percentage

---

## Resilience

### RetryPolicy

Defines a retry policy for failed operations.

**Namespace:** `Convex.Client.Shared.Resilience`

**Properties:**

- `int MaxRetries { get; internal set; }` - Maximum number of retry attempts
- `BackoffStrategy BackoffStrategy { get; internal set; }` - Backoff strategy
- `TimeSpan InitialDelay { get; internal set; }` - Initial delay
- `double BackoffMultiplier { get; internal set; }` - Backoff multiplier
- `TimeSpan? MaxDelay { get; internal set; }` - Maximum delay
- `bool UseJitter { get; internal set; }` - Whether to use jitter
- `HashSet<Type> RetryableExceptionTypes { get; }` - Exception types that trigger retry
- `Action<int, Exception, TimeSpan>? OnRetryCallback { get; internal set; }` - Retry callback

**Static Methods:**

- `static RetryPolicy Default()` - Gets default retry policy
- `static RetryPolicy Aggressive()` - Gets aggressive retry policy
- `static RetryPolicy Conservative()` - Gets conservative retry policy
- `static RetryPolicy None()` - Gets retry policy with no retries

**Methods:**

- `TimeSpan CalculateDelay(int attemptNumber)` - Calculates delay before next retry
- `bool ShouldRetry(Exception exception)` - Determines if exception should trigger retry

### RetryPolicyBuilder

Fluent builder for creating retry policies.

**Namespace:** `Convex.Client.Shared.Resilience`

**Methods:**

- `RetryPolicyBuilder MaxRetries(int maxRetries)` - Sets maximum retries
- `RetryPolicyBuilder ExponentialBackoff(TimeSpan initialDelay, double multiplier = 2.0, bool useJitter = true)` - Configures exponential backoff
- `RetryPolicyBuilder LinearBackoff(TimeSpan initialDelay)` - Configures linear backoff
- `RetryPolicyBuilder ConstantBackoff(TimeSpan delay)` - Configures constant backoff
- `RetryPolicyBuilder WithMaxDelay(TimeSpan maxDelay)` - Sets maximum delay
- `RetryPolicyBuilder RetryOn<TException>() where TException : Exception` - Configures retry on exception type
- `RetryPolicyBuilder OnRetry(Action<int, Exception, TimeSpan> callback)` - Configures retry callback
- `RetryPolicy Build()` - Builds the retry policy

### BackoffStrategy

Defines the backoff strategy for retry delays.

**Namespace:** `Convex.Client.Shared.Resilience`

**Values:**

- `Constant` - Constant delay
- `Linear` - Linear backoff
- `Exponential` - Exponential backoff

### IConvexResilience

Provides retry and circuit breaker patterns.

**Namespace:** `Convex.Client.Slices.Resilience`

**Properties:**

- `RetryPolicy? RetryPolicy { get; set; }` - Gets or sets the retry policy
- `ICircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }` - Gets or sets the circuit breaker policy

**Methods:**

- `Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)` - Executes with resilience
- `Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)` - Executes with resilience

### ResilienceSlice

Resilience slice implementation.

**Namespace:** `Convex.Client.Slices.Resilience`

**Properties:**

- `RetryPolicy? RetryPolicy { get; set; }` - Gets or sets retry policy
- `ICircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }` - Gets or sets circuit breaker policy

**Methods:**

- `Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)` - Executes with resilience
- `Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)` - Executes with resilience

### ICircuitBreakerPolicy

Interface for circuit breaker policy.

**Namespace:** `Convex.Client.Shared.Resilience`

---

## Middleware & Interceptors

### IConvexMiddleware

Interface for Convex request/response middleware.

**Namespace:** `Convex.Client.Shared.Middleware`

**Methods:**

- `Task<ConvexResponse> InvokeAsync(ConvexRequest request, ConvexRequestDelegate next)` - Invokes the middleware

### ConvexRequest

Request wrapper for middleware pipeline.

**Namespace:** `Convex.Client.Shared.Middleware`

**Properties:**

- `string FunctionName { get; }` - Function name
- `string Method { get; }` - Request method
- `object? Args { get; }` - Request arguments
- `CancellationToken CancellationToken { get; }` - Cancellation token
- `TimeSpan? Timeout { get; set; }` - Timeout

### ConvexResponse

Response wrapper for middleware pipeline.

**Namespace:** `Convex.Client.Shared.Middleware`

**Properties:**

- `bool IsSuccess { get; }` - Whether response is successful
- `Exception? Error { get; }` - Error if failed

**Methods:**

- `static ConvexResponse Success<T>(T value)` - Creates success response
- `static ConvexResponse Failure(Exception error)` - Creates failure response
- `T GetValue<T>()` - Gets the response value

### ConvexRequestDelegate

Delegate type for middleware pipeline.

**Namespace:** `Convex.Client.Shared.Middleware`

**Signature:** `Task<ConvexResponse> ConvexRequestDelegate(ConvexRequest request)`

### IConvexInterceptor

Interface for observing and transforming Convex requests and responses.

**Namespace:** `Convex.Client.Shared.Interceptors`

**Methods:**

- `Task<ConvexRequestContext> BeforeRequestAsync(ConvexRequestContext context, CancellationToken cancellationToken = default)` - Called before request is sent
- `Task<ConvexResponseContext> AfterResponseAsync(ConvexResponseContext context, CancellationToken cancellationToken = default)` - Called after successful response
- `Task OnErrorAsync(ConvexErrorContext context, CancellationToken cancellationToken = default)` - Called when error occurs

### ConvexRequestContext

Context for request interception.

**Namespace:** `Convex.Client.Shared.Interceptors`

### ConvexResponseContext

Context for response interception.

**Namespace:** `Convex.Client.Shared.Interceptors`

### ConvexErrorContext

Context for error interception.

**Namespace:** `Convex.Client.Shared.Interceptors`

---

## Error Handling

### ConvexResult<T>

Represents the result of a Convex operation. This is an immutable record type with value equality semantics.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Type:** `record` (immutable)

**Properties:**

- `bool IsSuccess { get; }` - Whether operation succeeded
- `bool IsFailure { get; }` - Whether operation failed
- `T Value { get; }` - Success value (throws if failure)
- `ConvexError Error { get; }` - Error (throws if success)

**Note:** As a record type, `ConvexResult<T>` instances are immutable and use value-based equality. Two results with the same success state and value/error are considered equal.

**Static Methods:**

- `static ConvexResult<T> Success(T value)` - Creates successful result
- `static ConvexResult<T> Failure(ConvexError error)` - Creates failed result
- `static ConvexResult<T> Failure(Exception exception)` - Creates failed result from exception

**Methods:**

- `TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ConvexError, TResult> onFailure)` - Matches result
- `void Match(Action<T> onSuccess, Action<ConvexError> onFailure)` - Matches result with actions
- `ConvexResult<T> OnSuccess(Action<T> action)` - Executes action if successful
- `ConvexResult<T> OnFailure(Action<ConvexError> action)` - Executes action if failed
- `ConvexResult<TNew> Map<TNew>(Func<T, TNew> mapper)` - Maps success value
- `ConvexResult<TNew> Bind<TNew>(Func<T, ConvexResult<TNew>> binder)` - Binds to new result
- `T GetValueOrDefault(T defaultValue = default!)` - Gets value or default
- `T GetValueOrDefault(Func<ConvexError, T> defaultValueFactory)` - Gets value or default from factory

### ConvexError

Represents an error from a Convex operation.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Static Methods:**

- `static ConvexError FromException(Exception exception)` - Creates error from exception

### ConvexException

Base exception for all Convex-related errors.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `string? ErrorCode { get; set; }` - Convex-specific error code
- `JsonElement? ErrorData { get; set; }` - Additional error data
- `RequestContext? RequestContext { get; set; }` - Request context

### ConvexFunctionException

Exception thrown when a Convex function execution fails.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `string FunctionName { get; }` - Function name that failed

### ConvexArgumentException

Exception thrown when function arguments are invalid.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `string ArgumentName { get; }` - Invalid argument name

### ConvexNetworkException

Exception thrown when network-related errors occur.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `NetworkErrorType ErrorType { get; }` - Network error type
- `HttpStatusCode? StatusCode { get; set; }` - HTTP status code

### ConvexAuthenticationException

Exception thrown when authentication fails.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

### ConvexRateLimitException

Exception thrown when rate limits are exceeded.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `TimeSpan RetryAfter { get; }` - Time to wait before retrying
- `int CurrentLimit { get; }` - Current rate limit

### ConvexCircuitBreakerException

Exception thrown when a circuit breaker is open.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `CircuitBreakerState CircuitState { get; }` - Circuit breaker state

### NetworkErrorType

Enumeration of network error types.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Values:**

- `Timeout` - Request timeout
- `DnsResolution` - DNS resolution failure
- `SslCertificate` - SSL certificate error
- `ServerError` - General server error
- `ConnectionFailure` - Connection failure

### CircuitBreakerState

Enumeration of circuit breaker states.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Values:**

- `Closed` - Circuit is closed
- `Open` - Circuit is open
- `HalfOpen` - Circuit is half-open

### RequestContext

Context information for a request.

**Namespace:** `Convex.Client.Shared.ErrorHandling`

**Properties:**

- `string FunctionName { get; set; }` - Function name
- `string RequestType { get; set; }` - Request type
- `string RequestId { get; set; }` - Request ID
- `DateTimeOffset Timestamp { get; set; }` - Timestamp

---

## Connection Management

### ConnectionState

Represents the state of the WebSocket connection.

**Namespace:** `Convex.Client.Shared.Connection`

**Values:**

- `Disconnected` - Not connected
- `Connecting` - Currently connecting
- `Connected` - Connected and ready
- `Reconnecting` - Connection lost, attempting to reconnect
- `Failed` - Connection failed and not attempting to reconnect

### ReconnectionPolicy

Defines the reconnection strategy for WebSocket connections.

**Namespace:** `Convex.Client.Shared.Internal.Connection`

**Properties:**

- `int MaxAttempts { get; set; }` - Maximum reconnection attempts (-1 for unlimited)
- `TimeSpan BaseDelay { get; set; }` - Base delay for reconnection attempts
- `TimeSpan MaxDelay { get; set; }` - Maximum delay between attempts
- `bool UseExponentialBackoff { get; set; }` - Whether to use exponential backoff
- `bool UseJitter { get; set; }` - Whether to add jitter
- `int AttemptCount { get; }` - Current attempt count

**Static Methods:**

- `static ReconnectionPolicy Default()` - Creates default policy
- `static ReconnectionPolicy Unlimited()` - Creates unlimited policy
- `static ReconnectionPolicy None()` - Creates no reconnection policy

**Methods:**

- `bool ShouldRetry()` - Determines if reconnection should be attempted
- `TimeSpan GetNextDelay()` - Gets delay before next attempt
- `void Reset()` - Resets attempt counter

### ConnectionQuality

Represents the quality level of the connection.

**Namespace:** `Convex.Client.Shared.Quality`

**Values:**

- `Unknown` - Quality cannot be determined
- `Excellent` - Excellent connection quality
- `Good` - Good connection quality
- `Fair` - Fair connection quality
- `Poor` - Poor connection quality
- `Terrible` - Terrible connection quality

### ConnectionQualityInfo

Provides detailed information about connection quality.

**Namespace:** `Convex.Client.Shared.Quality`

**Properties:**

- `ConnectionQuality Quality { get; }` - Current quality level
- `string Description { get; }` - Human-readable description
- `DateTimeOffset Timestamp { get; }` - Assessment timestamp
- `double? AverageLatencyMs { get; }` - Average latency in milliseconds
- `double? LatencyVarianceMs { get; }` - Latency variance
- `double? PacketLossRate { get; }` - Packet loss rate percentage
- `int ReconnectionCount { get; }` - Number of reconnections
- `int ErrorCount { get; }` - Total number of errors
- `TimeSpan? TimeSinceLastMessage { get; }` - Time since last message
- `double UptimePercentage { get; }` - Connection uptime percentage
- `int QualityScore { get; }` - Quality score (0-100)
- `IReadOnlyDictionary<string, object> AdditionalData { get; }` - Additional diagnostic data

---

## Validation

### SchemaValidationOptions

Options for configuring schema validation behavior.

**Namespace:** `Convex.Client.Shared.Validation`

**Properties:**

- `bool ValidateOnQuery { get; set; }` - Whether to validate query responses
- `bool ValidateOnMutation { get; set; }` - Whether to validate mutation responses
- `bool ValidateOnAction { get; set; }` - Whether to validate action responses
- `bool ValidateOnSubscription { get; set; }` - Whether to validate subscription updates
- `bool ThrowOnValidationError { get; set; }` - Whether to throw on validation errors
- `bool StrictTypeChecking { get; set; }` - Whether to perform strict type checking

**Static Methods:**

- `static SchemaValidationOptions Strict()` - Creates strict validation options
- `static SchemaValidationOptions LogOnly()` - Creates log-only validation options

### ISchemaValidator

Interface for schema validation.

**Namespace:** `Convex.Client.Shared.Validation`

---

## Attributes

### ConvexFunctionAttribute

Base attribute for marking Convex functions.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `string FunctionName { get; }` - Convex function name
- `FunctionType FunctionType { get; }` - Function type

### ConvexQueryAttribute

Attribute for marking Convex query functions.

**Namespace:** `Convex.Client.Attributes`

### ConvexMutationAttribute

Attribute for marking Convex mutation functions.

**Namespace:** `Convex.Client.Attributes`

### ConvexActionAttribute

Attribute for marking Convex action functions.

**Namespace:** `Convex.Client.Attributes`

### FunctionType

Enumeration of Convex function types.

**Namespace:** `Convex.Client.Attributes`

**Values:**

- `Query` - Query function
- `Mutation` - Mutation function
- `Action` - Action function

### ConvexTableAttribute

Attribute for marking a class as a Convex table.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `string? TableName { get; set; }` - Table name override

### ConvexIndexAttribute

Attribute for marking a property as indexed.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `string? IndexName { get; set; }` - Index name
- `bool IsUnique { get; set; }` - Whether index is unique

### ConvexSearchIndexAttribute

Attribute for marking a property as searchable.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `string? IndexName { get; set; }` - Search index name

### ConvexForeignKeyAttribute

Attribute for marking a property as a foreign key.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `string TableName { get; }` - Referenced table name

### ConvexValidationAttribute

Attribute for specifying validation constraints.

**Namespace:** `Convex.Client.Attributes`

**Properties:**

- `double Min { get; set; }` - Minimum value
- `double Max { get; set; }` - Maximum value
- `int MinLength { get; set; }` - Minimum length
- `int MaxLength { get; set; }` - Maximum length

### ConvexIgnoreAttribute

Attribute for excluding a property from the Convex schema.

**Namespace:** `Convex.Client.Attributes`

---

## Shared Types

### IOptimisticLocalStore

View of query results for use within optimistic updates.

**Namespace:** `Convex.Client.Shared.OptimisticUpdates`

**Methods:**

- `TResult? GetQuery<TResult>(string queryName, object? args = null)` - Retrieves query result
- `IEnumerable<QueryResult<TResult, TArgs>> GetAllQueries<TResult, TArgs>(string queryName)` - Retrieves all queries with name
- `void SetQuery<TResult>(string queryName, TResult? value, object? args = null)` - Optimistically updates query result

### QueryResult<TResult, TArgs>

Represents a query result with its arguments. This is an immutable record type with value equality semantics.

**Namespace:** `Convex.Client.Shared.OptimisticUpdates`

**Type:** `record` (immutable)

**Properties:**

- `TArgs? Args { get; init; }` - Query arguments
- `TResult? Value { get; init; }` - Query result

**Note:** As a record type, `QueryResult<TResult, TArgs>` instances are immutable and use value-based equality. Two results with the same arguments and value are considered equal.

### IConvexSerializer

Interface for serializing Convex values.

**Namespace:** `Convex.Client.Shared.Serialization`

### IHttpClientProvider

Interface for providing HTTP clients.

**Namespace:** `Convex.Client.Shared.Http`

### TimestampManager

Manages timestamps for consistent reads.

**Namespace:** `Convex.Client.Shared.ConsistentQueries`

### ArgumentBuilder<TArgs>

Fluent builder for constructing function arguments with type safety and better IntelliSense support.

**Namespace:** `Convex.Client.Shared.ArgumentBuilders`

**Type Parameters:**

- `TArgs` - The type of arguments being built (must be a class with parameterless constructor)

**Constructors:**

- `ArgumentBuilder()` - Creates a new argument builder
- `ArgumentBuilder(TArgs args)` - Creates a builder initialized with existing arguments

**Methods:**

- `ArgumentBuilder<TArgs> Set(Action<TArgs> setter)` - Sets a property value using a fluent API
- `TArgs Build()` - Builds and returns the configured arguments

**Static Factory Methods:**

- `static ArgumentBuilder<TArgs> Create<TArgs>()` - Creates a new argument builder
- `static ArgumentBuilder<TArgs> From<TArgs>(TArgs args)` - Creates a builder from existing arguments

**Implicit Conversion:**

- `implicit operator TArgs(ArgumentBuilder<TArgs> builder)` - Allows using builder directly where arguments are expected

**Example:**

```csharp
using Convex.Client.Shared.ArgumentBuilders;

// Create arguments with fluent API
var args = ArgumentBuilder.Create<GetMessagesArgs>()
    .Set(a => a.RoomId = "room-1")
    .Set(a => a.Limit = 50)
    .Build();

// Or use implicit conversion
var args2 = ArgumentBuilder.Create<GetMessagesArgs>()
    .Set(a => a.RoomId = "room-1");

// Use with query builder
var messages = await client.Query<List<Message>>("messages:get")
    .WithArgs(args)
    .ExecuteAsync();
```

---

## Common Patterns

This section provides practical examples and common usage patterns for the Convex .NET client.

### Basic Query Pattern

```csharp
// Simple query without arguments
var todos = await client.Query<List<Todo>>("todos:list")
    .ExecuteAsync();

// Query with arguments (anonymous object)
var user = await client.Query<User>("users:get")
    .WithArgs(new { userId = "123" })
    .ExecuteAsync();

// Query with arguments (using ArgumentBuilder for better IntelliSense)
using Convex.Client.Shared.ArgumentBuilders;

var args = ArgumentBuilder.Create<GetMessagesArgs>()
    .Set(a => a.RoomId = "room-1")
    .Set(a => a.Limit = 50);

var messages = await client.Query<List<Message>>("messages:get")
    .WithArgs(args)
    .ExecuteAsync();

// Query with timeout and error handling
var result = await client.Query<List<Message>>("messages:list")
    .WithArgs(new { roomId = "room-1" })
    .WithTimeout(TimeSpan.FromSeconds(5))
    .OnError(ex => Console.WriteLine($"Error: {ex.Message}"))
    .ExecuteAsync();
```

### Using Result Types for Error Handling

```csharp
// Using ExecuteWithResultAsync for functional error handling
var result = await client.Query<List<Todo>>("todos:list")
    .ExecuteWithResultAsync();

result.Match(
    onSuccess: todos => Console.WriteLine($"Found {todos.Count} todos"),
    onFailure: error => Console.WriteLine($"Error: {error.Message}")
);

// Using extension methods for common patterns
var todos = await client.Query<List<Todo>>("todos:list")
    .ExecuteWithResultAsync()
    .GetValueOrDefault(new List<Todo>());

// Unwrap with fallback
var user = await client.Query<User>("users:get")
    .WithArgs(new { userId = "123" })
    .ExecuteWithResultAsync()
    .OrElse(new User { Name = "Guest" });
```

### Mutation with Optimistic Updates

```csharp
// Simple mutation (anonymous object)
var newTodo = await client.Mutate<Todo>("todos:create")
    .WithArgs(new { text = "Buy milk" })
    .ExecuteAsync();

// Mutation with ArgumentBuilder
using Convex.Client.Shared.ArgumentBuilders;

var createArgs = ArgumentBuilder.Create<CreateTodoArgs>()
    .Set(a => a.Text = "Buy milk")
    .Set(a => a.UserId = "user-123");

var newTodo = await client.Mutate<Todo>("todos:create")
    .WithArgs(createArgs)
    .ExecuteAsync();

// Mutation with optimistic update and auto-rollback
await client.Mutate<Todo>("todos:create")
    .WithArgs(new { text = "Buy milk" })
    .OptimisticWithAutoRollback(
        getter: () => _todos,
        setter: value => _todos = value,
        update: todos => todos.Append(newTodo).ToList()
    )
    .ExecuteAsync();

// Mutation with query-focused optimistic update
await client.Mutate<Message>("messages:send")
    .WithArgs(new { text = "Hello!" })
    .WithOptimisticUpdate((localStore, args) =>
    {
        var currentMessages = localStore.GetQuery<List<Message>>("messages:list") ?? new List<Message>();
        var newMessage = new Message { Text = args.text, Id = Guid.NewGuid().ToString() };
        localStore.SetQuery("messages:list", currentMessages.Append(newMessage).ToList());
    })
    .ExecuteAsync();
```

### Real-Time Subscriptions

```csharp
// Basic subscription
var subscription = client.Observe<List<Todo>>("todos:list")
    .Subscribe(todos => UpdateUI(todos));

// Subscription with error handling
var subscription = client.Observe<List<Message>>("messages:list")
    .Subscribe(
        onNext: messages => UpdateUI(messages),
        onError: error => Console.WriteLine($"Subscription error: {error.Message}")
    );

// Using extension methods for async/await patterns
await foreach (var todos in client.Observe<List<Todo>>("todos:list").ToAsyncEnumerable())
{
    UpdateUI(todos);
}

// Get first value without maintaining subscription
var initialTodos = await client.Observe<List<Todo>>("todos:list")
    .SubscribeAsync();

// Only emit when value actually changes
client.Observe<List<Todo>>("todos:list")
    .DistinctUntilChanged()
    .Subscribe(todos => UpdateUI(todos));
```

### Batch Queries

```csharp
// Execute multiple queries in parallel
var (todos, user, stats) = await client.Batch()
    .Query<List<Todo>>("todos:list")
    .Query<User>("users:current")
    .Query<Stats>("dashboard:stats")
    .ExecuteAsync<List<Todo>, User, Stats>();

// Dynamic number of queries (array)
var results = await client.Batch()
    .Query<List<Todo>>("todos:list")
    .Query<User>("users:current")
    .Query<Stats>("dashboard:stats")
    .ExecuteAsync();

var todos = (List<Todo>)results[0];
var user = (User)results[1];
var stats = (Stats)results[2];

// Named results using dictionary (better for many queries)
var resultsDict = await client.Batch()
    .Query<List<Todo>>("todos:list")
    .Query<User>("users:current")
    .Query<Stats>("dashboard:stats")
    .ExecuteAsDictionaryAsync();

var todos = (List<Todo>)resultsDict["todos:list"];
var user = (User)resultsDict["users:current"];
var stats = (Stats)resultsDict["dashboard:stats"];
```

### Error Handling Best Practices

```csharp
// Pattern 1: Using Result types (recommended)
var result = await client.Query<List<Todo>>("todos:list")
    .ExecuteWithResultAsync();

if (result.IsSuccess)
{
    ProcessTodos(result.Value);
}
else
{
    HandleError(result.Error);
}

// Pattern 2: Using try/catch with detailed error messages
try
{
    var todos = await client.Query<List<Todo>>("todos:list")
        .ExecuteAsync();
    ProcessTodos(todos);
}
catch (ConvexException ex)
{
    // Access detailed error information
    Console.WriteLine(ex.GetDetailedMessage());
    if (ex.ErrorDetails != null)
    {
        foreach (var suggestion in ex.ErrorDetails.Suggestions)
        {
            Console.WriteLine($"  • {suggestion}");
        }
    }
}
```

### Pagination Pattern

```csharp
// Create paginated query
var paginator = client.PaginationSlice
    .Query<Message>("messages:list")
    .WithPageSize(25)
    .WithArgs(new { roomId = "room-1" })
    .Build();

// Load initial page
await paginator.LoadNextAsync();

// Load more pages
while (paginator.HasMore)
{
    await paginator.LoadNextAsync();
}

// Use with async enumerable
await foreach (var message in paginator.AsAsyncEnumerable())
{
    ProcessMessage(message);
}
```

### Client Configuration

```csharp
// Using builder pattern
var client = new ConvexClientBuilder()
    .UseDeployment("https://your-deployment.convex.cloud")
    .WithAutoReconnect(maxAttempts: 5)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithRequestLogging()
    .WithDevelopmentDefaults() // Or WithProductionDefaults()
    .Build();

// Monitor connection state
client.ConnectionStateChanges
    .Subscribe(state => Console.WriteLine($"Connection: {state}"));

// Monitor connection quality
client.ConnectionQualityChanges
    .Subscribe(quality => Console.WriteLine($"Quality: {quality}"));
```

### Caching and Invalidation

```csharp
// Define automatic cache invalidation
client.DefineQueryDependency(
    "todos:create",
    "todos:list",
    "todos:count",
    "dashboard:stats"
);

// Manual cache invalidation
await client.InvalidateQueryAsync("todos:list");
await client.InvalidateQueriesAsync("todos:*");

// Use cached values from active subscriptions
if (client.TryGetCachedValue<List<Todo>>("todos:list", out var cachedTodos))
{
    DisplayTodos(cachedTodos);
}
```

### Quick Reference

**Common Operations:**

- **Query**: `client.Query<TResult>(functionName).WithArgs(args).ExecuteAsync()`
- **Mutation**: `client.Mutate<TResult>(functionName).WithArgs(args).ExecuteAsync()`
- **Action**: `client.Action<TResult>(functionName).WithArgs(args).ExecuteAsync()`
- **Subscribe**: `client.Observe<T>(functionName).Subscribe(onNext)`
- **Batch**: `client.Batch().Query<T>(name).Query<T2>(name2).ExecuteAsync<T, T2>()`
- **Result Type**: `.ExecuteWithResultAsync()` returns `ConvexResult<T>`

**Best Practices:**

1. Use `ExecuteWithResultAsync()` for functional error handling
2. Always dispose subscriptions when done
3. Use `DistinctUntilChanged()` to avoid unnecessary UI updates
4. Define query dependencies for automatic cache invalidation
5. Use optimistic updates for better UX
6. Monitor connection state and quality for adaptive behavior

---

## Summary

This API specification covers all public interfaces, classes, enums, and exceptions in the Convex .NET client library. The library provides:

- **Core Client**: Main interface for interacting with Convex backend
- **Queries**: Read-only operations with caching and batching support
- **Mutations**: State-changing operations with optimistic updates
- **Actions**: Side-effect operations
- **Subscriptions**: Real-time observable streams
- **Pagination**: Cursor-based pagination for large datasets
- **Caching**: In-memory caching with optimistic updates
- **Authentication**: Token-based authentication management
- **File Storage**: Upload, download, and file management
- **Vector Search**: AI-powered similarity search
- **Scheduling**: Delayed and recurring function execution
- **HTTP Actions**: REST API endpoints
- **Health & Diagnostics**: Connection health and performance tracking
- **Resilience**: Retry policies and circuit breakers
- **Middleware & Interceptors**: Request/response pipeline customization
- **Error Handling**: Type-safe error handling with Result types
- **Connection Management**: WebSocket connection state and quality monitoring
- **Validation**: Schema validation for responses
- **Attributes**: Code generation and schema definition attributes
