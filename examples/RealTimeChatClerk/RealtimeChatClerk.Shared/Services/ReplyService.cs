using Convex.Client;
using Convex.Client.Infrastructure.ErrorHandling;
using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service for reply operations.
/// </summary>
public class ReplyService : IReplyService
{
    private readonly IConvexClient _convexClient;
    private readonly string _getRepliesFunctionName;

    public ReplyService(IConvexClient convexClient, string getRepliesFunctionName = "functions/getReplies")
    {
        _convexClient = convexClient ?? throw new ArgumentNullException(nameof(convexClient));
        _getRepliesFunctionName = getRepliesFunctionName;
    }

    public async Task<List<Message>> LoadRepliesAsync(string parentMessageId)
    {
        try
        {
            var replies = await _convexClient.Query<List<MessageDto>>(_getRepliesFunctionName)
                .WithArgs(new GetRepliesArgs
                {
                    ParentMessageId = parentMessageId
                })
                .ExecuteAsync();

            if (replies != null)
            {
                return replies.Select(m => new Message(
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
            }
        }
        catch (ConvexException ex)
        {
            Console.Error.WriteLine($"Error loading replies: {ex.Message}");
        }

        return [];
    }

    public bool HasReplies(string messageId, Dictionary<string, List<Message>> messageReplies)
    {
        return messageReplies.ContainsKey(messageId) && messageReplies[messageId].Count > 0;
    }

    public List<Message> GetReplies(string messageId, Dictionary<string, List<Message>> messageReplies)
    {
        return messageReplies.TryGetValue(messageId, out var replies) ? replies : [];
    }
}

