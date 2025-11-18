using Microsoft.AspNetCore.Components;
using RealtimeChat.Frontend.Components;
using RealtimeChat.Shared.Models;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    // Component references
    protected MessagesList? messagesListRef;
    protected ElementReference messageInputRef;
    protected ElementReference searchInputRef;

    // Computed properties
    protected string AppVersion => State.AppVersion;
    protected List<MessageGroup> GroupedMessages => ChatService.GetGroupedMessages(State.Username);
    protected List<OnlineUserDto> MentionSuggestions => GetMentionSuggestions();
}
