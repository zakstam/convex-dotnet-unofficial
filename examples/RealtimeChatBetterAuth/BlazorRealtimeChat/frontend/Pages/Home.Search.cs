using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RealtimeChat.Shared.Models;
using MessageDto = RealtimeChat.Shared.Models.MessageDto;
using AttachmentDto = RealtimeChat.Shared.Models.AttachmentDto;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task ToggleDarkMode()
    {
        State.IsDarkMode = !State.IsDarkMode;
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "darkMode", State.IsDarkMode);
        }
        catch { }
        StateHasChanged();
    }

    protected void ToggleSearch()
    {
        State.ShowSearch = !State.ShowSearch;
        if (State.ShowSearch)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                await JSRuntime.InvokeVoidAsync("focusElement", searchInputRef);
            });
        }
        else
        {
            ClearSearch();
        }
        StateHasChanged();
    }

    protected async Task HandleSearch()
    {
        if (string.IsNullOrWhiteSpace(State.SearchText))
        {
            State.SearchResults.Clear();
            StateHasChanged();
            return;
        }

        try
        {
            var results = await ChatService.SearchMessagesAsync(State.SearchText, 20);
            // Convert Message to MessageDto for consistency
            State.SearchResults = results.Select(m => new MessageDto
            {
                Id = m.Id,
                Username = m.Username,
                Text = m.Text,
                Timestamp = m.Timestamp,
                EditedAt = m.EditedAt,
                ParentMessageId = m.ParentMessageId,
                Attachments = m.Attachments?.Select(a => new AttachmentDto
                {
                    StorageId = a.StorageId,
                    Filename = a.Filename,
                    ContentType = a.ContentType,
                    Size = (ulong)a.Size
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error searching messages: {ex.Message}");
        }

        StateHasChanged();
    }

    protected void HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            ClearSearch();
            ToggleSearch();
        }
    }

    protected void ClearSearch()
    {
        State.SearchText = "";
        State.SearchResults.Clear();
        StateHasChanged();
    }

    protected async Task ScrollToMessage(string messageId)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("scrollToElement", $"message-{messageId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error scrolling to message: {ex.Message}");
        }
    }

    /// <summary>
    /// Scrolls to the latest unread message (the oldest unread message chronologically).
    /// </summary>
    protected async Task ScrollToLatestUnreadMessage()
    {
        if (State.Messages.Count == 0 || string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        try
        {
            // Find the oldest unread message (first unread message chronologically)
            var unreadMessages = State.Messages
                .Where(m => m.Username != State.Username && !State.CurrentUserReadMessages.Contains(m.Id))
                .OrderBy(m => m.Timestamp)
                .ToList();

            if (unreadMessages.Count > 0)
            {
                var latestUnreadMessage = unreadMessages.First();
                await ScrollToMessage(latestUnreadMessage.Id);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error scrolling to latest unread message: {ex.Message}");
        }
    }
}
