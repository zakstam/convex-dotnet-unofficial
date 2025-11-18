using System.Text.Json;

namespace Convex.Client.Extensions.ArgumentBuilders;

/// <summary>
/// Fluent builder for constructing strongly-typed arguments for Convex queries, mutations, and actions.
/// Provides a cleaner alternative to anonymous objects with validation and type-safety.
/// </summary>
public class ArgumentBuilder
{
    private readonly Dictionary<string, object?> _arguments = [];

    /// <summary>
    /// Adds a required argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    public ArgumentBuilder Add(string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "Argument name cannot be null or empty.");
        }

        _arguments[name] = value;
        return this;
    }

    /// <summary>
    /// Adds an optional argument with the specified name and value.
    /// The argument is only included if the value is not null.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The argument value (null values are excluded).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddOptional(string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "Argument name cannot be null or empty.");
        }

        if (value != null)
        {
            _arguments[name] = value;
        }

        return this;
    }

    /// <summary>
    /// Adds a string argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddString(string name, string value) => Add(name, value);

    /// <summary>
    /// Adds an integer argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddInt(string name, int value) => Add(name, value);

    /// <summary>
    /// Adds a long argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The long value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddLong(string name, long value) => Add(name, value);

    /// <summary>
    /// Adds a double argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The double value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddDouble(string name, double value) => Add(name, value);

    /// <summary>
    /// Adds a boolean argument with the specified name and value.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddBool(string name, bool value) => Add(name, value);

    /// <summary>
    /// Adds a DateTime argument as a Convex timestamp (Unix milliseconds).
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="dateTime">The DateTime value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddDateTime(string name, DateTime dateTime) => Add(name, Converters.TimestampConverter.ToConvexTimestamp(dateTime));

    /// <summary>
    /// Adds an optional DateTime argument as a Convex timestamp (Unix milliseconds).
    /// The argument is only included if the value is not null.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="dateTime">The nullable DateTime value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddOptionalDateTime(string name, DateTime? dateTime)
    {
        return AddOptional(name, dateTime.HasValue
            ? Converters.TimestampConverter.ToConvexTimestamp(dateTime.Value)
            : null);
    }

    /// <summary>
    /// Adds a DateTimeOffset argument as a Convex timestamp (Unix milliseconds).
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="dateTimeOffset">The DateTimeOffset value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddDateTimeOffset(string name, DateTimeOffset dateTimeOffset) => Add(name, Converters.TimestampConverter.ToConvexTimestamp(dateTimeOffset));

    /// <summary>
    /// Adds an optional DateTimeOffset argument as a Convex timestamp (Unix milliseconds).
    /// The argument is only included if the value is not null.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="dateTimeOffset">The nullable DateTimeOffset value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddOptionalDateTimeOffset(string name, DateTimeOffset? dateTimeOffset)
    {
        return AddOptional(name, dateTimeOffset.HasValue
            ? Converters.TimestampConverter.ToConvexTimestamp(dateTimeOffset.Value)
            : null);
    }

    /// <summary>
    /// Adds an array argument with the specified name and values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array values.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddArray<T>(string name, IEnumerable<T> values) => Add(name, values.ToArray());

    /// <summary>
    /// Adds an optional array argument with the specified name and values.
    /// The argument is only included if the values collection is not null and not empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array values (null or empty arrays are excluded).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddOptionalArray<T>(string name, IEnumerable<T>? values) => values != null && values.Any() ? Add(name, values.ToArray()) : this;

    /// <summary>
    /// Adds a nested object argument with the specified name.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="configure">Action to configure the nested object.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddObject(string name, Action<ArgumentBuilder> configure)
    {
        var nestedBuilder = new ArgumentBuilder();
        configure(nestedBuilder);
        return Add(name, nestedBuilder.Build());
    }

    /// <summary>
    /// Adds multiple arguments from a dictionary.
    /// </summary>
    /// <param name="arguments">Dictionary of argument names and values.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder AddRange(IDictionary<string, object?> arguments)
    {
        foreach (var kvp in arguments)
        {
            _ = Add(kvp.Key, kvp.Value);
        }
        return this;
    }

    /// <summary>
    /// Removes an argument with the specified name.
    /// </summary>
    /// <param name="name">The argument name to remove.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder Remove(string name)
    {
        _ = _arguments.Remove(name);
        return this;
    }

    /// <summary>
    /// Clears all arguments from the builder.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public ArgumentBuilder Clear()
    {
        _arguments.Clear();
        return this;
    }

    /// <summary>
    /// Checks if an argument with the specified name exists.
    /// </summary>
    /// <param name="name">The argument name to check.</param>
    /// <returns>True if the argument exists; otherwise, false.</returns>
    public bool Contains(string name) => _arguments.ContainsKey(name);

    /// <summary>
    /// Gets the number of arguments in the builder.
    /// </summary>
    public int Count => _arguments.Count;

    /// <summary>
    /// Builds and returns the arguments as an object suitable for Convex function calls.
    /// Returns null if no arguments have been added.
    /// </summary>
    /// <returns>An object containing all added arguments, or null if no arguments exist.</returns>
    public object? Build() => _arguments.Count == 0 ? null : _arguments;

    /// <summary>
    /// Builds and returns the arguments as a typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the arguments to.</typeparam>
    /// <returns>Strongly-typed arguments object.</returns>
    public T? BuildAs<T>()
    {
        if (_arguments.Count == 0)
        {
            return default;
        }

        var json = JsonSerializer.Serialize(_arguments);
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Creates a new ArgumentBuilder instance.
    /// </summary>
    /// <returns>A new ArgumentBuilder instance.</returns>
    public static ArgumentBuilder Create() => new();

    /// <summary>
    /// Creates a new ArgumentBuilder with a single argument.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>A new ArgumentBuilder instance with the specified argument.</returns>
    public static ArgumentBuilder With(string name, object? value) => new ArgumentBuilder().Add(name, value);
}
