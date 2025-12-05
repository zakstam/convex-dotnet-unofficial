using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Data Transfer Object for file attachments.
/// </summary>
public class AttachmentDto
{
    /// <summary>
    /// Storage ID for the file in Convex file storage.
    /// </summary>
    [JsonPropertyName("storageId")]
    public string StorageId { get; set; } = string.Empty;

    /// <summary>
    /// Original filename.
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "image/png").
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public double Size { get; set; }
}

