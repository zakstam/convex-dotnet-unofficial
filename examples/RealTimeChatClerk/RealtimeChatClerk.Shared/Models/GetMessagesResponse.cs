using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Response from the getMessages query function.
/// Contains a list of messages that can be displayed in the chat UI.
/// </summary>
public class GetMessagesResponse
{
    /// <summary>
    /// List of message objects.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<MessageDto> Messages { get; set; } = new();

    /// <summary>
    /// Indicates if there are more messages to load (used with pagination).
    /// </summary>
    [JsonPropertyName("isDone")]
    public bool IsDone { get; set; }

    /// <summary>
    /// Cursor for pagination to load the next page of messages.
    /// </summary>
    [JsonPropertyName("continueCursor")]
    public string? ContinueCursor { get; set; }
}

