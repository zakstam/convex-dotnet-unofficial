using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Features.RealTime.Pagination;
using Convex.Generated;
using RealtimeChat.Shared.Models;
using Message = RealtimeChat.Shared.Models.Message;

namespace RealtimeChat.Shared.Services;

/// <summary>
/// Business logic service for chat operations.
/// Handles all Convex client interactions, message management, and subscriptions.
/// Completely independent of UI concerns.
/// </summary>
public class ChatService : IDisposable
{
    #region Events
    /// <summary>
    /// Raised when messages are updated (from initial load or subscription).
    /// </summary>
    public event Action<List<MessageDto>>? MessagesUpdated;

    /// <summary>
    /// Raised when messages are updated as domain models.
    /// </summary>
    public event Action<List<Message>>? MessagesUpdatedAsDomain;

    /// <summary>
    /// Raised when loading state changes.
    /// </summary>
    public event Action<bool>? LoadingStateChanged;

    /// <summary>
    /// Raised when loading more (older messages) state changes.
    /// </summary>
    public event Action<bool>? LoadingMoreStateChanged;

    /// <summary>
    /// Raised when an error occurs.
    /// </summary>
    public event Action<string>? ErrorOccurred;

    #endregion

    #region State
    private readonly IConvexClient _client;
    private readonly string _getMessagesFunctionName;
    private readonly string _sendMessageFunctionName;
    private readonly string? _editMessageFunctionName;
    private readonly string? _deleteMessageFunctionName;
    private readonly string? _searchMessagesFunctionName;
    private PaginatedQueryHelper<MessageDto>? _paginationHelper;
    private List<MessageDto> _currentMessages = [];
    private const int PageSize = 25;
    private readonly int _initialMessageLimit;
    #endregion

    /// <summary>
    /// Creates a new ChatService instance.
    /// </summary>
    /// <param name="client">The Convex client instance.</param>
    /// <param name="getMessagesFunctionName">The function name for getting messages (e.g., "functions/getMessages" or from ConvexFunctions.Queries.GetMessages).</param>
    /// <param name="sendMessageFunctionName">The function name for sending messages (e.g., "functions/sendMessage" or from ConvexFunctions.Mutations.SendMessage).</param>
    /// <param name="initialMessageLimit">The initial number of messages to load. Defaults to 10.</param>
    /// <param name="editMessageFunctionName">Optional: The function name for editing messages.</param>
    /// <param name="deleteMessageFunctionName">Optional: The function name for deleting messages.</param>
    /// <param name="searchMessagesFunctionName">Optional: The function name for searching messages.</param>
    public ChatService(IConvexClient client, string getMessagesFunctionName = "functions/getMessages", string sendMessageFunctionName = "functions/sendMessage", int initialMessageLimit = 10, string? editMessageFunctionName = null, string? deleteMessageFunctionName = null, string? searchMessagesFunctionName = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _getMessagesFunctionName = getMessagesFunctionName ?? throw new ArgumentNullException(nameof(getMessagesFunctionName));
        _sendMessageFunctionName = sendMessageFunctionName ?? throw new ArgumentNullException(nameof(sendMessageFunctionName));
        _editMessageFunctionName = editMessageFunctionName;
        _deleteMessageFunctionName = deleteMessageFunctionName;
        _searchMessagesFunctionName = searchMessagesFunctionName;
        _initialMessageLimit = initialMessageLimit;
    }

    #region Public Properties
    /// <summary>
    /// Current username.
    /// </summary>
    public string Username { get; set; } = "Anonymous";

    /// <summary>
    /// Current messages list (read-only).
    /// </summary>
    public IReadOnlyList<MessageDto> CurrentMessages => _currentMessages.AsReadOnly();

    /// <summary>
    /// Whether messages are currently being loaded.
    /// </summary>
    public bool IsLoading { get; private set; } = false;

    /// <summary>
    /// Whether older messages are currently being loaded.
    /// </summary>
    public bool IsLoadingMore { get; private set; } = false;

    /// <summary>
    /// Whether there are more messages to load.
    /// </summary>
    public bool HasMoreMessages => _paginationHelper?.HasMore ?? false;

    /// <summary>
    /// Current page boundaries for pagination separators.
    /// </summary>
    public IReadOnlyList<int> PageBoundaries => _paginationHelper?.PageBoundaries ?? [];
    #endregion

    #region Message Loading
    /// <summary>
    /// Load initial messages from Convex using pagination.
    /// </summary>
    public async Task LoadInitialMessagesAsync() => await LoadMessagesAsync();

    /// <summary>
    /// Load initial messages from Convex using pagination (alias for LoadInitialMessagesAsync).
    /// </summary>
    public async Task LoadMessagesAsync()
    {
        if (IsLoading)
        {
            return;
        }

        SetLoadingState(true);

        try
        {
            // Dispose any previous pagination helper
            if (_paginationHelper != null)
            {
                _paginationHelper.ItemsUpdated -= OnPaginationItemsUpdated;
                _paginationHelper.PageBoundaryAdded -= OnPageBoundaryAdded;
                _paginationHelper.SubscriptionStatusChanged -= OnSubscriptionStatusChanged;
                _paginationHelper.ErrorOccurred -= OnPaginationError;
                _paginationHelper.Dispose();
                _paginationHelper = null;
            }

            // Create pagination helper with automatic subscription handling and UI thread marshalling.
            // Since MessageDto implements IHasId and IHasSortKey, we don't need WithIdExtractor or WithSortKey.
            // The subscription returns GetMessagesResponse wrapper, so we still need WithSubscriptionExtractor.
            _paginationHelper = await _client
                .Paginate<MessageDto>(_getMessagesFunctionName, PageSize)
                .WithArgs(new Convex.Generated.GetMessagesArgs { Limit = _initialMessageLimit })
                .WithSubscriptionExtractor<GetMessagesResponse>(response => response.Messages ?? [])
                .WithUIThreadMarshalling()
                .OnItemsUpdated(OnPaginationItemsUpdated)
                .OnPageBoundaryAdded(OnPageBoundaryAdded)
                .OnSubscriptionStatusChanged(OnSubscriptionStatusChanged)
                .OnError(OnPaginationError)
                .InitializeAsync(enableSubscription: true);
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to load messages: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void OnPaginationItemsUpdated(IReadOnlyList<MessageDto> items, IReadOnlyList<int> boundaries)
    {
        _currentMessages = [.. items];
        MessagesUpdated?.Invoke(_currentMessages);

        // Also raise event with domain models
        var domainMessages = _currentMessages.Select(m => new Message(
            Id: m.Id,
            Username: m.Username,
            Text: m.Text,
            Timestamp: m.Timestamp,
            EditedAt: m.EditedAt,
            ParentMessageId: m.ParentMessageId,
            Attachments: m.Attachments?.Select(a => new Attachment(
                StorageId: a.StorageId,
                Filename: a.Filename,
                ContentType: a.ContentType,
                Size: (long)a.Size
            )).ToList()
        )).ToList();
        MessagesUpdatedAsDomain?.Invoke(domainMessages);
    }

    private void OnPageBoundaryAdded(int boundaryIndex)
    {
        // Can be used for UI pagination separators if needed
    }

    private void OnSubscriptionStatusChanged(string status)
    {
        // Can be used for connection status updates if needed
    }

    private void OnPaginationError(string error) => ErrorOccurred?.Invoke(error);

    /// <summary>
    /// Load older messages (next page) using pagination.
    /// </summary>
    public async Task LoadOlderMessagesAsync() => await LoadMoreMessagesAsync();

    /// <summary>
    /// Load older messages (next page) using pagination (alias for LoadOlderMessagesAsync).
    /// </summary>
    public async Task LoadMoreMessagesAsync()
    {
        if (IsLoadingMore || _paginationHelper == null || !_paginationHelper.HasMore)
        {
            return;
        }

        SetLoadingMoreState(true);

        try
        {
            // Load next page (ItemsUpdated event will be raised automatically)
            _ = await _paginationHelper.LoadNextAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to load older messages: {ex.Message}");
        }
        finally
        {
            SetLoadingMoreState(false);
        }
    }

    /// <summary>
    /// Subscribe to real-time message updates for new messages.
    /// Note: Subscription is now handled automatically by PaginatedQueryHelper.
    /// This method is kept for backwards compatibility but does nothing.
    /// </summary>
    public void SubscribeToMessages()
    {
        // Subscription is automatically started in InitializeAsync()
    }

    /// <summary>
    /// Subscribe to real-time message updates with callback.
    /// Note: This method is deprecated. Subscription is handled automatically by PaginatedQueryHelper.
    /// Use ChatService events (MessagesUpdated, ErrorOccurred) instead.
    /// </summary>
    [Obsolete("Subscription is handled automatically. Use ChatService events instead.")]
    public void SubscribeToMessages(Action<List<Message>>? onUpdate = null, Action<string>? onError = null)
    {
        // Subscription is automatically started in InitializeAsync()
        // Use ChatService events (MessagesUpdated, ErrorOccurred) instead
    }
    #endregion

    #region Message Sending
    /// <summary>
    /// Send a new message.
    /// </summary>
    public async Task<bool> SendMessageAsync(string text, List<Attachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(text) && (attachments == null || attachments.Count == 0))
        {
            return false;
        }

        try
        {
            var messageText = text?.Trim() ?? "";

            var result = await _client.Mutate<object>(_sendMessageFunctionName)
                .WithArgs(new Convex.Generated.SendMessageArgs
                {
                    Username = Username,
                    Text = messageText,
                    Attachments = attachments?.Select(a => new SendMessageArgsAttachments
                    {
                        StorageId = a.StorageId,
                        Filename = a.Filename,
                        ContentType = a.ContentType,
                        Size = a.Size
                    }).ToList()
                })
                .ExecuteAsync();

            return true;
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to send message: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a reply to a message.
    /// </summary>
    public async Task SendReplyAsync(string text, MessageId parentMessageId, List<Attachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(text) && (attachments == null || attachments.Count == 0))
        {
            return;
        }

        try
        {
            var messageText = text?.Trim() ?? "";

            // Use sendMessage with parentMessageId, or a dedicated sendReply function if available
            // For now, we'll use sendMessage with parentMessageId
            _ = await _client.Mutate<object>(_sendMessageFunctionName)
                .WithArgs(new Convex.Generated.SendMessageArgs
                {
                    Username = Username,
                    Text = messageText,
                    ParentMessageId = parentMessageId,
                    Attachments = attachments?.Select(a => new SendMessageArgsAttachments
                    {
                        StorageId = a.StorageId,
                        Filename = a.Filename,
                        ContentType = a.ContentType,
                        Size = a.Size
                    }).ToList()
                })
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to send reply: {ex.Message}");
        }
    }

    /// <summary>
    /// Edit an existing message.
    /// </summary>
    public async Task EditMessageAsync(string messageId, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText) || string.IsNullOrEmpty(_editMessageFunctionName))
        {
            if (string.IsNullOrEmpty(_editMessageFunctionName))
            {
                ErrorOccurred?.Invoke("EditMessageAsync requires an editMessage function to be configured");
            }
            return;
        }

        try
        {
            _ = await _client.Mutate<object>(_editMessageFunctionName)
                .WithArgs(new Convex.Generated.EditMessageArgs
                {
                    Id = messageId,
                    Text = newText.Trim()
                })
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to edit message: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a message.
    /// </summary>
    public async Task DeleteMessageAsync(string messageId)
    {
        if (string.IsNullOrEmpty(_deleteMessageFunctionName))
        {
            ErrorOccurred?.Invoke("DeleteMessageAsync requires a deleteMessage function to be configured");
            return;
        }

        try
        {
            _ = await _client.Mutate<object>(_deleteMessageFunctionName)
                .WithArgs(new Convex.Generated.DeleteMessageArgs
                {
                    Id = messageId
                })
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Failed to delete message: {ex.Message}");
        }
    }
    #endregion

    #region Search
    /// <summary>
    /// Search messages by text or username (local search).
    /// </summary>
    public List<MessageDto> SearchMessages(string searchText, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            return [];
        }

        return [.. _currentMessages
            .Where(m => m.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       m.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .Take(maxResults)];
    }

    /// <summary>
    /// Search messages using server-side search (if available).
    /// </summary>
    public async Task<List<Message>> SearchMessagesAsync(string searchText, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        try
        {
            // Try server-side search if available, otherwise fall back to local search
            if (!string.IsNullOrEmpty(_searchMessagesFunctionName))
            {
                var results = await _client.Query<List<MessageDto>>(_searchMessagesFunctionName)
                    .WithArgs(new Dictionary<string, object> { ["searchText"] = searchText, ["limit"] = limit })
                    .ExecuteAsync();

                if (results != null)
                {
                    return [.. results.Select(m => new Message(
                        Id: m.Id,
                        Username: m.Username,
                        Text: m.Text,
                        Timestamp: m.Timestamp,
                        EditedAt: m.EditedAt,
                        ParentMessageId: m.ParentMessageId,
                        Attachments: m.Attachments?.Select(a => new Attachment(
                            StorageId: a.StorageId,
                            Filename: a.Filename,
                            ContentType: a.ContentType,
                            Size: (long)a.Size
                        )).ToList()
                    ))];
                }
            }

            // Fall back to local search
            var localResults = SearchMessages(searchText, limit);
            return [.. localResults.Select(m => new Message(
                Id: m.Id,
                Username: m.Username,
                Text: m.Text,
                Timestamp: m.Timestamp,
                EditedAt: m.EditedAt,
                ParentMessageId: m.ParentMessageId,
                Attachments: m.Attachments?.Select(a => new Attachment(
                    StorageId: a.StorageId,
                    Filename: a.Filename,
                    ContentType: a.ContentType,
                    Size: (long)a.Size
                )).ToList()
            ))];
        }
        catch (ConvexException ex)
        {
            ErrorOccurred?.Invoke($"Error searching messages: {ex.Message}");
            return [];
        }
    }
    #endregion

    #region Message Grouping
    /// <summary>
    /// Gets grouped messages for display.
    /// </summary>
    public List<MessageGroup> GetGroupedMessages(string currentUsername)
    {
        var messages = _currentMessages
            .OrderBy(m => m.Timestamp)
            .ToList();

        if (messages.Count == 0)
        {
            return [];
        }

        var grouped = new List<MessageGroup>();
        MessageGroup? currentGroup = null;
        const long GROUP_TIME_THRESHOLD = 300000; // 5 minutes in milliseconds

        foreach (var message in messages)
        {
            var isOwnMessage = message.Username == currentUsername;

            if (currentGroup == null ||
                currentGroup.Username != message.Username ||
                (message.Timestamp - currentGroup.Messages.Last().Timestamp) > GROUP_TIME_THRESHOLD)
            {
                if (currentGroup != null)
                {
                    grouped.Add(currentGroup);
                }

                currentGroup = new MessageGroup(
                    message.Username,
                    [message],
                    isOwnMessage
                );
            }
            else
            {
                currentGroup = currentGroup with
                {
                    Messages = [.. currentGroup.Messages, message]
                };
            }
        }

        if (currentGroup != null)
        {
            grouped.Add(currentGroup);
        }

        return grouped;
    }
    #endregion

    #region Helper Methods
    private void SetLoadingState(bool loading)
    {
        IsLoading = loading;
        LoadingStateChanged?.Invoke(IsLoading);
    }

    private void SetLoadingMoreState(bool loading)
    {
        IsLoadingMore = loading;
        LoadingMoreStateChanged?.Invoke(IsLoadingMore);
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        if (_paginationHelper != null)
        {
            _paginationHelper.ItemsUpdated -= OnPaginationItemsUpdated;
            _paginationHelper.PageBoundaryAdded -= OnPageBoundaryAdded;
            _paginationHelper.SubscriptionStatusChanged -= OnSubscriptionStatusChanged;
            _paginationHelper.Dispose();
        }
        GC.SuppressFinalize(this);
    }
    #endregion
}

