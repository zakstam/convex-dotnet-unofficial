namespace Convex.Client.Extensions.EmptyArgs;

/// <summary>
/// Helper class for Convex functions that have args: {} in their TypeScript definition.
/// Convex functions with empty args still expect to receive an object (not null),
/// so use NoArgs.Instance when calling these functions.
/// </summary>
/// <example>
/// <code>
/// // Instead of:
/// var result = await client.QueryAsync&lt;List&lt;Item&gt;, object&gt;("items:list", new { });
///
/// // Use:
/// var result = await client.QueryAsync&lt;List&lt;Item&gt;, object&gt;("items:list", NoArgs.Instance);
/// </code>
/// </example>
public static class NoArgs
{
    /// <summary>
    /// Singleton empty object instance for Convex functions with args: {}.
    /// This is more efficient than creating new { } for every call.
    /// </summary>
    public static readonly object Instance = new { };
}
