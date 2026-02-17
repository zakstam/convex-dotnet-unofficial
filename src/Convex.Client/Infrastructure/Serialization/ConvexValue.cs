namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// Defines operations for i convex value.
/// </summary>
public interface IConvexValue
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    ConvexValueType Type { get; }

    /// <summary>
    /// Gets value.
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
