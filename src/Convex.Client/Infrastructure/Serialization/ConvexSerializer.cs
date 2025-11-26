using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// Provides serialization and deserialization of values to/from Convex's JSON format.
/// This implementation must exactly match the TypeScript client's behavior.
/// </summary>
public static class ConvexSerializer
{
    /// <summary>
    /// Serializes a .NET value to Convex's JSON format.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The Convex JSON representation.</returns>
    public static string SerializeToConvexJson<T>(T? value) => SerializeToConvexJson(value, []);

    /// <summary>
    /// Serializes a .NET value to Convex's JSON format with cycle detection.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="visited">Set of already visited objects to prevent cycles.</param>
    /// <returns>The Convex JSON representation.</returns>
    private static string SerializeToConvexJson<T>(T? value, HashSet<object> visited)
    {
        return value switch
        {
            null => "null",
            long longValue => SerializeBigInt(longValue),
            double doubleValue when RequiresSpecialEncoding(doubleValue) => SerializeSpecialFloat(doubleValue),
            double doubleValue => doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            byte[] bytes => SerializeBytes(bytes),
            bool boolValue => boolValue ? "true" : "false",
            string stringValue => SerializeString(stringValue),
            DateTime dateTime => ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds().ToString(),
            Enum enumValue => SerializeString(enumValue.ToString()),
            _ => SerializeComplexObject(value, visited)
        };
    }

    /// <summary>
    /// Serializes a string to JSON format, matching JavaScript's JSON.stringify() behavior.
    /// Preserves Unicode characters (including emoji) while escaping only necessary control characters.
    /// </summary>
    /// <param name="value">The string to serialize.</param>
    /// <returns>The JSON string representation.</returns>
    private static string SerializeString(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        _ = sb.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    _ = sb.Append("\\\"");
                    break;
                case '\\':
                    _ = sb.Append("\\\\");
                    break;
                case '\b':
                    _ = sb.Append("\\b");
                    break;
                case '\f':
                    _ = sb.Append("\\f");
                    break;
                case '\n':
                    _ = sb.Append("\\n");
                    break;
                case '\r':
                    _ = sb.Append("\\r");
                    break;
                case '\t':
                    _ = sb.Append("\\t");
                    break;
                default:
                    // Only escape control characters (U+0000 to U+001F)
                    if (c < 0x20)
                    {
                        _ = sb.Append("\\u");
                        _ = sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        // Preserve all other characters including Unicode/emoji
                        _ = sb.Append(c);
                    }
                    break;
            }
        }

        _ = sb.Append('"');
        return sb.ToString();
    }

    /// <summary>
    /// Serializes a 64-bit integer to Convex's BigInt format.
    /// </summary>
    /// <param name="value">The integer value to serialize.</param>
    /// <returns>The Convex JSON representation with $integer field.</returns>
    private static string SerializeBigInt(long value)
    {
        // Convert to little-endian bytes (matching TypeScript's behavior)
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);

        // Base64 encode the bytes
        var base64 = Convert.ToBase64String(bytes);

        return $"{{\"$integer\":\"{base64}\"}}";
    }

    /// <summary>
    /// Serializes special float values (NaN, Infinity, -0) to Convex's format.
    /// </summary>
    /// <param name="value">The double value to serialize.</param>
    /// <returns>The Convex JSON representation with $float field.</returns>
    private static string SerializeSpecialFloat(double value)
    {
        // Convert to little-endian IEEE 754 bytes (matching TypeScript's behavior)
        Span<byte> bytes = stackalloc byte[8];

        if (double.IsNaN(value))
        {
            // TypeScript produces positive quiet NaN (0x7FF8000000000000)
            // C# double.NaN produces negative quiet NaN (0xFFF8000000000000)
            // Force compatibility with TypeScript's NaN bit pattern
            const long typeScriptNaNBits = 0x7FF8000000000000;
            BinaryPrimitives.WriteInt64LittleEndian(bytes, typeScriptNaNBits);
        }
        else
        {
#if NETSTANDARD2_1
            // BinaryPrimitives.WriteDoubleLittleEndian not available in netstandard2.1
            var doubleBytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(doubleBytes);
            }
            doubleBytes.CopyTo(bytes);
#else
            BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
#endif
        }

        // Base64 encode the bytes
        var base64 = Convert.ToBase64String(bytes);

        return $"{{\"$float\":\"{base64}\"}}";
    }

    /// <summary>
    /// Serializes a byte array to Convex's format.
    /// </summary>
    /// <param name="bytes">The byte array to serialize.</param>
    /// <returns>The Convex JSON representation with $bytes field.</returns>
    private static string SerializeBytes(byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        return $"{{\"$bytes\":\"{base64}\"}}";
    }

    /// <summary>
    /// Serializes complex objects (arrays, dictionaries, custom objects).
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="visited">Set of already visited objects to prevent cycles.</param>
    /// <returns>The Convex JSON representation.</returns>
    private static string SerializeComplexObject<T>(T? value, HashSet<object> visited)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle dictionaries (must check before IEnumerable since Dictionary implements it)
        if (value is System.Collections.IDictionary dictionary)
        {
            var serializedProperties = new List<string>();
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                var serializedValue = SerializeToConvexJson(entry.Value, visited);
                serializedProperties.Add($"\"{key}\":{serializedValue}");
            }
            // Sort keys alphabetically to match convex-js behavior
            serializedProperties.Sort();
            return $"{{{string.Join(",", serializedProperties)}}}";
        }

        // Handle arrays
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var array = enumerable.Cast<object?>().ToArray();
            var serializedItems = array.Select(item => SerializeToConvexJson(item, visited));
            return $"[{string.Join(",", serializedItems)}]";
        }

        // Handle objects by recursively serializing properties
        return SerializeObjectRecursively(value!, visited);
    }

    /// <summary>
    /// Recursively serializes an object, handling Convex special values in nested properties.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="visited">Set of already visited objects to prevent cycles.</param>
    /// <returns>The Convex JSON representation.</returns>
    private static string SerializeObjectRecursively<T>(T value, HashSet<object> visited)
    {
        // Check for cycles - only if value is not null
        if (value != null && !visited.Add(value))
        {
            // Cycle detected, return null to avoid infinite recursion
            return "null";
        }

        try
        {
            var type = value!.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var serializedProperties = new List<string>();

            foreach (var property in properties)
            {
                object? propertyValue;
                try
                {
                    propertyValue = property.GetValue(value);
                }
                catch
                {
                    // Skip properties that throw exceptions when accessed
                    continue;
                }

                // Skip null properties for Convex compatibility (optional fields should be omitted)
                if (propertyValue == null)
                {
                    continue;
                }

                var propertyName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                var serializedValue = SerializeToConvexJson(propertyValue, visited);
                serializedProperties.Add($"\"{propertyName}\":{serializedValue}");
            }

            // Sort keys alphabetically to match convex-js behavior
            serializedProperties.Sort();

            return $"{{{string.Join(",", serializedProperties)}}}";
        }
        finally
        {
            // Remove from visited set when done with this object (only if not null)
            if (value != null)
            {
                _ = visited.Remove(value);
            }
        }
    }    /// <summary>
         /// Determines if a double value requires special encoding.
         /// </summary>
         /// <param name="value">The double value to check.</param>
         /// <returns>True if the value needs special encoding.</returns>
    private static bool RequiresSpecialEncoding(double value)
    {
        return double.IsNaN(value) ||
               double.IsInfinity(value) ||
               IsNegativeZero(value);
    }

    /// <summary>
    /// Checks if a double represents negative zero.
    /// </summary>
    /// <param name="value">The double value to check.</param>
    /// <returns>True if the value is negative zero.</returns>
    private static bool IsNegativeZero(double value) => value == 0.0 && double.IsNegativeInfinity(1.0 / value);
}
