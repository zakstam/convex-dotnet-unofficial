using Convex.Client.Infrastructure.Builders;

namespace Convex.Client.Infrastructure.ArgumentBuilders;

/// <summary>
/// Extension methods for query builders to work with ArgumentBuilder.
/// </summary>
public static class QueryBuilderArgumentExtensions
{
    /// <summary>
    /// Sets arguments using an ArgumentBuilder for fluent construction.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TArgs">The arguments type.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <param name="argumentBuilder">The argument builder.</param>
    /// <returns>The query builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var args = ArgumentBuilder.Create&lt;GetMessagesArgs&gt;()
    ///     .Set(a => a.RoomId = "room-1")
    ///     .Set(a => a.Limit = 50);
    /// 
    /// var messages = await client.Query&lt;List&lt;Message&gt;&gt;("messages:get")
    ///     .WithArgs(args)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public static IQueryBuilder<TResult> WithArgs<TResult, TArgs>(
        this IQueryBuilder<TResult> builder,
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

