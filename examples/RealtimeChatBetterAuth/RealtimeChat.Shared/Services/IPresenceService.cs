using RealtimeChat.Shared.Models;

namespace RealtimeChat.Shared.Services;

/// <summary>
/// Service interface for presence and typing indicator operations.
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Starts presence tracking for the current user.
    /// </summary>
    void StartPresenceTracking(string username);

    /// <summary>
    /// Stops presence tracking.
    /// </summary>
    void StopPresenceTracking();

    /// <summary>
    /// Updates the user's presence.
    /// </summary>
    Task UpdatePresenceAsync(string username);

    /// <summary>
    /// Sets the typing indicator for the current user.
    /// </summary>
    Task SetTypingAsync(string username, bool isTyping);

    /// <summary>
    /// Starts typing indicator tracking.
    /// </summary>
    void StartTypingTracking();

    /// <summary>
    /// Stops typing indicator tracking.
    /// </summary>
    void StopTypingTracking();

    /// <summary>
    /// Subscribes to online users updates.
    /// </summary>
    void SubscribeToOnlineUsers(Action<List<OnlineUserDto>> onUpdate, Action<string>? onError = null);

    /// <summary>
    /// Subscribes to typing users updates.
    /// </summary>
    void SubscribeToTypingUsers(string currentUsername, Action<List<string>> onUpdate, Action<string>? onError = null);

    /// <summary>
    /// Event raised when online users are updated.
    /// </summary>
    event EventHandler? OnlineUsersUpdated;

    /// <summary>
    /// Event raised when typing users are updated.
    /// </summary>
    event EventHandler? TypingUsersUpdated;

    /// <summary>
    /// Handles typing input to update typing indicator.
    /// </summary>
    void HandleTypingInput(string messageText);
}

