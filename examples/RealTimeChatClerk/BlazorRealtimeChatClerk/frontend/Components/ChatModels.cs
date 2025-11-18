using System.Text.Json.Serialization;

namespace RealtimeChat.Frontend.Components;

// Response DTO for getMessages function - now matches PaginationResult format
// Using shared GetMessagesResponse from RealtimeChatClerk.Shared.Models

// Pagination options for subscription (needed because anonymous types can't be serialized when trimmed)
public class PaginationOptionsArgs
{
    [JsonPropertyName("numItems")]
    public int NumItems { get; set; }
}

// Arguments for getMessages subscription
public class GetMessagesSubscriptionArgs
{
    [JsonPropertyName("paginationOpts")]
    public PaginationOptionsArgs PaginationOpts { get; set; } = new();
}

// Arguments for getMessages subscription using limit parameter
public class GetMessagesSubscriptionArgsWithLimit
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

// Using shared types from RealtimeChatClerk.Shared.Models
// - MessageDto, AttachmentDto, OnlineUserDto, ReactionDto
// - Message, Attachment, MessageGroup, PendingFile

// Reply count DTO
public class ReplyCountDto
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

// Using shared argument classes from RealtimeChatClerk.Shared.Models:
// - SendReplyArgs, GetRepliesArgs, EditMessageArgs, DeleteMessageArgs
// - UpdatePresenceArgs, SetTypingArgs, ToggleReactionArgs, GetReactionsArgs
// - SendMessageArgs, GetMessagesArgs

