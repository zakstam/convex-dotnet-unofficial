using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RealtimeChat.Frontend;
using RealtimeChat.Frontend.Components;
using RealtimeChat.Frontend.Services;
using RealtimeChatClerk.Shared.Models;
using RealtimeChatClerk.Shared.Services;
using Convex.Client.Extensions.Clerk.Blazor;
using MessageDto = RealtimeChatClerk.Shared.Models.MessageDto;

namespace RealtimeChat.Frontend.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] public ChatState State { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.ChatService ChatService { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.PresenceService PresenceService { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.ReactionService ReactionService { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.FileService FileService { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.ReplyService ReplyService { get; set; } = default!;
    [Inject] public RealtimeChatClerk.Shared.Services.ReadReceiptService ReadReceiptService { get; set; } = default!;
    [Inject] public BlazorClerkTokenService ClerkTokenService { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private bool _hasScrolledAfterLogin = false;
    private int _previousMessageCount = 0;

    protected override async Task OnInitializedAsync()
    {
        // Load dark mode preference
        try
        {
            var darkModeValue = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "darkMode");
            State.IsDarkMode = darkModeValue == "true";
        }
        catch { }

        // Check if user is already authenticated with Clerk
        await ClerkTokenService.InitializeAsync();
        if (ClerkTokenService.IsAuthenticated)
        {
            await HandleClerkAuthenticated();
        }

        // Subscribe to service events
        ChatService.MessagesUpdated += OnMessagesUpdated;
        ChatService.LoadingStateChanged += OnLoadingStateChanged;
        ChatService.LoadingMoreStateChanged += OnLoadingMoreStateChanged;
        PresenceService.OnlineUsersUpdated += OnOnlineUsersUpdated;
        PresenceService.TypingUsersUpdated += OnTypingUsersUpdated;
        ReactionService.ReactionsUpdated += OnReactionsUpdated;
        ReadReceiptService.ReadReceiptsUpdated += OnReadReceiptsUpdated;

        // Don't subscribe to messages here - wait until after login
        // Subscription will be set up in Login() method
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only scroll on first render
        if (firstRender)
        {
            await ScrollToBottom();
            _previousMessageCount = State.Messages.Count;
        }
        // Scroll after login if we haven't scrolled yet
        else if (!string.IsNullOrEmpty(State.Username) && !_hasScrolledAfterLogin)
        {
            await ScrollToBottom();
            _hasScrolledAfterLogin = true;
            _previousMessageCount = State.Messages.Count;
        }
    }

    private async void OnMessagesUpdated(List<MessageDto> messages)
    {
        // Note: Callback is already on UI thread thanks to WithUIThreadMarshalling() in ChatService
        // However, we still need InvokeAsync for Blazor's StateHasChanged() to work properly
        await InvokeAsync(async () =>
        {
            var currentCount = messages.Count;
            var isNewMessage = currentCount > _previousMessageCount;
            var isDeletedMessage = currentCount < _previousMessageCount;

            // Check if user is near bottom BEFORE updating the UI (for accurate scroll position)
            var wasNearBottom = isNewMessage ? await IsNearBottomAsync() : false;

            // Update the previous count
            _previousMessageCount = currentCount;

            // Update state with new messages
            State.Messages = messages.ToList();
            State.CanLoadMore = ChatService.HasMoreMessages;
            StateHasChanged();

            // Load attachment URLs for messages (especially important for newly sent messages with images)
            if (State.Messages.Count > 0)
            {
                await LoadAttachmentUrlsForMessagesAsync(State.Messages);
                StateHasChanged();
            }

            // Load reactions when messages are updated (especially important on page refresh)
            if (State.Messages.Count > 0 && !string.IsNullOrEmpty(State.Username))
            {
                var messageIds = State.Messages.Select(m => m.Id).ToList();
                await ReactionService.LoadReactionsAsync(messageIds);
                State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
                
                // Set up subscription if not already set up
                ReactionService.SubscribeToReactions(messageIds, OnReactionsReceived, OnReactionsError);
                
                // Load read receipts and subscribe for real-time updates
                await LoadReadReceipts();
                
                // Update unread count after loading read receipts
                State.UnreadMessageCount = GetUnreadMessageCount(State.Messages, State.CurrentUserReadMessages, State.Username);
                
                StateHasChanged();
            }

            // Auto-scroll if new messages were added and user was near bottom
            if (isNewMessage && wasNearBottom)
            {
                // Small delay to ensure DOM has updated with new messages
                await Task.Delay(100);
                await ScrollToBottom();
            }
        });
    }

    private void OnLoadingStateChanged(bool isLoading)
    {
        State.IsLoading = isLoading;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnLoadingMoreStateChanged(bool isLoadingMore)
    {
        State.IsLoadingMore = isLoadingMore;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnOnlineUsersUpdated(object? sender, EventArgs e)
    {
        State.OnlineUsers = PresenceService.OnlineUsers;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnTypingUsersUpdated(object? sender, EventArgs e)
    {
        State.TypingUsers = PresenceService.TypingUsers;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnReactionsUpdated(object? sender, EventArgs e)
    {
        State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnReadReceiptsUpdated(object? sender, EventArgs e)
    {
        State.MessageReadReceipts = CloneReadReceipts(ReadReceiptService.MessageReadReceipts, State.Messages);
        State.CurrentUserReadMessages = GetCurrentUserReadMessages(ReadReceiptService.MessageReadReceipts, State.Username);
        State.UnreadMessageCount = GetUnreadMessageCount(State.Messages, State.CurrentUserReadMessages, State.Username);
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Gets the count of unread messages (messages from other users that the current user hasn't read).
    /// </summary>
    private int GetUnreadMessageCount(
        List<MessageDto> messages,
        HashSet<string> currentUserReadMessages,
        string currentUsername)
    {
        if (string.IsNullOrEmpty(currentUsername) || messages.Count == 0)
        {
            return 0;
        }

        return messages.Count(m => 
            m.Username != currentUsername && 
            !currentUserReadMessages.Contains(m.Id));
    }

    /// <summary>
    /// Gets the set of message IDs that the current user has read.
    /// Uses unfiltered read receipts to determine read status for all messages.
    /// </summary>
    private HashSet<string> GetCurrentUserReadMessages(
        Dictionary<string, List<MessageReadDto>> readReceipts,
        string currentUsername)
    {
        var readMessages = new HashSet<string>();

        if (string.IsNullOrEmpty(currentUsername) || readReceipts.Count == 0)
        {
            return readMessages;
        }

        foreach (var kvp in readReceipts)
        {
            var messageId = kvp.Key;
            var receipts = kvp.Value;

            // Check if current user has read this message
            if (receipts.Any(r => r.Username == currentUsername))
            {
                readMessages.Add(messageId);
            }
        }

        return readMessages;
    }

    /// <summary>
    /// Filters read receipts to only show each user's avatar on the latest message they've read.
    /// </summary>
    private Dictionary<string, List<MessageReadDto>> FilterReadReceiptsToLatestMessage(
        Dictionary<string, List<MessageReadDto>> readReceipts,
        List<MessageDto> messages)
    {
        if (readReceipts.Count == 0 || messages.Count == 0)
        {
            return new Dictionary<string, List<MessageReadDto>>();
        }

        // Create a dictionary to map message IDs to their timestamps for quick lookup
        var messageTimestamps = messages.ToDictionary(m => m.Id, m => m.Timestamp);

        // For each user, find the latest message they've read
        var userLatestMessage = new Dictionary<string, (string messageId, long timestamp)>();
        
        foreach (var kvp in readReceipts)
        {
            var messageId = kvp.Key;
            
            // Skip if message doesn't exist in current messages list
            if (!messageTimestamps.TryGetValue(messageId, out var messageTimestamp))
            {
                continue;
            }

            foreach (var readReceipt in kvp.Value)
            {
                var username = readReceipt.Username;
                
                // Track the latest message for this user
                if (!userLatestMessage.TryGetValue(username, out var currentLatest) ||
                    messageTimestamp > currentLatest.timestamp)
                {
                    userLatestMessage[username] = (messageId, messageTimestamp);
                }
            }
        }

        // Build filtered dictionary: only include read receipts for each user's latest message
        var filtered = new Dictionary<string, List<MessageReadDto>>();
        
        foreach (var (username, (messageId, _)) in userLatestMessage)
        {
            if (!filtered.TryGetValue(messageId, out var receipts))
            {
                receipts = new List<MessageReadDto>();
                filtered[messageId] = receipts;
            }

            // Find the original read receipt for this user and message
            if (readReceipts.TryGetValue(messageId, out var originalReceipts))
            {
                var userReceipt = originalReceipts.FirstOrDefault(r => r.Username == username);
                if (userReceipt != null)
                {
                    receipts.Add(new MessageReadDto
                    {
                        Username = userReceipt.Username,
                        ReadAt = userReceipt.ReadAt
                    });
                }
            }
        }

        return filtered;
    }

    private Dictionary<string, List<MessageReadDto>> CloneReadReceipts(
        Dictionary<string, List<MessageReadDto>> readReceipts,
        List<MessageDto> messages)
    {
        // First clone the read receipts
        var cloned = new Dictionary<string, List<MessageReadDto>>();
        foreach (var kvp in readReceipts)
        {
            cloned[kvp.Key] = kvp.Value.Select(r => new MessageReadDto
            {
                Username = r.Username,
                ReadAt = r.ReadAt
            }).ToList();
        }
        
        // Then filter to only show each user's latest read message
        return FilterReadReceiptsToLatestMessage(cloned, messages);
    }

    // Forward subscription callbacks to methods in Home.Messages.cs
    private void OnMessagesReceivedFromService(List<MessageDto> messages) => OnMessagesReceived(messages);
    private void OnMessagesErrorFromService(string error) => OnMessagesError(error);

    public void Dispose()
    {
        ChatService.MessagesUpdated -= OnMessagesUpdated;
        ChatService.LoadingStateChanged -= OnLoadingStateChanged;
        ChatService.LoadingMoreStateChanged -= OnLoadingMoreStateChanged;
        PresenceService.OnlineUsersUpdated -= OnOnlineUsersUpdated;
        PresenceService.TypingUsersUpdated -= OnTypingUsersUpdated;
        ReactionService.ReactionsUpdated -= OnReactionsUpdated;
        ReadReceiptService.ReadReceiptsUpdated -= OnReadReceiptsUpdated;

        ChatService.Dispose();
        if (PresenceService is IDisposable presenceDisposable)
        {
            presenceDisposable.Dispose();
        }
        if (ReactionService is IDisposable reactionDisposable)
        {
            reactionDisposable.Dispose();
        }
        if (ReadReceiptService is IDisposable readReceiptDisposable)
        {
            readReceiptDisposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

