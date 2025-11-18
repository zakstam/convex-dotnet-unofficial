using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Shared.Serialization;

/// <summary>
/// Default implementation of <see cref="IConvexSerializer"/> that uses the Convex serialization format.
/// This implementation wraps the existing Convex.Client.Serialization.ConvexSerializer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultConvexSerializer"/> class.
/// </remarks>
/// <param name="logger">Optional logger for diagnostic information. If null, a NullLogger is used.</param>
public class DefaultConvexSerializer(ILogger<DefaultConvexSerializer>? logger = null) : IConvexSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new ConvexInt64JsonConverter() }
    };

    private readonly ILogger<DefaultConvexSerializer>? _logger = logger;

    /// <inheritdoc/>
    public string Serialize<T>(T? value) =>
        // Use the existing ConvexSerializer for now
        // TODO: Refactor ConvexSerializer to be instance-based
        ConvexSerializer.SerializeToConvexJson(value);

    /// <inheritdoc/>
    public T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            var targetTypeName = typeof(T).Name;
            var jsonPreview = json[..Math.Min(500, json.Length)];
            var message = $"Failed to deserialize JSON to {targetTypeName}. " +
                         $"Error: {ex.Message}. " +
                         $"JSON content (first 500 chars): {jsonPreview}";

            _logger?.LogError(ex, "JSON deserialization failed for type {TypeName}. JSON content: {JsonContent}",
                targetTypeName, jsonPreview);

            throw new InvalidOperationException(message, ex);
        }
    }

    /// <inheritdoc/>
    public object? Deserialize(string json, Type type)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, type, _jsonOptions);
        }
        catch (JsonException ex)
        {
            var targetTypeName = type.Name;
            var jsonPreview = json[..Math.Min(500, json.Length)];
            var message = $"Failed to deserialize JSON to {targetTypeName}. " +
                         $"Error: {ex.Message}. " +
                         $"JSON content (first 500 chars): {jsonPreview}";

            _logger?.LogError(ex, "JSON deserialization failed for type {TypeName}. JSON content: {JsonContent}",
                targetTypeName, jsonPreview);

            throw new InvalidOperationException(message, ex);
        }
    }
}
