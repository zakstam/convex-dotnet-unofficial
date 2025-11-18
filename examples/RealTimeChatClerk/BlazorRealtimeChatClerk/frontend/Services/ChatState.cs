using RealtimeChat.Frontend.Components;
using RealtimeChatClerk.Shared.Models;
using MessageDto = RealtimeChatClerk.Shared.Models.MessageDto;

namespace RealtimeChat.Frontend.Services;

/// <summary>
/// Represents the UI state for the chat application.
/// This class holds all state that is used for rendering the UI.
/// </summary>
public class ChatState
{
    // User state
    public string Username { get; set; } = "";
    public string UsernameInput { get; set; } = "";

    // Message state
    public string MessageText { get; set; } = "";
    public string EditMessageText { get; set; } = "";
    public bool IsLoading { get; set; } = true;
    public bool IsSending { get; set; } = false;
    public bool IsSavingEdit { get; set; } = false;
    public bool IsLoadingMore { get; set; } = false;

    // Message editing state
    public string? HoveredMessageId { get; set; } = null;
    public MessageDto? EditingMessage { get; set; } = null;

    // Pagination state
    public bool CanLoadMore { get; set; } = true;

    // UI state
    public bool IsDarkMode { get; set; } = false;
    public bool ShowSearch { get; set; } = false;
    public string SearchText { get; set; } = "";
    public List<MessageDto> SearchResults { get; set; } = [];
    public bool ShowEmojiPicker { get; set; } = false;
    public bool ShowMentionSuggestions { get; set; } = false;
    public string? ShowReactionPickerForMessageId { get; set; } = null;

    // File upload state
    public List<PendingFile> PendingFiles { get; set; } = [];
    public bool IsUploadingFiles { get; set; } = false;

    // Attachment URL cache - maps storageId to download URL
    public Dictionary<string, string> AttachmentUrlCache { get; set; } = [];

    // Preview URL cache - maps pending file ID to preview object URL (for images before upload)
    public Dictionary<string, string> PendingFilePreviewUrls { get; set; } = [];

    // Reply state
    public MessageDto? ReplyingToMessage { get; set; } = null;
    public Dictionary<string, List<MessageDto>> MessageReplies { get; set; } = []; // parentMessageId -> replies
    public Dictionary<string, int> ReplyCounts { get; set; } = []; // messageId -> reply count
    public Dictionary<string, bool> ShowRepliesForMessage { get; set; } = []; // messageId -> show replies

    // Data collections
    public List<MessageDto> Messages { get; set; } = [];
    public List<OnlineUserDto> OnlineUsers { get; set; } = [];
    public List<string> TypingUsers { get; set; } = [];
    public Dictionary<string, List<ReactionDto>> MessageReactions { get; set; } = [];
    public Dictionary<string, List<MessageReadDto>> MessageReadReceipts { get; set; } = []; // messageId -> read receipts
    public HashSet<string> CurrentUserReadMessages { get; set; } = []; // messageId -> whether current user has read it
    public int UnreadMessageCount { get; set; } = 0; // count of unread messages

    // Computed properties
    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    public readonly List<string> QuickReactions = ["👍", "❤️", "😂", "😮", "😢", "🔥"];

    /// <summary>
    /// Resets all state to initial values (useful for logout).
    /// </summary>
    public void Reset()
    {
        Username = "";
        Messages.Clear();
        MessageText = "";
        OnlineUsers.Clear();
        TypingUsers.Clear();
        HoveredMessageId = null;
        EditingMessage = null;
        SearchText = "";
        SearchResults.Clear();
        MessageReplies.Clear();
        ReplyCounts.Clear();
        ShowRepliesForMessage.Clear();
        MessageReactions.Clear();
        MessageReadReceipts.Clear();
        CurrentUserReadMessages.Clear();
        UnreadMessageCount = 0;
        PendingFiles.Clear();
        AttachmentUrlCache.Clear();
        PendingFilePreviewUrls.Clear();
        ReplyingToMessage = null;
        IsLoading = true;
        IsSending = false;
        IsSavingEdit = false;
        IsLoadingMore = false;
        CanLoadMore = true;
    }
}

