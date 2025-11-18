using Microsoft.JSInterop;
using RealtimeChat.Frontend.Components;
using RealtimeChatClerk.Shared.Models;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected string GetUserInitial(string username) => string.IsNullOrWhiteSpace(username) ? "?" : username[..1].ToUpperInvariant();

    protected string GetUserColor(string username)
    {
        try
        {
            var valueTask = JSRuntime.InvokeAsync<string>("userColors.getColor", username);
            if (valueTask.IsCompletedSuccessfully)
            {
                return valueTask.Result ?? "#667eea";
            }
            return valueTask.AsTask().GetAwaiter().GetResult() ?? "#667eea";
        }
        catch
        {
            return "#667eea";
        }
    }

    protected string RenderMarkdown(string text)
    {
        try
        {
            var valueTask = JSRuntime.InvokeAsync<string>("markdownParser.parse", text);
            if (valueTask.IsCompletedSuccessfully)
            {
                return valueTask.Result ?? text;
            }
            return valueTask.AsTask().GetAwaiter().GetResult() ?? text;
        }
        catch
        {
            return text;
        }
    }

    protected List<OnlineUserDto> GetMentionSuggestions()
    {
        if (string.IsNullOrWhiteSpace(State.MessageText) || !State.MessageText.Contains('@'))
        {
            return [];
        }

        var lastAt = State.MessageText.LastIndexOf('@');
        if (lastAt < 0)
        {
            return [];
        }

        var query = State.MessageText[(lastAt + 1)..].Trim();
        // Show all users including self for mentions
        return string.IsNullOrWhiteSpace(query)
            ? [.. State.OnlineUsers]
            : [.. State.OnlineUsers.Where(u => u.Username.StartsWith(query, StringComparison.OrdinalIgnoreCase))];
    }

    protected string FormatTimestamp(long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
        var now = DateTime.Now;
        var timeSpan = now - date;

        if (timeSpan.TotalMinutes < 1)
        {
            return "Just now";
        }

        if (timeSpan.TotalHours < 1)
        {
            return $"{(int)timeSpan.TotalMinutes}m ago";
        }

        if (timeSpan.TotalDays < 1)
        {
            return date.ToString("HH:mm");
        }

        return timeSpan.TotalDays < 7 ? date.ToString("ddd HH:mm") : date.ToString("MMM dd, HH:mm");
    }

    protected async Task ScrollToBottom()
    {
        try
        {
            if (messagesListRef != null)
            {
                await messagesListRef.ScrollToBottom(JSRuntime);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error scrolling to bottom: {ex.Message}");
        }
    }

    protected async Task<bool> IsNearBottomAsync()
    {
        try
        {
            if (messagesListRef != null)
            {
                return await messagesListRef.IsNearBottomAsync(JSRuntime);
            }
        }
        catch { }
        return true; // Default to true if check fails (safer to scroll)
    }

    protected string FormatFileSize(long bytes) => FileService.FormatFileSize(bytes);

    protected string GetAttachmentUrlSync(string storageId)
    {
        // First check the state cache (which is updated when messages are loaded)
        if (!string.IsNullOrEmpty(storageId) && State.AttachmentUrlCache.TryGetValue(storageId, out var cachedUrl) && !string.IsNullOrEmpty(cachedUrl))
        {
            return cachedUrl;
        }
        
        // Fall back to FileService cache
        return FileService.GetAttachmentUrlSync(storageId);
    }
}
