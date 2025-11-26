using Convex.Client.Infrastructure.Builders;

namespace Convex.Client.Infrastructure.ArgumentBuilders;

/// <summary>
/// Extension methods for mutation builders to work with ArgumentBuilder.
/// </summary>
public static class MutationBuilderArgumentExtensions
{
    /// <summary>
    /// Sets arguments using an ArgumentBuilder for fluent construction.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TArgs">The arguments type.</typeparam>
    /// <param name="builder">The mutation builder.</param>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <returns>The mutation builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var args = ArgumentBuilder.Create&lt;CreateTodoArgs&gt;()
    ///     .Set(a => a.Text = "Buy milk")
    ///     .Set(a => a.UserId = "user-123");
    /// 
    /// var todo = await client.Mutate&lt;Todo&gt;("todos:create")
    ///     .WithArgs(args)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public static IMutationBuilder<TResult> WithArgs<TResult, TArgs>(
        this IMutationBuilder<TResult> builder,
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

