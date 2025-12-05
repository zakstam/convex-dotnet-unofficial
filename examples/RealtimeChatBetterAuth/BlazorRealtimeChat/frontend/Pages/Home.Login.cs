using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RealtimeChat.Frontend.Components;
using RealtimeChat.Shared.Models;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    /// <summary>
    /// Called when login is successful (from LoginForm).
    /// Initializes the chat services for the authenticated user.
    /// </summary>
    protected async Task OnLoginSuccess()
    {
        if (!AuthService.IsAuthenticated || AuthService.CurrentUser == null)
        {
            return;
        }

        await InitializeChatForAuthenticatedUser();
        StateHasChanged();
    }

    /// <summary>
    /// Initializes chat services for the authenticated user.
    /// Called both on login and when restoring an existing session.
    /// </summary>
    private async Task InitializeChatForAuthenticatedUser()
    {
        var user = AuthService.CurrentUser;
        if (user == null) return;

        try
        {
            // Token provider is automatically wired up via DI in AddConvexBetterAuth()
            // No need to manually call SetAuthTokenProviderAsync

            // Set the username from the authenticated user
            var username = user.Name ?? user.Email?.Split('@')[0] ?? "Anonymous";
            State.Username = username;

            // Sync username to ChatService
            ChatService.Username = username;

            // Start presence tracking
            PresenceService.StartPresenceTracking(username);

            // Start typing indicator tracking
            PresenceService.StartTypingTracking();

            // Subscribe to online users and typing indicators
            PresenceService.SubscribeToOnlineUsers(_ => { }, _ => { });
            PresenceService.SubscribeToTypingUsers(username, _ => { }, _ => { });

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
        }
        catch (Exception ex)
        {
            // Log the error but don't crash - user can still see the UI
            Console.WriteLine($"Error initializing chat: {ex.Message}");
            // Sign out to allow user to try again
            await AuthService.SignOutAsync();
            await ConvexClient.Auth.ClearAuthAsync();
            State.Reset();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    protected async Task Logout()
    {
        // Sign out via BetterAuth
        await AuthService.SignOutAsync();

        // Clear Convex authentication
        await ConvexClient.Auth.ClearAuthAsync();

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
        // This method is no longer used with email/password auth
        // The LoginForm handles its own key events
        await Task.CompletedTask;
    }
}
