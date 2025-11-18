using System;
using System.Collections.Generic;
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
    /// Get formatted timestamp as readable string.
    /// </summary>
    public string GetFormattedTime()
    {
        var dateTime = UnixTimeStampToDateTime(Timestamp);
        var now = DateTime.Now;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}

