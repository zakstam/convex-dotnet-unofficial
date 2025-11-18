namespace Convex.Client.Shared.ArgumentBuilders;

/// <summary>
/// Fluent builder for constructing function arguments with type safety and validation.
/// Provides a more ergonomic way to build arguments compared to anonymous objects.
/// </summary>
/// <typeparam name="TArgs">The type of arguments being built.</typeparam>
public class ArgumentBuilder<TArgs> where TArgs : class, new()
{
    private readonly TArgs _args;

    /// <summary>
    /// Initializes a new instance of the ArgumentBuilder class.
    /// </summary>
    public ArgumentBuilder()
    {
        _args = new TArgs();
    }

    /// <summary>
    /// Initializes a new instance of the ArgumentBuilder class with existing arguments.
    /// </summary>
    /// <param name="args">Existing arguments to build upon.</param>
    public ArgumentBuilder(TArgs args)
    {
        _args = args ?? new TArgs();
    }

    /// <summary>
    /// Sets a property value using a fluent API.
    /// </summary>
    /// <param name="setter">Action that sets the property value.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var args = new ArgumentBuilder&lt;GetMessagesArgs&gt;()
    ///     .Set(a => a.RoomId = "room-1")
    ///     .Set(a => a.Limit = 50)
    ///     .Build();
    /// </code>
    /// </example>
    public ArgumentBuilder<TArgs> Set(Action<TArgs> setter)
    {
        if (setter == null)
        {
            throw new ArgumentNullException(nameof(setter));
        }

        setter(_args);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured arguments.
    /// </summary>
    /// <returns>The configured arguments instance.</returns>
    public TArgs Build() => _args;

    /// <summary>
    /// Implicitly converts the builder to the arguments type.
    /// Allows using the builder directly where arguments are expected.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built arguments.</returns>
    public static implicit operator TArgs(ArgumentBuilder<TArgs> builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Build();
    }
}

/// <summary>
/// Static factory methods for creating argument builders.
/// </summary>
public static class ArgumentBuilder
{
    /// <summary>
    /// Creates a new argument builder for the specified type.
    /// </summary>
    /// <typeparam name="TArgs">The type of arguments to build.</typeparam>
    /// <returns>A new argument builder.</returns>
    /// <example>
    /// <code>
    /// var args = ArgumentBuilder.Create&lt;GetMessagesArgs&gt;()
    ///     .Set(a => a.RoomId = "room-1")
    ///     .Set(a => a.Limit = 50)
    ///     .Build();
    /// </code>
    /// </example>
    public static ArgumentBuilder<TArgs> Create<TArgs>() where TArgs : class, new()
    {
        return new ArgumentBuilder<TArgs>();
    }

    /// <summary>
    /// Creates a new argument builder from existing arguments.
    /// </summary>
    /// <typeparam name="TArgs">The type of arguments.</typeparam>
    /// <param name="args">Existing arguments to build upon.</param>
    /// <returns>A new argument builder initialized with the provided arguments.</returns>
    public static ArgumentBuilder<TArgs> From<TArgs>(TArgs args) where TArgs : class, new()
    {
        return new ArgumentBuilder<TArgs>(args);
    }
}

