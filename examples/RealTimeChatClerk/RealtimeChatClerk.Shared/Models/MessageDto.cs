using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Data Transfer Object for a chat message.
/// Maps directly from Convex backend response.
/// </summary>
public class MessageDto
{
    /// <summary>
    /// Unique message ID (assigned by Convex).
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username of the message sender.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Message content text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp in milliseconds since epoch.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Optional: ID of parent message (for threads/replies).
    /// </summary>
    [JsonPropertyName("parentMessageId")]
    public string? ParentMessageId { get; set; }

    /// <summary>
    /// Optional: When message was last edited (ms since epoch).
    /// </summary>
    [JsonPropertyName("editedAt")]
    public long? EditedAt { get; set; }

    /// <summary>
    /// Optional: File attachments.
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<AttachmentDto>? Attachments { get; set; }

    /// <summary>
    /// Get formatted timestamp as a human-readable relative time string.
    /// </summary>
    /// <returns>
    /// A relative time string like "just now", "5m ago", "2h ago", "3d ago",
    /// or the full date for messages older than a week.
    /// </returns>
    public string GetFormattedTime()
    {
        var messageTime = UnixTimeStampToDateTime(Timestamp);
        var timeSinceMessage = DateTime.Now - messageTime;

        if (timeSinceMessage.TotalMinutes < 1)
        {
            return "just now";
        }

        if (timeSinceMessage.TotalMinutes < 60)
        {
            var minutes = (int)timeSinceMessage.TotalMinutes;
            return $"{minutes}m ago";
        }

        if (timeSinceMessage.TotalHours < 24)
        {
            var hours = (int)timeSinceMessage.TotalHours;
            return $"{hours}h ago";
        }

        if (timeSinceMessage.TotalDays < 7)
        {
            var days = (int)timeSinceMessage.TotalDays;
            return $"{days}d ago";
        }

        return messageTime.ToString("MMM d, yyyy");
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}

