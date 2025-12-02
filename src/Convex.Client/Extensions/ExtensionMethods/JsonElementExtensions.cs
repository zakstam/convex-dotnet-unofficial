using System.Text.Json;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for System.Text.Json.JsonElement to simplify common Convex client patterns.
/// These methods reduce boilerplate when working with JsonElement responses from Convex queries.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Checks if the JsonElement is null or undefined.
    /// JsonElement is a struct, so null checks don't work directly.
    /// </summary>
    /// <param name="element">The JsonElement to check.</param>
    /// <returns>True if the element is null or undefined, false otherwise.</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getData", args);
    /// if (result.IsNullOrUndefined()) return;
    /// // Process result...
    /// </code>
    /// </example>
    public static bool IsNullOrUndefined(this JsonElement element) => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    /// <summary>
    /// Unwraps the "value" property if present, otherwise returns the element itself.
    /// Convex sometimes wraps responses in a "value" property, and this helper extracts it.
    /// </summary>
    /// <param name="element">The JsonElement to unwrap.</param>
    /// <returns>The unwrapped JsonElement (either the "value" property or the original element).</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getData", args);
    /// var data = result.UnwrapValue();
    /// // data now contains the actual response data, not wrapped in "value"
    /// </code>
    /// </example>
    public static JsonElement UnwrapValue(this JsonElement element) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var valueProp) ? valueProp : element;

    /// <summary>
    /// Deserializes the JsonElement to a strongly-typed object.
    /// Uses System.Text.Json for deserialization with default options.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="element">The JsonElement to deserialize.</param>
    /// <param name="options">Optional JsonSerializerOptions. If null, uses default options.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getUser", args);
    /// var user = result.UnwrapValue().Deserialize&lt;User&gt;();
    /// if (user != null) { /* Use user */ }
    /// </code>
    /// </example>
    public static T? Deserialize<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes the JsonElement to a strongly-typed object, throwing an exception on failure.
    /// Use this when you expect deserialization to always succeed.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="element">The JsonElement to deserialize.</param>
    /// <param name="options">Optional JsonSerializerOptions. If null, uses default options.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getUser", args);
    /// var user = result.UnwrapValue().DeserializeOrThrow&lt;User&gt;();
    /// // Use user - guaranteed to be non-null
    /// </code>
    /// </example>
    public static T DeserializeOrThrow<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), options)
            ?? throw new JsonException($"Failed to deserialize JsonElement to {typeof(T).Name}");
    }

    /// <summary>
    /// Gets a property value as a string, returning null if the property doesn't exist or is null.
    /// </summary>
    /// <param name="element">The JsonElement to get the property from.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <returns>The property value as a string, or null if not found or null.</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getData", args);
    /// var name = result.GetStringProperty("name");
    /// </code>
    /// </example>
    public static string? GetStringProperty(this JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop)
            ? prop.ValueKind == JsonValueKind.Null ? null : prop.GetString()
            : null;
    }

    /// <summary>
    /// Gets a property value as an integer, returning null if the property doesn't exist or is null.
    /// </summary>
    /// <param name="element">The JsonElement to get the property from.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <returns>The property value as an integer, or null if not found or null.</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getData", args);
    /// var count = result.GetInt32Property("count");
    /// </code>
    /// </example>
    public static int? GetInt32Property(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (prop.ValueKind == JsonValueKind.Number)
            {
                try
                {
                    return prop.GetInt32();
                }
                catch
                {
                    return (int)prop.GetDouble();
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a property value as a boolean, returning null if the property doesn't exist or is null.
    /// </summary>
    /// <param name="element">The JsonElement to get the property from.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <returns>The property value as a boolean, or null if not found or null.</returns>
    /// <example>
    /// <code>
    /// var result = await ConvexClient.QueryAsync&lt;JsonElement&gt;("functions/getData", args);
    /// var isActive = result.GetBooleanProperty("isActive");
    /// </code>
    /// </example>
    public static bool? GetBooleanProperty(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (prop.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (prop.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }
        return null;
    }
}

