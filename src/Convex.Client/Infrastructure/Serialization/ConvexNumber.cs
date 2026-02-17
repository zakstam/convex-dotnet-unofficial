using System.Globalization;
using System.Text.Json.Serialization;

namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// Executes the convex number operation.
/// Convex stores all numbers as IEEE 754 doubles internally (same as JavaScript),
/// so this type provides a safe wrapper with implicit conversions to/from C# numeric types.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When to use ConvexNumber:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>When you need flexibility to treat a value as int, long, or double</description></item>
/// <item><description>When migrating code that mixes numeric types</description></item>
/// <item><description>When you want explicit control over numeric conversions</description></item>
/// </list>
/// <para>
/// <strong>Recommended approach:</strong><br/>
/// For most cases, use <c>double</c> directly for Convex numbers since that's what Convex uses internally.
/// Only use ConvexNumber when you need the flexibility of multiple numeric representations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Flexible usage:
/// ConvexNumber count = 42;
/// int countAsInt = count;
/// double countAsDouble = count;
///
/// // Explicit conversions when needed:
/// int userId = myNumber.AsInt32();
/// long timestamp = myNumber.AsInt64();
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the ConvexNumber struct.
/// </remarks>
/// <param name="value">The numeric value to store.</param>
[JsonConverter(typeof(ConvexNumberJsonConverter))]
public readonly struct ConvexNumber(double value) : IEquatable<ConvexNumber>, IComparable<ConvexNumber>, IFormattable
{
    private readonly double _value = value;

    /// <summary>
    /// Gets the value.
    /// </summary>
    public double Value => _value;

    #region Implicit Conversions FROM C# types TO ConvexNumber

    /// <summary>
    /// Gets the operator convex number.
    /// </summary>
    public static implicit operator ConvexNumber(int value) => new(value);
    /// <summary>
    /// Gets the operator convex number.
    /// </summary>
    public static implicit operator ConvexNumber(long value) => new(value);
    /// <summary>
    /// Gets the operator convex number.
    /// </summary>
    public static implicit operator ConvexNumber(double value) => new(value);
    /// <summary>
    /// Gets the operator convex number.
    /// </summary>
    public static implicit operator ConvexNumber(float value) => new(value);
    /// <summary>
    /// Gets the operator convex number.
    /// </summary>
    public static implicit operator ConvexNumber(decimal value) => new((double)value);

    #endregion

    #region Implicit Conversions FROM ConvexNumber TO C# types

    /// <summary>
    /// Gets the operator double.
    /// </summary>
    public static implicit operator double(ConvexNumber number) => number._value;
    /// <summary>
    /// Gets the operator float.
    /// </summary>
    public static implicit operator float(ConvexNumber number) => (float)number._value;
    /// <summary>
    /// Gets the operator decimal.
    /// </summary>
    public static implicit operator decimal(ConvexNumber number) => (decimal)number._value;

    #endregion

    #region Explicit Conversions (for integer types to prevent accidental truncation)

    /// <summary>
    /// Gets the operator int.
    /// </summary>
    public static explicit operator int(ConvexNumber number) => (int)number._value;
    /// <summary>
    /// Gets the operator long.
    /// </summary>
    public static explicit operator long(ConvexNumber number) => (long)number._value;
    /// <summary>
    /// Gets the operator short.
    /// </summary>
    public static explicit operator short(ConvexNumber number) => (short)number._value;
    /// <summary>
    /// Gets the operator byte.
    /// </summary>
    public static explicit operator byte(ConvexNumber number) => (byte)number._value;

    #endregion

    #region Safe Conversion Methods

    /// <summary>
    /// Converts the number to a 32-bit integer, truncating any decimal portion.
    /// </summary>
    /// <returns>The value as an Int32.</returns>
    public int AsInt32() => (int)_value;

    /// <summary>
    /// Converts the number to a 64-bit integer, truncating any decimal portion.
    /// </summary>
    /// <returns>The value as an Int64.</returns>
    public long AsInt64() => (long)_value;

    /// <summary>
    /// Gets the as double.
    /// </summary>
    /// <returns>The value as a double.</returns>
    public double AsDouble() => _value;

    /// <summary>
    /// Gets the as single.
    /// </summary>
    /// <returns>The value as a float.</returns>
    public float AsSingle() => (float)_value;

    /// <summary>
    /// Gets the as decimal.
    /// </summary>
    /// <returns>The value as a decimal.</returns>
    public decimal AsDecimal() => (decimal)_value;

    #endregion

    #region Equality and Comparison

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public bool Equals(ConvexNumber other) => _value.Equals(other._value);

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj) => obj is ConvexNumber other && Equals(other);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator ==(ConvexNumber left, ConvexNumber right) => left.Equals(right);

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator !=(ConvexNumber left, ConvexNumber right) => !left.Equals(right);

    /// <summary>
    /// Gets the compare to.
    /// </summary>
    public int CompareTo(ConvexNumber other) => _value.CompareTo(other._value);

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator <(ConvexNumber left, ConvexNumber right) => left._value < right._value;

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator <=(ConvexNumber left, ConvexNumber right) => left._value <= right._value;

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator >(ConvexNumber left, ConvexNumber right) => left._value > right._value;

    /// <summary>
    /// Gets or sets a value indicating whether operator.
    /// </summary>
    public static bool operator >=(ConvexNumber left, ConvexNumber right) => left._value >= right._value;

    #endregion

    #region Arithmetic Operators

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator +(ConvexNumber left, ConvexNumber right) => new(left._value + right._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator -(ConvexNumber left, ConvexNumber right) => new(left._value - right._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator *(ConvexNumber left, ConvexNumber right) => new(left._value * right._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator /(ConvexNumber left, ConvexNumber right) => new(left._value / right._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator %(ConvexNumber left, ConvexNumber right) => new(left._value % right._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator -(ConvexNumber value) => new(-value._value);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator ++(ConvexNumber value) => new(value._value + 1);

    /// <summary>
    /// Gets the operator.
    /// </summary>
    public static ConvexNumber operator --(ConvexNumber value) => new(value._value - 1);

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public string ToString(string? format) => _value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public string ToString(IFormatProvider? formatProvider) => _value.ToString(formatProvider);

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);

    #endregion
}
