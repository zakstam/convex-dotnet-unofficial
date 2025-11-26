namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// Provides serialization and deserialization of values to/from Convex's JSON format.
/// This interface abstracts serialization logic to enable testing and custom implementations.
/// </summary>
public interface IConvexSerializer
{
    /// <summary>
    /// Serializes a .NET value to Convex's JSON format.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The Convex JSON representation.</returns>
    string Serialize<T>(T? value);

    /// <summary>
    /// Deserializes a Convex JSON string to a .NET value.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The Convex JSON string.</param>
    /// <returns>The deserialized value, or default(T) if deserialization fails.</returns>
    T? Deserialize<T>(string json);

    /// <summary>
    /// Deserializes a Convex JSON string to a .NET value of the specified type.
    /// </summary>
    /// <param name="json">The Convex JSON string.</param>
    /// <param name="type">The target type.</param>
    /// <returns>The deserialized value, or null if deserialization fails.</returns>
    object? Deserialize(string json, Type type);
}
