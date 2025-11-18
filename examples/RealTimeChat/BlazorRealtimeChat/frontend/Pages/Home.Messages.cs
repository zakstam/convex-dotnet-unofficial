using Microsoft.JSInterop;
using RealtimeChat.Frontend.Components;
using RealtimeChat.Frontend.Services;
using RealtimeChat.Shared.Models;
using MessageDto = RealtimeChat.Shared.Models.MessageDto;
using AttachmentDto = RealtimeChat.Shared.Models.AttachmentDto;
using System.Threading;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task LoadMessages()
    {
        await ChatService.LoadMessagesAsync();
        
        // Load attachment URLs for messages with attachments
        if (State.Messages.Count > 0)
        {
            _ = LoadAttachmentUrlsForMessagesAsync(State.Messages);
        }

        // Load reactions initially and subscribe for real-time updates
        if (State.Messages.Count > 0)
        {
            var messageIds = State.Messages.Select(m => m.Id).ToList();
            await ReactionService.LoadReactionsAsync(messageIds);
            // Sync state immediately after loading
            State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
            StateHasChanged();
            
            // Set up subscription - it will update state when reactions change
            ReactionService.SubscribeToReactions(messageIds, OnReactionsReceived, OnReactionsError);

            // Load read receipts and subscribe for real-time updates
            await LoadReadReceipts();
        }

        StateHasChanged();
    }

    protected async Task LoadMoreMessages()
    {
        await ChatService.LoadMoreMessagesAsync();

        // Load attachment URLs for new messages
        if (State.Messages.Count > 0)
        {
            _ = LoadAttachmentUrlsForMessagesAsync(State.Messages);
        }

        // Load reactions for new messages and resubscribe
        if (State.Messages.Count > 0)
        {
            var messageIds = State.Messages.Select(m => m.Id).ToList();
            await ReactionService.LoadReactionsAsync(messageIds);
            // Sync state immediately after loading
            State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
            StateHasChanged();
            
            // Set up subscription - it will update state when reactions change
            ReactionService.SubscribeToReactions(messageIds, OnReactionsReceived, OnReactionsError);
        }

        StateHasChanged();
    }

    protected void SubscribeToMessages()
    {
        // Subscription is handled automatically by ChatService via MessagesUpdated event
        // This method is kept for compatibility but does nothing
    }

    protected async Task SendMessage()
    {
        if ((string.IsNullOrWhiteSpace(State.MessageText) && State.PendingFiles.Count == 0) || string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        try
        {
            // Stop typing indicator
            PresenceService.HandleTypingInput("");

            StateHasChanged();

            var messageText = State.MessageText?.Trim() ?? "";

            // Upload files if any
            List<Attachment>? attachments = null;
            if (State.PendingFiles.Count > 0)
            {
                State.IsUploadingFiles = true;
                try
                {
                    attachments = await FileService.UploadFilesAsync(State.PendingFiles);
                    if (attachments.Count == 0 && State.PendingFiles.Count > 0)
                    {
                        // If file upload failed, don't send message
                        StateHasChanged();
                        return;
                    }
                    
                    // Immediately cache URLs for uploaded attachments so they display right away
                    foreach (var attachment in attachments)
                    {
                        if (!string.IsNullOrEmpty(attachment.StorageId))
                        {
                            var url = await FileService.GetAttachmentUrlAsync(attachment.StorageId);
                            if (!string.IsNullOrEmpty(url))
                            {
                                State.AttachmentUrlCache[attachment.StorageId] = url;
                            }
                        }
                    }
                    
                    // Clean up preview URLs for sent files
                    foreach (var pendingFile in State.PendingFiles)
                    {
                        if (State.PendingFilePreviewUrls.TryGetValue(pendingFile.Id, out var previewUrl))
                        {
                            try
                            {
                                await JSRuntime.InvokeVoidAsync("revokeObjectURL", previewUrl);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Error revoking preview URL: {ex.Message}");
                            }
                            State.PendingFilePreviewUrls.Remove(pendingFile.Id);
                        }
                    }
                    
                    State.PendingFiles.Clear();
                }
                finally
                {
                    State.IsUploadingFiles = false;
                }
            }

            // Check if we're replying to a message
            if (State.ReplyingToMessage != null)
            {
                await ChatService.SendReplyAsync(messageText, State.ReplyingToMessage.Id, attachments);
                State.ReplyingToMessage = null;
            }
            else
            {
                var success = await ChatService.SendMessageAsync(messageText, attachments);
                if (!success)
                {
                    Console.Error.WriteLine("Failed to send message");
                }
            }

            State.MessageText = "";
            Console.WriteLine($"Message sent successfully: {messageText}");

            // Scroll to bottom after sending a message
            await Task.Delay(100); // Wait for message to appear via subscription
            await ScrollToBottom();

            await Task.Delay(50);
            await JSRuntime.InvokeVoidAsync("focusElement", messageInputRef);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error sending message: {ex}");
            State.MessageText = "";
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected void StartEditMessage(MessageDto message)
    {
        State.EditingMessage = message;
        State.EditMessageText = message.Text;
    }

    protected void CancelEdit()
    {
        State.EditingMessage = null;
        State.EditMessageText = "";
    }

    protected async Task SaveEditMessage()
    {
        if (State.EditingMessage == null || string.IsNullOrWhiteSpace(State.EditMessageText))
        {
            return;
        }

        try
        {
            await ChatService.EditMessageAsync(State.EditingMessage.Id, State.EditMessageText.Trim());

            CancelEdit();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error editing message: {ex}");
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected async Task DeleteMessage(string messageId)
    {
        try
        {
            await ChatService.DeleteMessageAsync(messageId);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting message: {ex.Message}");
        }
    }

    protected void ShowMessageMenu(string messageId) => State.HoveredMessageId = messageId;

    protected void HideMessageMenu() => State.HoveredMessageId = null;

    protected async Task CopyMessage(string text)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("copyToClipboard", text);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error copying message: {ex.Message}");
        }
    }

    private void OnMessagesReceived(List<MessageDto> messages)
    {
        // Note: State.Messages is already updated by OnPaginationItemsUpdated in ChatService
        // This callback is just for additional processing (attachments, reactions, etc.)
        // The actual state update happens via the MessagesUpdated event

        // Ensure we're on the UI thread
        _ = InvokeAsync(async () =>
        {
            // State.Messages is already updated by ChatService.OnPaginationItemsUpdated
            // We just need to handle side effects like loading attachments and reactions
            
            // Load attachment URLs for messages
            await LoadAttachmentUrlsForMessagesAsync(messages);

            // Load reactions and resubscribe with updated message list
            if (messages.Count > 0)
            {
                var messageIds = messages.Select(m => m.Id).ToList();
                await ReactionService.LoadReactionsAsync(messageIds);
                State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
                ReactionService.SubscribeToReactions(messageIds, OnReactionsReceived, OnReactionsError);
            }

            // StateHasChanged is already called by OnMessagesUpdated, but we call it here too
            // to ensure UI updates after side effects are processed
            StateHasChanged();
        });
    }

    private void OnMessagesError(string error)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnReactionsReceived(Dictionary<string, List<ReactionDto>> reactions)
    {
        State.MessageReactions = CloneReactions(reactions);
        InvokeAsync(StateHasChanged);
    }

    private static Dictionary<string, List<ReactionDto>> CloneReactions(Dictionary<string, List<ReactionDto>> reactions)
    {
        var cloned = new Dictionary<string, List<ReactionDto>>();
        foreach (var kvp in reactions)
        {
            cloned[kvp.Key] = kvp.Value.Select(r => new ReactionDto
            {
                Emoji = r.Emoji,
                Count = r.Count,
                Users = r.Users?.ToList() ?? []
            }).ToList();
        }
        return cloned;
    }

    private void OnReactionsError(string error)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task LoadAttachmentUrlsForMessagesAsync(List<MessageDto> messages)
    {
        // Convert MessageDto to Message for shared service
        var domainMessages = messages.Select(m => new Message(
            Id: m.Id,
            Username: m.Username,
            Text: m.Text,
            Timestamp: m.Timestamp,
            EditedAt: m.EditedAt,
            ParentMessageId: m.ParentMessageId,
            Attachments: m.Attachments?.Select(a => new Attachment(
                StorageId: a.StorageId,
                Filename: a.Filename,
                ContentType: a.ContentType,
                Size: (long)a.Size
            )).ToList()
        )).ToList();
        
        await FileService.LoadAttachmentUrlsForMessagesAsync(domainMessages);
        
        // Update cache in state from shared service cache
        foreach (var msg in messages)
        {
            if (msg.Attachments != null)
            {
                foreach (var att in msg.Attachments)
                {
                    var url = FileService.GetAttachmentUrlSync(att.StorageId);
                    if (!string.IsNullOrEmpty(url))
                    {
                        State.AttachmentUrlCache[att.StorageId] = url;
                    }
                }
            }
        }
    }
}
