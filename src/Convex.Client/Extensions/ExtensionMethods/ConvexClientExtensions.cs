namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for IConvexClient that provide additional convenience and safety features.
/// Note: The fluent API is the primary way to interact with Convex. Use Query&lt;TResult&gt;(), Mutate&lt;TResult&gt;(), and Action&lt;TResult&gt;() methods.
/// </summary>
public static class ConvexClientExtensions
{
    // All non-fluent extension methods have been removed.
    // Use the fluent API instead:
    // - client.Query&lt;TResult&gt;(functionName).WithArgs(args).ExecuteAsync()
    // - client.Mutate&lt;TResult&gt;(functionName).WithArgs(args).ExecuteAsync()
    // - client.Action&lt;TResult&gt;(functionName).WithArgs(args).ExecuteAsync()
}
