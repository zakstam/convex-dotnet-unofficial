using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Convex.Client.Shared.Internal.Connection;
using Convex.Client.Shared.Internal.Threading;
using Convex.Client.Shared.Connection;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Http;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Shared.Internal.WebSocket;

/// <summary>
/// WebSocket client for real-time communication with Convex backend.
/// Manages connection, subscriptions, and message routing.
/// </summary>
/// <remarks>
/// Creates a new WebSocket client.
/// </remarks>
/// <param name="deploymentUrl">The Convex deployment URL.</param>
/// <param name="syncContext">Optional SynchronizationContext for UI thread marshalling.</param>
/// <param name="reconnectionPolicy">Optional reconnection policy. Defaults to 5 attempts with exponential backoff.</param>
/// <param name="logger">Optional logger for structured logging.</param>
internal sealed class ConvexWebSocketClient(
    string deploymentUrl,
    SyncContextCapture? syncContext = null,
    ReconnectionPolicy? reconnectionPolicy = null,
    ILogger? logger = null) : IDisposable
{
    /// <summary>
    /// Tracks subscription metadata needed for reconnection.
    /// </summary>
    private sealed class SubscriptionInfo
    {
        public required Channel<object> Channel { get; init; }
        public required string FunctionName { get; init; }
        public object? Args { get; init; }
    }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new ConvexInt64JsonConverter() }
    };

    private readonly string _deploymentUrl = deploymentUrl;
    private ClientWebSocket _webSocket = new ClientWebSocket();
    private readonly ConcurrentDictionary<string, SubscriptionInfo> _subscriptions = new ConcurrentDictionary<string, SubscriptionInfo>();
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private readonly ReconnectionPolicy _reconnectionPolicy = reconnectionPolicy ?? ReconnectionPolicy.Default();
    private readonly SyncContextCapture _syncContext = syncContext ?? new SyncContextCapture();
    private readonly ILogger? _logger = logger;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _isDisposed;
    private int _nextSubscriptionId = 0;
    private int _connectionCount = 0;
    private string? _lastCloseReason = "InitialConnect";
    private readonly string _sessionId = Guid.NewGuid().ToString();
    private int _querySetVersion = 0;
    private int _identityVersion; // Tracks authentication version (incremented on each auth change)
    private Func<CancellationToken, Task<string?>>? _authTokenProvider;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

    /// <summary>
    /// Occurs when the connection state changes.
    /// Events are automatically marshalled to the UI thread.
    /// </summary>
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// Sets the authentication token provider function.
    /// This is called by ConvexClient to wire up the Authentication slice.
    /// </summary>
    /// <param name="authTokenProvider">Function that retrieves the authentication token.</param>
    public void SetAuthTokenProvider(Func<CancellationToken, Task<string?>> authTokenProvider) => _authTokenProvider = authTokenProvider;

    /// <summary>
    /// Connects to the WebSocket server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            return; // Already connected
        }

        // If WebSocket is not in None state, we need a new instance for reconnection
        if (_webSocket.State != WebSocketState.None)
        {
            _webSocket.Dispose();
            _webSocket = new ClientWebSocket();
        }

        UpdateConnectionState(ConnectionState.Connecting);

        try
        {
            // Convert HTTP(S) URL to WS(S) URL
            // Convex WebSocket endpoint format: wss://deployment.convex.cloud/api/{version}/sync
            // See: convex-js/src/browser/sync/client.ts:314
            const string ConvexProtocolVersion = "1.27.3"; // Match convex-js version from submodule
            var wsUri = new Uri(_deploymentUrl.Replace("https://", "wss://").Replace("http://", "ws://") + $"/api/{ConvexProtocolVersion}/sync");

            await _webSocket.ConnectAsync(wsUri, cancellationToken);

            // Increment connection count
            _connectionCount++;

            // Reset query set version on reconnection (server will sync its state)
            // This ensures protocol version synchronization after reconnection
            // Must be done BEFORE sending Connect message so it uses the correct baseVersion
            _querySetVersion = 0;

            // Send Connect handshake message
            // See: convex-js/src/browser/sync/client.ts:395-403
            await SendConnectMessageAsync();

            // Send Authenticate message if auth token is available
            // See: convex-js/src/browser/sync/local_state.ts:192-206
            await SendAuthenticateMessageAsync(cancellationToken);

            // Start receiving messages
            _receiveCts = new CancellationTokenSource();
            _receiveTask = ReceiveMessagesAsync(_receiveCts.Token);

            UpdateConnectionState(ConnectionState.Connected);

            // Reset reconnection policy on successful connection
            _reconnectionPolicy.Reset();

            // Update last close reason for next reconnection
            _lastCloseReason = null;

            // Re-establish all active subscriptions after reconnection
            await ResubscribeAllAsync();

        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to WebSocket at {Url}: {Message}", _deploymentUrl, ex.Message);
            UpdateConnectionState(ConnectionState.Disconnected);
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the WebSocket server.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State != WebSocketState.Open && _webSocket.State != WebSocketState.CloseReceived)
        {
            UpdateConnectionState(ConnectionState.Disconnected);
            return; // Already disconnected
        }

        try
        {
            // Cancel the receive loop
            _receiveCts?.Cancel();

            // Close the WebSocket connection gracefully
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", cancellationToken);

            // Wait for receive task to complete
            if (_receiveTask != null)
            {
                await _receiveTask;
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - we're already disconnecting
            _logger?.LogWarning(ex,
                "Exception during WebSocket disconnection from {Url} (safe to ignore during cleanup): {Message}",
                _deploymentUrl, ex.Message);
        }
        finally
        {
            UpdateConnectionState(ConnectionState.Disconnected);

            // Mark channels as completed but preserve subscription metadata for reconnection
            // This allows us to re-establish subscriptions when reconnecting
            foreach (var subscription in _subscriptions.Values)
            {
                // Complete the old channel - consumers will need to handle this
                subscription.Channel.Writer.Complete();
            }
            // Note: We don't clear _subscriptions here - metadata is preserved for reconnection
            // Channels will be recreated in ResubscribeAllAsync when reconnecting
        }
    }

    /// <summary>
    /// Creates a live query subscription.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the query.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <returns>An async enumerable that yields values as they arrive.</returns>
    public async IAsyncEnumerable<T> LiveQuery<T>(string functionName)
    {
        await EnsureConnectedAsync();

        // Generate unique subscription ID
        var subscriptionId = Interlocked.Increment(ref _nextSubscriptionId).ToString();

        // Create channel for this subscription
        var channel = Channel.CreateUnbounded<object>();
        var subscriptionInfo = new SubscriptionInfo
        {
            Channel = channel,
            FunctionName = functionName,
            Args = null
        };
        _subscriptions[subscriptionId] = subscriptionInfo;

        try
        {
            // Send subscription request to server
            await SendSubscriptionRequestAsync(subscriptionId, functionName, null);

            // Yield values as they arrive
            await foreach (var value in subscriptionInfo.Channel.Reader.ReadAllAsync())
            {
                // Deserialize from raw JSON string to type T
                if (value is string json)
                {
                    var typedValue = JsonSerializer.Deserialize<T>(json, JsonOptions);
                    if (typedValue != null)
                    {
                        yield return typedValue;
                    }
                }
            }
        }
        finally
        {
            // Cleanup subscription
            _ = _subscriptions.TryRemove(subscriptionId, out _);
            await SendUnsubscribeRequestAsync(subscriptionId);
        }
    }

    /// <summary>
    /// Creates a live query subscription with arguments.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the query.</typeparam>
    /// <typeparam name="TArgs">The type of arguments.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>An async enumerable that yields values as they arrive.</returns>
    public async IAsyncEnumerable<T> LiveQuery<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        await EnsureConnectedAsync();

        var subscriptionId = Interlocked.Increment(ref _nextSubscriptionId).ToString();
        var channel = Channel.CreateUnbounded<object>();
        var subscriptionInfo = new SubscriptionInfo
        {
            Channel = channel,
            FunctionName = functionName,
            Args = args
        };
        _subscriptions[subscriptionId] = subscriptionInfo;

        try
        {
            await SendSubscriptionRequestAsync(subscriptionId, functionName, args);

            await foreach (var value in subscriptionInfo.Channel.Reader.ReadAllAsync())
            {
                // Deserialize from raw JSON string to type T
                if (value is string json)
                {
                    var typedValue = JsonSerializer.Deserialize<T>(json, JsonOptions);
                    if (typedValue != null)
                    {
                        yield return typedValue;
                    }
                }
            }
        }
        finally
        {
            _ = _subscriptions.TryRemove(subscriptionId, out _);
            await SendUnsubscribeRequestAsync(subscriptionId);
        }
    }

    /// <summary>
    /// Gets a cached value from an active subscription, if available.
    /// </summary>
    public T? LocalQueryResult<T>(string functionName) where T : class =>
        // TODO: Implement cached value retrieval
        // For now, return null - this will be implemented when we add caching layer
        null;

    /// <summary>
    /// Ensures the WebSocket is connected, connecting if necessary.
    /// </summary>
    private async Task EnsureConnectedAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            return; // Already connected
        }

        if (ConnectionState == ConnectionState.Connecting)
        {
            // Wait for ongoing connection with timeout
            var timeout = TimeSpan.FromSeconds(30);
            var elapsed = TimeSpan.Zero;
            var pollInterval = TimeSpan.FromMilliseconds(50);

            while (ConnectionState == ConnectionState.Connecting && elapsed < timeout)
            {
                await Task.Delay(pollInterval);
                elapsed += pollInterval;
            }

            // If still connecting after timeout, throw
            if (ConnectionState == ConnectionState.Connecting)
            {
                throw new TimeoutException("Connection attempt timed out after 30 seconds.");
            }

            return;
        }

        // Connect if disconnected
        await ConnectAsync();
    }

    /// <summary>
    /// Sends the Connect handshake message to the server.
    /// See: convex-js/src/browser/sync/protocol.ts:118-125
    /// </summary>
    private async Task SendConnectMessageAsync()
    {
        var connectMessage = new ConnectMessage
        {
            SessionId = _sessionId,
            ConnectionCount = _connectionCount,
            LastCloseReason = _lastCloseReason,
            MaxObservedTimestamp = null, // TODO: Implement resumption with maxObservedTimestamp
            ClientTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            BaseVersion = _querySetVersion // Query set version (not identity version)
        };

        var messageBytes = ConvexWebSocketProtocol.EncodeClientMessage(connectMessage);
        await SendMessageBytesAsync(messageBytes);
    }

    /// <summary>
    /// Sends an Authenticate message to the server with the current auth token.
    /// See: convex-js/src/browser/sync/local_state.ts:192-206
    /// </summary>
    private async Task SendAuthenticateMessageAsync(CancellationToken cancellationToken = default)
    {
        // Get auth token if provider is available
        string? authToken = null;
        if (_authTokenProvider != null)
        {
            try
            {
                authToken = await _authTokenProvider(cancellationToken);
                if (!string.IsNullOrEmpty(authToken))
                {
                    _logger?.LogDebug("Sending Authenticate message (token length: {TokenLength})", authToken.Length);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get auth token: {Message}", ex.Message);
                // Continue without auth - server will return auth errors if operations require it
                return;
            }
        }

        if (string.IsNullOrEmpty(authToken))
        {
            _logger?.LogDebug("No auth token available, skipping Authenticate message");
            return;
        }

        var baseVersion = _identityVersion;
        _identityVersion++; // Increment identity version after sending auth

        var authenticateMessage = new AuthenticateMessage
        {
            TokenType = "User",
            Value = authToken,
            BaseVersion = baseVersion
        };

        var messageBytes = ConvexWebSocketProtocol.EncodeClientMessage(authenticateMessage);
        await SendMessageBytesAsync(messageBytes);
    }

    /// <summary>
    /// Sends a subscription request to the server using ModifyQuerySet protocol.
    /// See: convex-js/src/browser/sync/protocol.ts:144-149
    /// </summary>
    private async Task SendSubscriptionRequestAsync(string subscriptionId, string functionName, object? args)
    {
        // Increment query set version
        var baseVersion = _querySetVersion;
        var newVersion = ++_querySetVersion;

        // Create ModifyQuerySet message with Add query modification
        var message = new ModifyQuerySetMessage
        {
            BaseVersion = baseVersion,
            NewVersion = newVersion,
            Modifications = new[]
            {
                new QuerySetModification
                {
                    Type = "Add",
                    QueryId = int.Parse(subscriptionId), // Convert string ID to int for Convex protocol
                    UdfPath = functionName,
                    Args = new[] { args ?? new Dictionary<string, object>() } // Always wrap in array, use empty dictionary if null (Convex compatibility)
                }
            }
        };

        var messageBytes = ConvexWebSocketProtocol.EncodeClientMessage(message);
        await SendMessageBytesAsync(messageBytes);
    }

    /// <summary>
    /// Sends an unsubscribe request to the server using ModifyQuerySet protocol.
    /// See: convex-js/src/browser/sync/protocol.ts:139-142
    /// </summary>
    private async Task SendUnsubscribeRequestAsync(string subscriptionId)
    {
        // Increment query set version
        var baseVersion = _querySetVersion;
        var newVersion = ++_querySetVersion;

        // Create ModifyQuerySet message with Remove query modification
        var message = new ModifyQuerySetMessage
        {
            BaseVersion = baseVersion,
            NewVersion = newVersion,
            Modifications = new[]
            {
                new QuerySetModification
                {
                    Type = "Remove",
                    QueryId = int.Parse(subscriptionId)
                }
            }
        };

        var messageBytes = ConvexWebSocketProtocol.EncodeClientMessage(message);
        await SendMessageBytesAsync(messageBytes);
    }

    /// <summary>
    /// Sends a JSON message over the WebSocket.
    /// </summary>
    private async Task SendMessageAsync(object message)
    {
        // Guard against disposed state
        if (_isDisposed || _webSocket.State != WebSocketState.Open)
        {
            // Silently ignore - WebSocket is already closed/disposed
            return;
        }

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendMessageBytesAsync(bytes);
    }

    /// <summary>
    /// Sends message bytes over the WebSocket.
    /// </summary>
    private async Task SendMessageBytesAsync(ReadOnlyMemory<byte> bytes)
    {
        // Guard against disposed state
        if (_isDisposed || _webSocket.State != WebSocketState.Open)
        {
            // Silently ignore - WebSocket is already closed/disposed
            return;
        }

        await _sendLock.WaitAsync();
        try
        {
            // Double-check state after acquiring lock
            if (_isDisposed || _webSocket.State != WebSocketState.Open)
            {
                return;
            }

            await _webSocket.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);

        }
        catch (ObjectDisposedException)
        {
            // WebSocket was disposed during send - ignore gracefully
        }
        catch (WebSocketException)
        {
            // WebSocket connection was closed - ignore gracefully
        }
        finally
        {
            _ = _sendLock.Release();
        }
    }

    /// <summary>
    /// Receives messages from the WebSocket and routes them to subscriptions.
    /// Automatically reconnects on disconnection with exponential backoff.
    /// </summary>
    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        // Use List with pre-sized capacity to reduce allocations
        // Typical messages are a few KB, so start with reasonable capacity
        var messageBuffer = new List<byte>(capacity: 4096);

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _lastCloseReason = "ServerClosed";
                    UpdateConnectionState(ConnectionState.Disconnected);

                    // Attempt automatic reconnection
                    await TryReconnectAsync(cancellationToken);
                    break;
                }

                // Efficiently append received bytes using AddRange
                // This is O(n) instead of O(n²) from individual Add() calls
                if (result.Count > 0)
                {
                    // Create a span view of just the received bytes
                    var receivedSpan = buffer.AsSpan(0, result.Count);

                    // AddRange from array is more efficient than byte-by-byte
                    messageBuffer.AddRange(receivedSpan.ToArray());
                }

                if (result.EndOfMessage)
                {
                    var json = Encoding.UTF8.GetString([.. messageBuffer]);
                    messageBuffer.Clear();

                    await ProcessMessageAsync(json);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation - don't reconnect
        }
        catch (Exception ex)
        {
            _lastCloseReason = $"Exception:{ex.GetType().Name}";
            UpdateConnectionState(ConnectionState.Disconnected);

            // Attempt automatic reconnection
            if (!cancellationToken.IsCancellationRequested)
            {
                await TryReconnectAsync(cancellationToken);
            }
        }

    }

    /// <summary>
    /// Re-establishes all active subscriptions after reconnection.
    /// This ensures subscriptions continue to work after network interruptions.
    /// </summary>
    private async Task ResubscribeAllAsync()
    {
        if (_subscriptions.IsEmpty)
        {
            return; // No subscriptions to re-establish
        }

        _logger?.LogInformation("Re-establishing {Count} subscription(s) after reconnection", _subscriptions.Count);

        // Create a snapshot of subscriptions to avoid modification during iteration
        var subscriptionsToResubscribe = _subscriptions.ToArray();

        foreach (var kvp in subscriptionsToResubscribe)
        {
            var subscriptionId = kvp.Key;
            var subscriptionInfo = kvp.Value;

            try
            {
                // Recreate the channel if it was completed during disconnection
                // Check if channel is completed by trying to write (this is a best-effort check)
                var needsNewChannel = subscriptionInfo.Channel.Reader.Completion.IsCompleted;

                if (needsNewChannel)
                {
                    // Create a new channel to replace the completed one
                    var newChannel = Channel.CreateUnbounded<object>();
                    var updatedInfo = new SubscriptionInfo
                    {
                        Channel = newChannel,
                        FunctionName = subscriptionInfo.FunctionName,
                        Args = subscriptionInfo.Args
                    };
                    _subscriptions[subscriptionId] = updatedInfo;
                    subscriptionInfo = updatedInfo;
                }

                // Re-send subscription request to server
                await SendSubscriptionRequestAsync(subscriptionId, subscriptionInfo.FunctionName, subscriptionInfo.Args);
                _logger?.LogDebug("Re-established subscription {SubscriptionId} for function {FunctionName}", subscriptionId, subscriptionInfo.FunctionName);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to re-establish subscription {SubscriptionId} for function {FunctionName}: {Message}",
                    subscriptionId, subscriptionInfo.FunctionName, ex.Message);
                // Continue with other subscriptions even if one fails
            }
        }
    }

    /// <summary>
    /// Attempts to reconnect with exponential backoff according to the reconnection policy.
    /// </summary>
    private async Task TryReconnectAsync(CancellationToken cancellationToken)
    {
        if (!_reconnectionPolicy.ShouldRetry())
        {
            return;
        }

        var delay = _reconnectionPolicy.GetNextDelay();

        UpdateConnectionState(ConnectionState.Reconnecting);

        try
        {
            await Task.Delay(delay, cancellationToken);
            await ConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log reconnection failure
            _logger?.LogWarning(ex,
                "Reconnection attempt {Attempt} to {Url} failed after {Delay}ms: {Message}. Will retry if policy allows.",
                _reconnectionPolicy.AttemptCount, _deploymentUrl, delay.TotalMilliseconds, ex.Message);

            // ConnectAsync already updated state to Disconnected

            // Try again if policy allows
            if (!cancellationToken.IsCancellationRequested)
            {
                await TryReconnectAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes a received message and routes it to the appropriate subscription.
    /// Handles Transition messages from Convex protocol.
    /// See: convex-js/src/browser/sync/protocol.ts:315-328
    /// </summary>
    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Check for message type
            if (!root.TryGetProperty("type", out var typeElement))
            {
                _logger?.LogWarning("WebSocket message from {Url} missing 'type' property, ignoring", _deploymentUrl);
                return;
            }

            var messageType = typeElement.GetString();
            if (string.IsNullOrEmpty(messageType))
            {
                _logger?.LogWarning("WebSocket message from {Url} has null/empty type, ignoring", _deploymentUrl);
                return;
            }

            _logger?.LogDebug("[WebSocket] Received message type: {MessageType}", messageType);
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Received message type: {messageType}");
            System.Console.WriteLine($"[WebSocket] Received message type: {messageType}");

            switch (messageType)
            {
                case "Transition":
                    // Handle Transition message with query modifications
                    _logger?.LogDebug("[WebSocket] Processing Transition message");
                    System.Diagnostics.Debug.WriteLine("[WebSocket] Processing Transition message");
                    System.Console.WriteLine("[WebSocket] Processing Transition message");

                    if (root.TryGetProperty("modifications", out var modificationsElement))
                    {
                        var modCount = 0;
                        foreach (var modification in modificationsElement.EnumerateArray())
                        {
                            modCount++;

                            // Log the modification structure to debug
                            var modJson = modification.GetRawText();
                            _logger?.LogDebug("[WebSocket] Modification {Index}: {Json}", modCount, modJson);
                            System.Diagnostics.Debug.WriteLine($"[WebSocket] Modification {modCount}: {modJson}");
                            System.Console.WriteLine($"[WebSocket] Modification {modCount}: {modJson}");

                            var hasQueryId = modification.TryGetProperty("queryId", out var queryIdElement);
                            var hasValue = modification.TryGetProperty("value", out var valueElement);

                            _logger?.LogDebug("[WebSocket] hasQueryId: {HasQueryId}, hasValue: {HasValue}", hasQueryId, hasValue);
                            System.Diagnostics.Debug.WriteLine($"[WebSocket] hasQueryId: {hasQueryId}, hasValue: {hasValue}");
                            System.Console.WriteLine($"[WebSocket] hasQueryId: {hasQueryId}, hasValue: {hasValue}");

                            if (hasQueryId && hasValue)
                            {
                                var queryId = queryIdElement.GetInt32().ToString();

                                _logger?.LogDebug("[WebSocket] Transition modification for queryId: {QueryId}", queryId);
                                System.Diagnostics.Debug.WriteLine($"[WebSocket] Transition modification for queryId: {queryId}");
                                System.Console.WriteLine($"[WebSocket] Transition modification for queryId: {queryId}");

                                if (_subscriptions.TryGetValue(queryId, out var subscriptionInfo))
                                {
                                    // Store the raw JSON string so LiveQuery can deserialize it to the correct type T
                                    var rawJson = valueElement.GetRawText();
                                    _logger?.LogDebug("[WebSocket] Writing to channel for queryId: {QueryId}", queryId);
                                    System.Diagnostics.Debug.WriteLine($"[WebSocket] Writing to channel for queryId: {queryId}");
                                    System.Console.WriteLine($"[WebSocket] Writing to channel for queryId: {queryId}");
                                    await subscriptionInfo.Channel.Writer.WriteAsync(rawJson);
                                }
                                else
                                {
                                    _logger?.LogWarning("[WebSocket] No subscription found for queryId: {QueryId}", queryId);
                                    System.Diagnostics.Debug.WriteLine($"[WebSocket] No subscription found for queryId: {queryId}");
                                    System.Console.WriteLine($"[WebSocket] No subscription found for queryId: {queryId}");
                                }
                            }
                            else
                            {
                                _logger?.LogWarning("[WebSocket] Modification missing queryId or value");
                                System.Diagnostics.Debug.WriteLine("[WebSocket] Modification missing queryId or value");
                                System.Console.WriteLine("[WebSocket] Modification missing queryId or value");
                            }
                        }
                        _logger?.LogDebug("[WebSocket] Processed {ModCount} modifications", modCount);
                        System.Diagnostics.Debug.WriteLine($"[WebSocket] Processed {modCount} modifications");
                        System.Console.WriteLine($"[WebSocket] Processed {modCount} modifications");
                    }
                    else
                    {
                        _logger?.LogWarning("[WebSocket] Transition message missing modifications");
                        System.Diagnostics.Debug.WriteLine("[WebSocket] Transition message missing modifications");
                        System.Console.WriteLine("[WebSocket] Transition message missing modifications");
                    }
                    break;

                case "FatalError":
                case "AuthError":
                    // Handle error messages - disconnect and complete all subscriptions
                    if (root.TryGetProperty("error", out var errorElement))
                    {
                        var errorMessage = errorElement.GetString() ?? "Unknown error";
                        _lastCloseReason = $"{messageType}:{errorMessage}";

                        // Log error with helpful guidance
                        var guidance = GetAuthErrorGuidance(messageType, errorMessage);
                        _logger?.LogError("[WebSocket] {MessageType}: {Error}\n{Guidance}", messageType, errorMessage, guidance);
                        System.Diagnostics.Debug.WriteLine($"[WebSocket] {messageType}: {errorMessage}\n{guidance}");
                        System.Console.WriteLine($"[WebSocket] {messageType}: {errorMessage}\n{guidance}");

                        // Complete all subscription channels with error
                        foreach (var subscriptionInfo in _subscriptions.Values)
                        {
                            subscriptionInfo.Channel.Writer.Complete(new InvalidOperationException($"{messageType}: {errorMessage}"));
                        }
                        _subscriptions.Clear();

                        // Disconnect without reconnection for auth errors
                        UpdateConnectionState(ConnectionState.Disconnected);
                    }
                    break;

                case "Ping":
                    // TODO: Handle ping/pong for keepalive
                    break;

                default:
                    _logger?.LogWarning("Unknown WebSocket message type '{MessageType}' received from {Url}, ignoring",
                        messageType, _deploymentUrl);
                    break;
            }
        }
        catch (JsonException ex)
        {
            // Invalid JSON received - log and ignore this message but continue receiving
            _logger?.LogWarning(ex,
                "Invalid JSON received from WebSocket at {Url}: {Message}. Message will be skipped.",
                _deploymentUrl, ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected error processing message - log and ignore but continue receiving
            // Connection will be handled by ReceiveMessagesAsync if WebSocket fails
            _logger?.LogWarning(ex,
                "Unexpected error processing WebSocket message from {Url}: {Message}. Message will be skipped.",
                _deploymentUrl, ex.Message);
        }
    }

    /// <summary>
    /// Provides helpful guidance for authentication and fatal errors.
    /// </summary>
    private static string GetAuthErrorGuidance(string messageType, string errorMessage)
    {
        // Check for common error patterns and provide specific guidance
        if (errorMessage.Contains("Authentication required", StringComparison.OrdinalIgnoreCase))
        {
            return """
                Common causes:
                1. auth.config.ts is missing or not deployed
                   → Run: npx convex dev --once in your backend directory

                2. Authenticate message not sent or sent incorrectly
                   → Verify auth token provider is configured
                   → Check that Authenticate message has: tokenType, value, baseVersion

                3. JWT token validation failed
                   → Verify 'applicationID' matches JWT 'aud' claim
                   → Verify 'issuer' matches JWT 'iss' claim
                   → Check token is not expired

                Documentation: https://docs.convex.dev/auth/advanced/custom-jwt
                """;
        }

        if (errorMessage.Contains("Invalid token", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("JWT", StringComparison.OrdinalIgnoreCase))
        {
            return """
                JWT validation issues:
                1. Token format incorrect or corrupted
                2. Token signature verification failed
                   → Verify JWKS endpoint is accessible
                   → Check 'algorithm' matches token signing algorithm (usually RS256)
                3. Token claims don't match auth.config.ts
                   → 'aud' claim must match 'applicationID'
                   → 'iss' claim must match 'issuer'
                4. Token expired
                   → Check token 'exp' claim

                Debug: Decode your JWT at https://jwt.io to inspect claims
                """;
        }

        if (errorMessage.Contains("missing field", StringComparison.OrdinalIgnoreCase))
        {
            return """
                Protocol message format error:
                1. Authenticate message must include:
                   - type: "Authenticate"
                   - tokenType: "User" | "Admin" | "None"
                   - value: <JWT token string>
                   - baseVersion: <identity version number>

                2. Connect message must include:
                   - baseVersion: <query set version number>
                   (Note: auth token goes in separate Authenticate message)
                """;
        }

        // Generic guidance for other errors
        return messageType == "AuthError"
            ? """
              Authentication troubleshooting:
              1. Check backend auth.config.ts is deployed
              2. Verify JWT token is valid and not expired
              3. Ensure Authenticate message format is correct
              4. Check Convex backend logs for detailed error messages

              See: https://docs.convex.dev/auth/debug
              """
            : """
              Fatal error occurred. Common causes:
              1. Protocol message format error
              2. Backend configuration issue
              3. Network connectivity problem

              Check Convex backend logs for detailed error information.
              """;
    }

    /// <summary>
    /// Updates the connection state and raises the event.
    /// Events are automatically marshalled to the UI thread.
    /// </summary>
    private void UpdateConnectionState(ConnectionState newState)
    {
        if (ConnectionState == newState)
        {
            return;
        }

        ConnectionState = newState;

        // Use thread marshalling helper to invoke event on the correct thread
        ThreadMarshallingHelper.InvokeEvent(ConnectionStateChanged, this, newState, _syncContext);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Cancel receive loop
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();

        // Dispose WebSocket directly without waiting for async close
        // Note: We don't wait for CloseAsync here to avoid potential deadlocks on UI threads
        // The cancellation token will cause the receive loop to terminate
        _webSocket.Dispose();

        // Complete all subscription channels
        foreach (var subscriptionInfo in _subscriptions.Values)
        {
            subscriptionInfo.Channel.Writer.Complete();
        }
        _subscriptions.Clear();

        // Dispose semaphore
        _sendLock.Dispose();

        // Clear event handlers
        ConnectionStateChanged = null;
    }
}
