namespace Convex.Client.Extensions.Converters;

/// <summary>
/// Provides utility methods for converting between .NET DateTime/DateTimeOffset and Convex timestamps.
/// Convex uses Unix timestamps in milliseconds (double) for all date/time values.
/// </summary>
public static class TimestampConverter
{
    /// <summary>
    /// Unix epoch as DateTimeOffset (January 1, 1970, 00:00:00 UTC).
    /// </summary>
    private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Converts a DateTime to a Convex timestamp (Unix milliseconds as double).
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>Unix timestamp in milliseconds.</returns>
    /// <exception cref="ArgumentException">Thrown when DateTime.Kind is Unspecified.</exception>
    public static double ToConvexTimestamp(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "DateTime.Kind must be Utc or Local. Use DateTime.SpecifyKind() to specify the kind.",
                nameof(dateTime));
        }

        var utcDateTime = dateTime.Kind == DateTimeKind.Local
            ? dateTime.ToUniversalTime()
            : dateTime;

        return (utcDateTime - UnixEpoch.DateTime).TotalMilliseconds;
    }

    /// <summary>
    /// Converts a DateTimeOffset to a Convex timestamp (Unix milliseconds as double).
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to convert.</param>
    /// <returns>Unix timestamp in milliseconds.</returns>
    public static double ToConvexTimestamp(this DateTimeOffset dateTimeOffset) => (dateTimeOffset - UnixEpoch).TotalMilliseconds;

    /// <summary>
    /// Converts a nullable DateTime to a Convex timestamp (Unix milliseconds as double).
    /// Returns null if the input is null.
    /// </summary>
    /// <param name="dateTime">The nullable DateTime to convert.</param>
    /// <returns>Unix timestamp in milliseconds, or null if input is null.</returns>
    public static double? ToConvexTimestamp(this DateTime? dateTime) => dateTime?.ToConvexTimestamp();

    /// <summary>
    /// Converts a nullable DateTimeOffset to a Convex timestamp (Unix milliseconds as double).
    /// Returns null if the input is null.
    /// </summary>
    /// <param name="dateTimeOffset">The nullable DateTimeOffset to convert.</param>
    /// <returns>Unix timestamp in milliseconds, or null if input is null.</returns>
    public static double? ToConvexTimestamp(this DateTimeOffset? dateTimeOffset) => dateTimeOffset?.ToConvexTimestamp();

    /// <summary>
    /// Converts a Convex timestamp (Unix milliseconds as double) to a DateTime in UTC.
    /// </summary>
    /// <param name="timestamp">The Convex timestamp in milliseconds.</param>
    /// <returns>DateTime in UTC.</returns>
    public static DateTime FromConvexTimestamp(double timestamp) => UnixEpoch.DateTime.AddMilliseconds(timestamp);

    /// <summary>
    /// Converts a Convex timestamp (Unix milliseconds as double) to a DateTimeOffset in UTC.
    /// </summary>
    /// <param name="timestamp">The Convex timestamp in milliseconds.</param>
    /// <returns>DateTimeOffset in UTC.</returns>
    public static DateTimeOffset FromConvexTimestampOffset(double timestamp) => UnixEpoch.AddMilliseconds(timestamp);

    /// <summary>
    /// Converts a nullable Convex timestamp (Unix milliseconds as double) to a nullable DateTime in UTC.
    /// Returns null if the input is null.
    /// </summary>
    /// <param name="timestamp">The nullable Convex timestamp in milliseconds.</param>
    /// <returns>DateTime in UTC, or null if input is null.</returns>
    public static DateTime? FromConvexTimestamp(double? timestamp) => timestamp.HasValue ? FromConvexTimestamp(timestamp.Value) : null;

    /// <summary>
    /// Converts a nullable Convex timestamp (Unix milliseconds as double) to a nullable DateTimeOffset in UTC.
    /// Returns null if the input is null.
    /// </summary>
    /// <param name="timestamp">The nullable Convex timestamp in milliseconds.</param>
    /// <returns>DateTimeOffset in UTC, or null if input is null.</returns>
    public static DateTimeOffset? FromConvexTimestampOffset(double? timestamp) => timestamp.HasValue ? FromConvexTimestampOffset(timestamp.Value) : null;

    /// <summary>
    /// Gets the current UTC time as a Convex timestamp (Unix milliseconds as double).
    /// </summary>
    /// <returns>Current UTC time as Convex timestamp.</returns>
    public static double Now() => DateTimeOffset.UtcNow.ToConvexTimestamp();

    /// <summary>
    /// Gets the current UTC date (midnight) as a Convex timestamp (Unix milliseconds as double).
    /// </summary>
    /// <returns>Current UTC date (midnight) as Convex timestamp.</returns>
    public static double Today() => DateTimeOffset.UtcNow.Date.ToConvexTimestamp();

    /// <summary>
    /// Creates a Convex timestamp from date components (UTC).
    /// </summary>
    /// <param name="year">Year (1-9999)</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="day">Day (1-31)</param>
    /// <param name="hour">Hour (0-23), default 0</param>
    /// <param name="minute">Minute (0-59), default 0</param>
    /// <param name="second">Second (0-59), default 0</param>
    /// <param name="millisecond">Millisecond (0-999), default 0</param>
    /// <returns>Convex timestamp representing the specified UTC date/time.</returns>
    public static double FromDateComponents(
        int year,
        int month,
        int day,
        int hour = 0,
        int minute = 0,
        int second = 0,
        int millisecond = 0)
    {
        var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
        return dateTime.ToConvexTimestamp();
    }

    /// <summary>
    /// Adds the specified number of days to a Convex timestamp.
    /// </summary>
    /// <param name="timestamp">The Convex timestamp.</param>
    /// <param name="days">Number of days to add (can be negative).</param>
    /// <returns>New Convex timestamp with days added.</returns>
    public static double AddDays(double timestamp, double days) => timestamp + (days * 24 * 60 * 60 * 1000);

    /// <summary>
    /// Adds the specified number of hours to a Convex timestamp.
    /// </summary>
    /// <param name="timestamp">The Convex timestamp.</param>
    /// <param name="hours">Number of hours to add (can be negative).</param>
    /// <returns>New Convex timestamp with hours added.</returns>
    public static double AddHours(double timestamp, double hours) => timestamp + (hours * 60 * 60 * 1000);

    /// <summary>
    /// Adds the specified number of minutes to a Convex timestamp.
    /// </summary>
    /// <param name="timestamp">The Convex timestamp.</param>
    /// <param name="minutes">Number of minutes to add (can be negative).</param>
    /// <returns>New Convex timestamp with minutes added.</returns>
    public static double AddMinutes(double timestamp, double minutes) => timestamp + (minutes * 60 * 1000);
}
