using Convex.Client.Shared.Builders;

namespace Convex.Client.Shared.ArgumentBuilders;

/// <summary>
/// Extension methods for action builders to work with ArgumentBuilder.
/// </summary>
public static class ActionBuilderArgumentExtensions
{
    /// <summary>
    /// Sets arguments using an ArgumentBuilder for fluent construction.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TArgs">The arguments type.</typeparam>
    /// <param name="builder">The action builder.</param>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <returns>The action builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var args = ArgumentBuilder.Create&lt;SendEmailArgs&gt;()
    ///     .Set(a => a.To = "user@example.com")
    ///     .Set(a => a.Subject = "Hello");
    /// 
    /// await client.Action&lt;object&gt;("emails:send")
    ///     .WithArgs(args)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public static IActionBuilder<TResult> WithArgs<TResult, TArgs>(
        this IActionBuilder<TResult> builder,
        ArgumentBuilder<TArgs> argumentBuilder) where TArgs : class, new()
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        if (argumentBuilder == null)
        {
            throw new ArgumentNullException(nameof(argumentBuilder));
        }

        return builder.WithArgs(argumentBuilder.Build());
    }
}

