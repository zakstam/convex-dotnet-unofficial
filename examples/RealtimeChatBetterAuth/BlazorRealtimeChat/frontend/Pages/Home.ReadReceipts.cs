using RealtimeChat.Frontend.Components;
using RealtimeChat.Frontend.Services;
using RealtimeChat.Shared.Models;
using MessageDto = RealtimeChat.Shared.Models.MessageDto;
using MessageReadDto = RealtimeChat.Shared.Models.MessageReadDto;
using System.Collections.Generic;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    private readonly HashSet<string> _markedAsRead = new(); // Track which messages we've marked as read
    private readonly Dictionary<string, DateTime> _messageViewTimes = new(); // Track when messages were first viewed
    private System.Threading.Timer? _readReceiptTimer;

    /// <summary>
    /// Marks messages as read when they come into view.
    /// Only marks messages from other users (not own messages).
    /// Uses debouncing to avoid marking on every scroll event.
    /// </summary>
    protected async Task MarkVisibleMessagesAsRead()
    {
        if (string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        try
        {
            // Get messages that are currently visible in viewport
            var visibleMessageIds = await JSRuntime.InvokeAsync<List<string>>("getVisibleMessageIds", Array.Empty<object>());
            
            if (visibleMessageIds == null || visibleMessageIds.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var messagesToMark = new List<string>();

            foreach (var messageId in visibleMessageIds)
            {
                // Find the message
                var message = State.Messages.FirstOrDefault(m => m.Id == messageId);
                if (message == null)
                {
                    continue;
                }

                // Only mark messages from other users as read
                if (message.Username == State.Username)
                {
                    continue;
                }

                // Skip if already marked as read
                if (_markedAsRead.Contains(messageId))
                {
                    continue;
                }

                // Track when message was first viewed
                if (!_messageViewTimes.TryGetValue(messageId, out var existingViewTime))
                {
                    _messageViewTimes[messageId] = now;
                }

                // Mark as read if message has been visible for more than 1 second
                var viewTime = _messageViewTimes[messageId];
                if ((now - viewTime).TotalSeconds >= 1.0)
                {
                    messagesToMark.Add(messageId);
                }
            }

            // Mark messages as read
            foreach (var messageId in messagesToMark)
            {
                if (_markedAsRead.Add(messageId))
                {
                    _ = ReadReceiptService.MarkMessageAsReadAsync(messageId, State.Username);
                }
            }
        }
        catch
        {
            // Silently handle errors
        }
    }

    /// <summary>
    /// Loads read receipts for messages and subscribes to updates.
    /// </summary>
    protected async Task LoadReadReceipts()
    {
        if (State.Messages.Count == 0 || string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        try
        {
            var messageIds = State.Messages.Select(m => m.Id).ToList();
            
            await ReadReceiptService.LoadReadReceiptsAsync(messageIds);
            
            State.MessageReadReceipts = CloneReadReceipts(ReadReceiptService.MessageReadReceipts, State.Messages);
            State.CurrentUserReadMessages = GetCurrentUserReadMessages(ReadReceiptService.MessageReadReceipts, State.Username);
            State.UnreadMessageCount = GetUnreadMessageCount(State.Messages, State.CurrentUserReadMessages, State.Username);
            
            // Set up subscription for real-time updates
            ReadReceiptService.SubscribeToReadReceipts(messageIds, OnReadReceiptsReceived, OnReadReceiptsError);
            
            StateHasChanged();
        }
        catch
        {
            // Silently handle errors
        }
    }

    private void OnReadReceiptsReceived(Dictionary<string, List<MessageReadDto>> readReceipts)
    {
        State.MessageReadReceipts = CloneReadReceipts(readReceipts, State.Messages);
        State.CurrentUserReadMessages = GetCurrentUserReadMessages(readReceipts, State.Username);
        State.UnreadMessageCount = GetUnreadMessageCount(State.Messages, State.CurrentUserReadMessages, State.Username);
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnReadReceiptsError(string error)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Starts a timer to periodically check for visible messages and mark them as read.
    /// </summary>
    protected void StartReadReceiptTimer()
    {
        _readReceiptTimer?.Dispose();
        _readReceiptTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await MarkVisibleMessagesAsRead();
            });
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)); // Check every 2 seconds
    }

    /// <summary>
    /// Stops the read receipt timer.
    /// </summary>
    protected void StopReadReceiptTimer()
    {
        _readReceiptTimer?.Dispose();
        _readReceiptTimer = null;
    }
}

