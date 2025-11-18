using Convex.Client.Extensions.Converters;

namespace Convex.Client.Extensions.Testing;

/// <summary>
/// Provides helper utilities for testing Convex-based applications.
/// </summary>
public static class ConvexTestHelpers
{
    /// <summary>
    /// Creates a mock Convex timestamp for a specific date/time (useful for testing).
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="month">Month</param>
    /// <param name="day">Day</param>
    /// <param name="hour">Hour (default: 0)</param>
    /// <param name="minute">Minute (default: 0)</param>
    /// <param name="second">Second (default: 0)</param>
    /// <returns>Convex timestamp (Unix milliseconds).</returns>
    public static double CreateTimestamp(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) => TimestampConverter.FromDateComponents(year, month, day, hour, minute, second);

    /// <summary>
    /// Creates a mock Convex timestamp for "now" (useful for consistent testing).
    /// </summary>
    /// <returns>Current timestamp.</returns>
    public static double CreateNow() => TimestampConverter.Now();

    /// <summary>
    /// Creates a mock Convex timestamp for a date relative to now.
    /// </summary>
    /// <param name="daysFromNow">Number of days from now (can be negative for past dates).</param>
    /// <returns>Convex timestamp.</returns>
    public static double CreateRelativeTimestamp(int daysFromNow)
    {
        var now = TimestampConverter.Now();
        return TimestampConverter.AddDays(now, daysFromNow);
    }

    /// <summary>
    /// Generates a random Convex document ID (for testing purposes).
    /// </summary>
    /// <param name="tableName">The table name prefix (e.g., "todos").</param>
    /// <returns>A mock Convex ID in the format "tableName|randomstring".</returns>
    public static string GenerateMockId(string tableName = "test")
    {
        var randomPart = Guid.NewGuid().ToString("N")[..16];
        return $"{tableName}|{randomPart}";
    }

    /// <summary>
    /// Generates multiple mock IDs.
    /// </summary>
    /// <param name="count">Number of IDs to generate.</param>
    /// <param name="tableName">The table name prefix.</param>
    /// <returns>Array of mock IDs.</returns>
    public static string[] GenerateMockIds(int count, string tableName = "test") => [.. Enumerable.Range(0, count).Select(_ => GenerateMockId(tableName))];

    /// <summary>
    /// Creates a test pagination result.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    /// <param name="items">Items for this page.</param>
    /// <param name="continueCursor">Cursor for the next page (null if this is the last page).</param>
    /// <param name="isDone">Whether this is the final page.</param>
    /// <returns>A mock pagination result.</returns>
    public static object CreatePaginationResult<T>(List<T> items, string? continueCursor = null, bool isDone = true)
    {
        return new
        {
            page = items,
            isDone,
            continueCursor
        };
    }

    /// <summary>
    /// Creates test data with specified count using a factory function.
    /// </summary>
    /// <typeparam name="T">Type of items to create.</typeparam>
    /// <param name="count">Number of items to create.</param>
    /// <param name="factory">Factory function to create each item (receives index as parameter).</param>
    /// <returns>List of created items.</returns>
    public static List<T> CreateTestData<T>(int count, Func<int, T> factory) => [.. Enumerable.Range(0, count).Select(factory)];

    /// <summary>
    /// Simulates a delay (useful for testing timeout scenarios).
    /// </summary>
    /// <param name="milliseconds">Delay duration in milliseconds.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task SimulateDelayAsync(int milliseconds, CancellationToken cancellationToken = default) => Task.Delay(milliseconds, cancellationToken);

    /// <summary>
    /// Creates a cancellation token that will cancel after a specified delay (useful for timeout testing).
    /// </summary>
    /// <param name="milliseconds">Delay before cancellation.</param>
    /// <returns>Cancellation token source.</returns>
    public static CancellationTokenSource CreateTimeoutToken(int milliseconds)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(milliseconds);
        return cts;
    }

    /// <summary>
    /// Asserts that two timestamps are within an acceptable tolerance (useful for testing timestamp equality).
    /// </summary>
    /// <param name="timestamp1">First timestamp.</param>
    /// <param name="timestamp2">Second timestamp.</param>
    /// <param name="toleranceMs">Tolerance in milliseconds (default: 1000ms).</param>
    /// <returns>True if timestamps are within tolerance.</returns>
    public static bool TimestampsAreEqual(double timestamp1, double timestamp2, double toleranceMs = 1000) => Math.Abs(timestamp1 - timestamp2) <= toleranceMs;

    /// <summary>
    /// Converts a Convex timestamp to a human-readable string (useful for debugging tests).
    /// </summary>
    /// <param name="timestamp">The Convex timestamp.</param>
    /// <param name="format">Optional date format string.</param>
    /// <returns>Formatted date string.</returns>
    public static string FormatTimestamp(double timestamp, string format = "yyyy-MM-dd HH:mm:ss")
    {
        var dateTime = TimestampConverter.FromConvexTimestamp(timestamp);
        return dateTime.ToString(format);
    }

    /// <summary>
    /// Creates a mock error response (useful for testing error handling).
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="code">Error code (optional).</param>
    /// <returns>Mock error object.</returns>
    public static object CreateErrorResponse(string message, string? code = null)
    {
        return new
        {
            error = new
            {
                message,
                code = code ?? "TEST_ERROR"
            }
        };
    }
}
