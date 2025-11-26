using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Infrastructure.ErrorHandling;
using System.Text.Json;
using RealtimeChatClerk.Shared.Models;
using Microsoft.Extensions.Logging;

namespace RealtimeChatClerk.Shared.Services;

/// <summary>
/// Service for read receipt operations.
/// </summary>
public class ReadReceiptService : IReadReceiptService, IDisposable
{
    private readonly IConvexClient _convexClient;
    private readonly ILogger<ReadReceiptService>? _logger;
    private readonly SubscriptionTracker _subscriptions = new();
    private readonly string _markMessageReadFunctionName;
    private readonly string _getMessageReadsFunctionName;
    private IDisposable? _readReceiptsSubscriptionDisposable;
    private bool _isFirstSubscriptionUpdate = true;

    public Dictionary<string, List<MessageReadDto>> MessageReadReceipts { get; private set; } = [];
    public event EventHandler? ReadReceiptsUpdated;

    public ReadReceiptService(IConvexClient convexClient, string markMessageReadFunctionName = "functions/markMessageRead", string getMessageReadsFunctionName = "functions/getMessageReads", ILogger<ReadReceiptService>? logger = null)
    {
        _convexClient = convexClient ?? throw new ArgumentNullException(nameof(convexClient));
        _markMessageReadFunctionName = markMessageReadFunctionName;
        _getMessageReadsFunctionName = getMessageReadsFunctionName;
        _logger = logger;
    }

    public async Task MarkMessageAsReadAsync(string messageId, string username)
    {
        // Username parameter is kept for validation/logging but not sent to Convex
        // The Convex function extracts username from the authenticated user
        if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(username))
        {
            _logger?.LogWarning("MarkMessageAsReadAsync: Invalid inputs: messageId={MessageId}, username={Username}", messageId, username);
            return;
        }

        try
        {
            _logger?.LogDebug("MarkMessageAsReadAsync: Marking message {MessageId} as read by {Username}", messageId, username);

            await _convexClient.Mutate<object>(_markMessageReadFunctionName)
                .WithArgs(new MarkMessageReadArgs
                {
                    MessageId = messageId
                })
                .OnError(ex => _logger?.LogError(ex, "Error marking message as read: {Message}", ex.Message))
                .ExecuteAsync();

            _logger?.LogDebug("MarkMessageAsReadAsync: Successfully marked message {MessageId} as read", messageId);
        }
        catch (ConvexException ex)
        {
            _logger?.LogError(ex, "Error marking message as read: {Message}", ex.Message);
        }
    }

    public async Task LoadReadReceiptsAsync(List<string> messageIds)
    {
        if (messageIds.Count == 0)
        {
            _logger?.LogDebug("LoadReadReceiptsAsync: No message IDs provided");
            return;
        }

        try
        {
            _logger?.LogInformation("LoadReadReceiptsAsync: Loading read receipts for {Count} messages", messageIds.Count);

            var result = await _convexClient.Query<JsonElement>(_getMessageReadsFunctionName)
                .WithArgs(new GetMessageReadsArgs { MessageIds = messageIds })
                .ExecuteAsync();

            if (result.IsNullOrUndefined())
            {
                _logger?.LogWarning("LoadReadReceiptsAsync: Result is null/undefined");
                return;
            }

            var readsData = result.UnwrapValue();

            if (readsData.ValueKind == JsonValueKind.Object)
            {
                var reads = ParseReadReceiptsFromJson(readsData);
                _logger?.LogInformation("LoadReadReceiptsAsync: Parsed {Count} message read receipts", reads.Count);

                foreach (var kvp in reads)
                {
                    MessageReadReceipts[kvp.Key] = kvp.Value;
                    _logger?.LogDebug("LoadReadReceiptsAsync: Message {MessageId} has {ReadCount} read receipts", kvp.Key, kvp.Value.Count);
                }

                // Remove read receipts for messages that are no longer in the list
                var messagesToRemove = MessageReadReceipts.Keys
                    .Where(id => !messageIds.Contains(id))
                    .ToList();
                foreach (var messageId in messagesToRemove)
                {
                    _ = MessageReadReceipts.Remove(messageId);
                }

                _logger?.LogInformation("LoadReadReceiptsAsync: Final MessageReadReceipts count: {Count}", MessageReadReceipts.Count);
                ReadReceiptsUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _logger?.LogWarning("LoadReadReceiptsAsync: Unexpected JSON value kind: {ValueKind}", readsData.ValueKind);
            }
        }
        catch (ConvexException ex)
        {
            _logger?.LogError(ex, "Error loading read receipts");
        }
    }

    public void SubscribeToReadReceipts(List<string> messageIds, Action<Dictionary<string, List<MessageReadDto>>> onUpdate, Action<string>? onError = null)
    {
        if (messageIds.Count == 0)
        {
            _logger?.LogDebug("SubscribeToReadReceipts: No message IDs provided");
            return;
        }

        _logger?.LogInformation("SubscribeToReadReceipts: Setting up subscription for {Count} messages", messageIds.Count);

        if (_readReceiptsSubscriptionDisposable != null)
        {
            _logger?.LogDebug("SubscribeToReadReceipts: Disposing old subscription");
            _ = _subscriptions.Remove(_readReceiptsSubscriptionDisposable);
            _readReceiptsSubscriptionDisposable?.Dispose();
        }

        _isFirstSubscriptionUpdate = true;

        _readReceiptsSubscriptionDisposable = _convexClient
            .CreateResilientSubscription<JsonElement>(_getMessageReadsFunctionName, new GetMessageReadsArgs { MessageIds = messageIds })
            .Subscribe(
                result =>
                {
                    _logger?.LogInformation("=== Read receipts subscription callback FIRED (isFirstUpdate: {IsFirstUpdate}) ===", _isFirstSubscriptionUpdate);

                    if (result.IsNullOrUndefined())
                    {
                        _logger?.LogWarning("Read receipts subscription callback: Result is null/undefined");
                        return;
                    }

                    var readsData = result.UnwrapValue();
                    if (readsData.ValueKind != JsonValueKind.Object)
                    {
                        _logger?.LogWarning("Read receipts subscription callback: Unexpected JSON value kind: {ValueKind}", readsData.ValueKind);
                        return;
                    }

                    var reads = ParseReadReceiptsFromJson(readsData);
                    _logger?.LogInformation("Read receipts subscription callback: Parsed {Count} message read receipts", reads.Count);

                    // Always update MessageReadReceipts from server data
                    var anyChanges = false;
                    foreach (var kvp in reads)
                    {
                        var hasChanged = !MessageReadReceipts.ContainsKey(kvp.Key);

                        if (!hasChanged)
                        {
                            var existing = MessageReadReceipts[kvp.Key];
                            var newReads = kvp.Value;
                            _logger?.LogDebug("Read receipts subscription callback: Message {MessageId} - existing reads: {ExistingCount}, new reads: {NewCount}", kvp.Key, existing.Count, newReads.Count);

                            if (existing.Count != newReads.Count)
                            {
                                hasChanged = true;
                                _logger?.LogDebug("Read receipts subscription callback: Message {MessageId} - count mismatch detected", kvp.Key);
                            }
                            else
                            {
                                // Check if any usernames or readAt timestamps changed
                                foreach (var newRead in newReads)
                                {
                                    var existingRead = existing.FirstOrDefault(r => r.Username == newRead.Username);
                                    if (existingRead == null || existingRead.ReadAt != newRead.ReadAt)
                                    {
                                        hasChanged = true;
                                        _logger?.LogDebug("Read receipts subscription callback: Message {MessageId} - read receipt changed for user {Username}", kvp.Key, newRead.Username);
                                        break;
                                    }
                                }
                            }
                        }

                        if (hasChanged)
                        {
                            _logger?.LogDebug("Read receipts subscription callback: Updating MessageReadReceipts for {MessageId}", kvp.Key);
                            MessageReadReceipts[kvp.Key] = kvp.Value;
                            anyChanges = true;
                        }
                        else
                        {
                            _logger?.LogDebug("Read receipts subscription callback: No changes for message {MessageId}", kvp.Key);
                        }
                    }

                    // Remove read receipts for messages that are no longer in the subscription list
                    var messagesToRemove = MessageReadReceipts.Keys
                        .Where(id => !messageIds.Contains(id))
                        .ToList();
                    if (messagesToRemove.Count > 0)
                    {
                        foreach (var messageId in messagesToRemove)
                        {
                            _ = MessageReadReceipts.Remove(messageId);
                        }
                        anyChanges = true;
                    }

                    // Always notify on first update to ensure initial state is synced, or if there are changes
                    if (anyChanges || _isFirstSubscriptionUpdate)
                    {
                        _logger?.LogInformation("Read receipts subscription callback: Notifying update (anyChanges: {AnyChanges}, isFirstUpdate: {IsFirstUpdate})", anyChanges, _isFirstSubscriptionUpdate);
                        _logger?.LogDebug("Read receipts subscription callback: MessageReadReceipts count: {Count}", MessageReadReceipts.Count);
                        _logger?.LogInformation("Read receipts subscription callback: Calling onUpdate callback with {Count} message read receipts", MessageReadReceipts.Count);
                        _isFirstSubscriptionUpdate = false;
                        try
                        {
                            onUpdate(MessageReadReceipts);
                            _logger?.LogInformation("Read receipts subscription callback: onUpdate callback completed successfully");
                        }
                        catch (ConvexException ex)
                        {
                            _logger?.LogError(ex, "Read receipts subscription callback: Error in onUpdate callback");
                        }
                        ReadReceiptsUpdated?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        _logger?.LogDebug("Read receipts subscription callback: No changes detected, skipping update");
                    }
                },
                error =>
                {
                    var errorMessage = error is Exception ex ? ex.Message : error?.ToString() ?? "Unknown error";
                    _logger?.LogError(error is Exception ex2 ? ex2 : null, "Read receipts subscription error: {ErrorMessage}", errorMessage);
                    onError?.Invoke(errorMessage);
                }
            );

        _logger?.LogInformation("SubscribeToReadReceipts: Subscription created successfully");
        _ = _subscriptions.Add(_readReceiptsSubscriptionDisposable);
    }

    private Dictionary<string, List<MessageReadDto>> ParseReadReceiptsFromJson(JsonElement readsData)
    {
        var reads = new Dictionary<string, List<MessageReadDto>>();

        foreach (var property in readsData.EnumerateObject())
        {
            var messageId = property.Name;
            var readsList = new List<MessageReadDto>();

            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var readElement in property.Value.EnumerateArray())
                {
                    var usernameValue = readElement.GetProperty("username").GetString() ?? "";
                    var readAtValue = readElement.GetProperty("readAt");

                    long readAt;
                    if (readAtValue.ValueKind == JsonValueKind.Number)
                    {
                        try
                        {
                            readAt = readAtValue.GetInt64();
                        }
                        catch
                        {
                            readAt = (long)readAtValue.GetDouble();
                        }
                    }
                    else
                    {
                        readAt = 0;
                    }

                    var read = new MessageReadDto
                    {
                        Username = usernameValue,
                        ReadAt = readAt
                    };
                    readsList.Add(read);
                }
            }

            reads[messageId] = readsList;
        }

        return reads;
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        GC.SuppressFinalize(this);
    }
}

