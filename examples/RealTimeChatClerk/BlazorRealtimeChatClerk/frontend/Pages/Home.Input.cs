using Microsoft.AspNetCore.Components.Web;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !string.IsNullOrWhiteSpace(State.MessageText))
        {
            await SendMessage();
        }
    }

    protected async Task HandleEditKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && e.CtrlKey && !string.IsNullOrWhiteSpace(State.EditMessageText))
        {
            await SaveEditMessage();
        }
        else if (e.Key == "Escape")
        {
            CancelEdit();
        }
    }

    protected void ToggleEmojiPicker()
    {
        State.ShowEmojiPicker = !State.ShowEmojiPicker;
        StateHasChanged();
    }

    protected void InsertEmoji(string emoji)
    {
        State.MessageText += emoji;
        State.ShowEmojiPicker = false;
        StateHasChanged();
    }

    protected void HandleMentionInput()
    {
        State.ShowMentionSuggestions = State.MessageText.Contains('@');
        StateHasChanged();
    }

    protected void InsertMention(string username)
    {
        var lastAt = State.MessageText.LastIndexOf('@');
        if (lastAt >= 0)
        {
            State.MessageText = string.Concat(State.MessageText.AsSpan(0, lastAt), $"@{username} ");
        }
        State.ShowMentionSuggestions = false;
        StateHasChanged();
    }
}
