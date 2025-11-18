using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for SetTyping mutation.
/// Note: Username is not needed as it's extracted from the authenticated user in the Convex function.
/// </summary>
public class SetTypingArgs
{
    [JsonPropertyName("isTyping")]
    public bool IsTyping { get; set; }
}

