namespace Convex.Client.Shared.Serialization;

/// <summary>
/// Represents a Convex value that can be serialized to/from Convex's JSON format.
/// </summary>
public interface IConvexValue
{
    /// <summary>
    /// Gets the type of this Convex value.
    /// </summary>
    ConvexValueType Type { get; }

    /// <summary>
    /// Gets the underlying .NET value with the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <returns>The value as the specified type.</returns>
    T? GetValue<T>();

    /// <summary>
    /// Serializes this value to Convex's JSON format.
    /// </summary>
    string ToConvexJson();
}

/// <summary>
/// Enumeration of all supported Convex value types.
/// </summary>
public enum ConvexValueType
{
    /// <summary>
    /// Null value.
    /// </summary>
    Null,

    /// <summary>
    /// Boolean value (true/false).
    /// </summary>
    Boolean,

    /// <summary>
    /// 64-bit integer value.
    /// </summary>
    Int64,

    /// <summary>
    /// 64-bit floating point value.
    /// </summary>
    Float64,

    /// <summary>
    /// String value.
    /// </summary>
    String,

    /// <summary>
    /// Byte array value.
    /// </summary>
    Bytes,

    /// <summary>
    /// Array of values.
    /// </summary>
    Array,

    /// <summary>
    /// Object with string keys and values.
    /// </summary>
    Object,

    /// <summary>
    /// Special floating point values (NaN, Infinity, -0).
    /// </summary>
    SpecialFloat
}
