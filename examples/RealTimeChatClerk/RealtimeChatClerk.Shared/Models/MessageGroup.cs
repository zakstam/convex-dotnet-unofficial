namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Represents a group of messages from the same user within a time window.
/// Used for better UX when displaying messages.
/// </summary>
public record MessageGroup(string Username, List<MessageDto> Messages, bool IsOwnMessage);

