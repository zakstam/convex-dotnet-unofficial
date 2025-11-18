using System.Collections.Concurrent;
using System.Reflection;

namespace Convex.Client.Shared.Internal.Reflection;

/// <summary>
/// Thread-safe cache for expensive reflection operations.
/// Dramatically improves performance by caching MethodInfo lookups that would
/// otherwise run on every query/mutation/action execution.
/// </summary>
internal static class ReflectionCache
{
    private static readonly ConcurrentDictionary<(Type, string, int), MethodInfo> MethodCache = new();

    /// <summary>
    /// Gets a cached generic method with the specified characteristics.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <param name="genericParameterCount">The number of generic type parameters.</param>
    /// <returns>The cached MethodInfo, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the method is not found.</exception>
    public static MethodInfo GetGenericMethod(Type type, string methodName, int genericParameterCount)
    {
        var key = (type, methodName, genericParameterCount);

        return MethodCache.GetOrAdd(key, static k =>
        {
            var (searchType, name, paramCount) = k;

            var method = searchType.GetMethods()
                .FirstOrDefault(m =>
                    m.Name == name &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == paramCount);

            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Could not find generic method '{name}' with {paramCount} type parameters on type '{searchType.Name}'");
            }

            return method;
        });
    }

    /// <summary>
    /// Gets a cached, instantiated generic method.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="genericParameterCount">The number of generic type parameters in the method definition.</param>
    /// <param name="typeArguments">The type arguments to instantiate the generic method with.</param>
    /// <returns>The instantiated MethodInfo ready to invoke.</returns>
    public static MethodInfo GetInstantiatedGenericMethod(
        Type type,
        string methodName,
        int genericParameterCount,
        params Type[] typeArguments)
    {
        if (typeArguments.Length != genericParameterCount)
        {
            throw new ArgumentException(
                $"Expected {genericParameterCount} type arguments but got {typeArguments.Length}",
                nameof(typeArguments));
        }

        // Get the generic method definition from cache
        var genericMethod = GetGenericMethod(type, methodName, genericParameterCount);

        // Make the generic method with the specific types
        // Note: MakeGenericMethod is also cached internally by the CLR
        return genericMethod.MakeGenericMethod(typeArguments);
    }

    /// <summary>
    /// Clears the reflection cache. Useful for testing or if memory is a concern.
    /// In production, the cache should generally be left to grow as it provides
    /// significant performance benefits.
    /// </summary>
    public static void Clear() => MethodCache.Clear();

    /// <summary>
    /// Gets the current number of cached method lookups.
    /// </summary>
    public static int CachedMethodCount => MethodCache.Count;
}
