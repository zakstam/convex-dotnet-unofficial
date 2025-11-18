using Microsoft.AspNetCore.Components;
using RealtimeChat.Frontend.Components;
using RealtimeChatClerk.Shared.Models;
using RealtimeChatClerk.Shared.Services;
using Convex.Client.Extensions.Clerk.Blazor;
using MessageDto = RealtimeChatClerk.Shared.Models.MessageDto;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task HandleClerkAuthenticated()
    {
        try
        {
            // Get user info from Clerk
            var userId = await ClerkTokenService.GetUserIdAsync();
            var userEmail = await ClerkTokenService.GetUserEmailAsync();
            
            // Use email as username, or fallback to user ID
            State.Username = userEmail ?? userId ?? "User";

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
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling Clerk authentication: {ex.Message}");
        }
    }

    private async Task Logout()
    {
        try
        {
            // Sign out from Clerk
            await ClerkTokenService.SignOutAsync();
            
            // Clean up services
            PresenceService.StopPresenceTracking();
            PresenceService.StopTypingTracking();
            StopReadReceiptTimer();

            // Reset state
            State.Reset();
            _hasScrolledAfterLogin = false;

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
        }
    }
}
