using System.Diagnostics;
using System.Text.Json;
using Convex.Client.Infrastructure.Builders;

namespace Convex.Client.DeveloperTools;

/// <summary>
/// Developer experience extensions for query builders.
/// Provides helpful tools for development, debugging, and performance analysis.
/// </summary>
public static class QueryBuilderDevExtensions
{
    private static readonly JsonSerializerOptions PrettyPrintOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Executes the query and dumps the result to console with pretty formatting.
    /// Useful for development and debugging.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <param name="title">Optional title to display before the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query result.</returns>
    /// <remarks>
    /// This method is intended for development use only. Consider removing
    /// or disabling in production builds.
    /// </remarks>
    public static async Task<TResult> DumpToConsoleAsync<TResult>(
        this IQueryBuilder<TResult> builder,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var result = await builder.ExecuteAsync(cancellationToken);

        Console.WriteLine("═══════════════════════════════════════════════════════");
        if (!string.IsNullOrEmpty(title))
        {
            Console.WriteLine($"Query Result: {title}");
        }
        else
        {
            Console.WriteLine("Query Result");
        }
        Console.WriteLine("═══════════════════════════════════════════════════════");

        try
        {
            var json = JsonSerializer.Serialize(result, PrettyPrintOptions);
            Console.WriteLine(json);
        }
        catch
        {
            // Fallback to ToString if serialization fails
            Console.WriteLine(result?.ToString() ?? "<null>");
        }

        Console.WriteLine("═══════════════════════════════════════════════════════");

        return result;
    }

    /// <summary>
    /// Executes the query and measures its performance.
    /// Returns both the result and the execution duration.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the result and execution duration.</returns>
    /// <remarks>
    /// Useful for identifying performance bottlenecks during development.
    /// The duration includes network latency and server-side execution time.
    /// </remarks>
    public static async Task<(TResult Result, TimeSpan Duration)> MeasurePerformanceAsync<TResult>(
        this IQueryBuilder<TResult> builder,
        CancellationToken cancellationToken = default)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await builder.ExecuteAsync(cancellationToken);
        stopwatch.Stop();

        return (result, stopwatch.Elapsed);
    }

    /// <summary>
    /// Executes the query, measures performance, and dumps the result with timing information.
    /// Combines DumpToConsole and MeasurePerformance for comprehensive debugging.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <param name="title">Optional title to display.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the result and execution duration.</returns>
    public static async Task<(TResult Result, TimeSpan Duration)> DumpWithTimingAsync<TResult>(
        this IQueryBuilder<TResult> builder,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await builder.ExecuteAsync(cancellationToken);
        stopwatch.Stop();

        Console.WriteLine("═══════════════════════════════════════════════════════");
        if (!string.IsNullOrEmpty(title))
        {
            Console.WriteLine($"Query Result: {title}");
        }
        else
        {
            Console.WriteLine("Query Result");
        }
        Console.WriteLine($"⏱️  Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        try
        {
            var json = JsonSerializer.Serialize(result, PrettyPrintOptions);
            Console.WriteLine(json);
        }
        catch
        {
            Console.WriteLine(result?.ToString() ?? "<null>");
        }

        Console.WriteLine("═══════════════════════════════════════════════════════");

        return (result, stopwatch.Elapsed);
    }
}
