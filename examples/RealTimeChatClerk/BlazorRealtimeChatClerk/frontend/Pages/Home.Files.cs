using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RealtimeChat.Frontend.Components;
using RealtimeChatClerk.Shared.Models;
using MessageDto = RealtimeChatClerk.Shared.Models.MessageDto;
using AttachmentDto = RealtimeChatClerk.Shared.Models.AttachmentDto;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        if (string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        var files = e.GetMultipleFiles(10); // Max 10 files
        foreach (var file in files)
        {
            if (file.Size > 50 * 1024 * 1024) // 50MB limit
            {
                Console.Error.WriteLine($"File {file.Name} exceeds 50MB limit");
                continue;
            }

            var fileId = Guid.NewGuid().ToString();
            
            // Create preview URL for images before creating the stream
            string? previewUrl = null;
            if (file.ContentType.StartsWith("image/"))
            {
                try
                {
                    // Read the file into a byte array for creating object URL
                    using var memoryStream = new MemoryStream();
                    using var fileStream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                    await fileStream.CopyToAsync(memoryStream);
                    var bytes = memoryStream.ToArray();

                    // Create object URL using JS interop
                    previewUrl = await JSRuntime.InvokeAsync<string>("createObjectURL", bytes, file.ContentType);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error creating preview for {file.Name}: {ex.Message}");
                }
            }

            // Create the pending file with a new stream
            var pendingFile = new PendingFile(
                Id: fileId,
                Filename: file.Name,
                ContentType: file.ContentType,
                Size: file.Size,
                Content: file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)
            );

            State.PendingFiles.Add(pendingFile);

            // Store preview URL if created
            if (!string.IsNullOrEmpty(previewUrl))
            {
                State.PendingFilePreviewUrls[fileId] = previewUrl;
            }
        }

        StateHasChanged();
    }

    protected async Task RemovePendingFile(string fileId)
    {
        var file = State.PendingFiles.FirstOrDefault(f => f.Id == fileId);
        if (file != null)
        {
            file.Content.Dispose();
            _ = State.PendingFiles.Remove(file);

            // Clean up preview URL if it exists
            if (State.PendingFilePreviewUrls.TryGetValue(fileId, out var previewUrl))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("revokeObjectURL", previewUrl);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error revoking preview URL: {ex.Message}");
                }
                State.PendingFilePreviewUrls.Remove(fileId);
            }
        }
    }

    protected void StartReply(MessageDto message)
    {
        State.ReplyingToMessage = message;
        StateHasChanged();
    }

    protected void CancelReply()
    {
        State.ReplyingToMessage = null;
        StateHasChanged();
    }

    protected async Task LoadReplies(string parentMessageId)
    {
        try
        {
            var replies = await ReplyService.LoadRepliesAsync(parentMessageId);
            // Convert Message to MessageDto
            var replyDtos = replies.Select(m => new MessageDto
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
            State.MessageReplies[parentMessageId] = replyDtos;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading replies: {ex.Message}");
        }
    }

    protected bool HasReplies(string messageId)
    {
        // Convert MessageDto dictionary to Message dictionary for shared service
        var messageRepliesConverted = State.MessageReplies.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(dto => new Message(
                Id: dto.Id,
                Username: dto.Username,
                Text: dto.Text,
                Timestamp: dto.Timestamp,
                EditedAt: dto.EditedAt,
                ParentMessageId: dto.ParentMessageId,
                Attachments: dto.Attachments?.Select(a => new Attachment(
                    StorageId: a.StorageId,
                    Filename: a.Filename,
                    ContentType: a.ContentType,
                    Size: (long)a.Size
                )).ToList()
            )).ToList()
        );
        return ReplyService.HasReplies(messageId, messageRepliesConverted);
    }

    protected async Task ToggleReplies(string messageId)
    {
        if (!State.ShowRepliesForMessage.TryGetValue(messageId, out var showReplies))
        {
            showReplies = false;
        }
        State.ShowRepliesForMessage[messageId] = !showReplies;

        // Load replies if showing and not already loaded
        if (State.ShowRepliesForMessage[messageId] && !State.MessageReplies.ContainsKey(messageId))
        {
            await LoadReplies(messageId);
        }

        StateHasChanged();
    }

    protected List<MessageDto> GetReplies(string messageId)
    {
        // Convert MessageDto dictionary to Message dictionary for shared service
        var messageRepliesConverted = State.MessageReplies.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(dto => new Message(
                Id: dto.Id,
                Username: dto.Username,
                Text: dto.Text,
                Timestamp: dto.Timestamp,
                EditedAt: dto.EditedAt,
                ParentMessageId: dto.ParentMessageId,
                Attachments: dto.Attachments?.Select(a => new Attachment(
                    StorageId: a.StorageId,
                    Filename: a.Filename,
                    ContentType: a.ContentType,
                    Size: (long)a.Size
                )).ToList()
            )).ToList()
        );
        var replies = ReplyService.GetReplies(messageId, messageRepliesConverted);
        // Convert Message back to MessageDto
        return replies.Select(m => new MessageDto
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
}
