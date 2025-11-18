using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RealtimeChat.Frontend.Components;
using RealtimeChat.Shared.Models;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task Login()
    {
        if (!string.IsNullOrWhiteSpace(State.UsernameInput))
        {
            State.Username = State.UsernameInput.Trim();
            State.UsernameInput = "";

            // Sync username to ChatService
            ChatService.Username = State.Username;

            // Start presence tracking
            PresenceService.StartPresenceTracking(State.Username);

            // Start typing indicator tracking
            PresenceService.StartTypingTracking();

            // Subscribe to online users and typing indicators
            // Note: State updates are handled via OnlineUsersUpdated and TypingUsersUpdated events
            // subscribed in OnInitializedAsync, so we just need to start the subscriptions
            // The events will automatically update State.OnlineUsers and State.TypingUsers
            PresenceService.SubscribeToOnlineUsers(_ => { }, _ => { });
            PresenceService.SubscribeToTypingUsers(State.Username, _ => { }, _ => { });
            
            // Sync initial state immediately after subscription starts
            State.OnlineUsers = PresenceService.OnlineUsers;
            State.TypingUsers = PresenceService.TypingUsers;

            // Reload messages after login to ensure we have the latest
            await ChatService.LoadMessagesAsync();

            // Load reactions initially and subscribe for real-time updates
            if (State.Messages.Count > 0)
            {
                var messageIds = State.Messages.Select(m => m.Id).ToList();
                await ReactionService.LoadReactionsAsync(messageIds);
                // Sync state immediately after loading
                State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
                StateHasChanged();
                
                // Set up subscription - it will update state when reactions change
                ReactionService.SubscribeToReactions(messageIds, OnReactionsReceived, OnReactionsError);

                // Load read receipts and subscribe for real-time updates
                await LoadReadReceipts();
            }

            // Start read receipt timer to periodically mark visible messages as read
            StartReadReceiptTimer();

            StateHasChanged();
        }
    }

    protected void Logout()
    {
        // Clean up services
        PresenceService.StopPresenceTracking();
        PresenceService.StopTypingTracking();
        StopReadReceiptTimer();

        // Reset state
        State.Reset();
        _hasScrolledAfterLogin = false;

        StateHasChanged();
    }

    protected async Task HandleLoginKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(State.UsernameInput))
        {
            await Login();
            await Task.Delay(100);
            await JSRuntime.InvokeVoidAsync("focusElement", messageInputRef);
        }
    }

}
