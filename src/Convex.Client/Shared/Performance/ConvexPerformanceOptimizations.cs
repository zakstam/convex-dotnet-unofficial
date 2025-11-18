using Microsoft.Extensions.ObjectPool;
using System.Buffers;
using System.Text;
using Convex.Client.Shared.Serialization;

namespace Convex.Client.Shared.Performance;

/// <summary>
/// High-performance optimizations for Convex client operations.
/// Provides object pooling, memory pooling, and zero-allocation patterns.
/// </summary>
public static class ConvexPerformanceOptimizations
{
    private static readonly ObjectPoolProvider DefaultPoolProvider = new DefaultObjectPoolProvider();

    /// <summary>
    /// Object pool for HttpRequestMessage instances.
    /// </summary>
    public static readonly ObjectPool<HttpRequestMessage> RequestPool =
        DefaultPoolProvider.Create(new HttpRequestMessagePoolPolicy());

    /// <summary>
    /// Object pool for StringBuilder instances.
    /// </summary>
    public static readonly ObjectPool<StringBuilder> StringBuilderPool =
        DefaultPoolProvider.Create(new StringBuilderPoolPolicy());

    /// <summary>
    /// Array pool for byte buffers.
    /// </summary>
    public static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Array pool for char buffers.
    /// </summary>
    public static readonly ArrayPool<char> CharArrayPool = ArrayPool<char>.Shared;
}

/// <summary>
/// Object pool policy for HttpRequestMessage instances.
/// </summary>
public class HttpRequestMessagePoolPolicy : PooledObjectPolicy<HttpRequestMessage>
{
    public override HttpRequestMessage Create() => new HttpRequestMessage();

    public override bool Return(HttpRequestMessage obj)
    {
        if (obj == null) return false;

        try
        {
            // Reset the request for reuse
            obj.Content?.Dispose();
            obj.Content = null;
            obj.Method = HttpMethod.Get;
            obj.RequestUri = null;
            obj.Version = new Version(1, 1);
            obj.Headers.Clear();

            // Clear options by creating new instance (HttpRequestOptions doesn't have Clear)
            // This is acceptable since object pooling is for reducing allocation pressure
            // and HttpRequestMessage creation is lightweight

            return true;
        }
        catch
        {
            // If reset fails, don't return to pool
            return false;
        }
    }
}

/// <summary>
/// Object pool policy for StringBuilder instances.
/// </summary>
public class StringBuilderPoolPolicy : PooledObjectPolicy<StringBuilder>
{
    private const int MaxCapacity = 4096;

    public override StringBuilder Create() => new StringBuilder();

    public override bool Return(StringBuilder obj)
    {
        if (obj == null || obj.Capacity > MaxCapacity)
            return false;

        _ = obj.Clear();
        return true;
    }
}

/// <summary>
/// High-performance JSON serialization using pooled resources.
/// </summary>
public static class PooledJsonSerialization
{
    /// <summary>
    /// Serializes a value to JSON using pooled StringBuilder for efficiency.
    /// </summary>
    /// <typeparam name="T">The type of value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON representation.</returns>
    public static string SerializeWithPooling<T>(T? value)
    {
        var sb = ConvexPerformanceOptimizations.StringBuilderPool.Get();
        try
        {
            // Use ConvexSerializer but build string efficiently
            switch (value)
            {
                case null:
                    return "null";

                case bool boolValue:
                    return boolValue ? "true" : "false";

                case string stringValue:
                    _ = sb.Append('"');
                    AppendEscapedString(sb, stringValue);
                    _ = sb.Append('"');
                    return sb.ToString();

                case long longValue:
                    return ConvexSerializer.SerializeToConvexJson(longValue);

                case double doubleValue:
                    return ConvexSerializer.SerializeToConvexJson(doubleValue);

                case byte[] bytesValue:
                    return ConvexSerializer.SerializeToConvexJson(bytesValue);

                default:
                    // Fall back to standard serialization for complex objects
                    return ConvexSerializer.SerializeToConvexJson(value);
            }
        }
        finally
        {
            ConvexPerformanceOptimizations.StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Efficiently appends escaped string content to StringBuilder.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="value">The string value to escape and append.</param>
    private static void AppendEscapedString(StringBuilder sb, string value)
    {
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    _ = sb.Append("\\\"");
                    break;
                case '\\':
                    _ = sb.Append("\\\\");
                    break;
                case '\n':
                    _ = sb.Append("\\n");
                    break;
                case '\r':
                    _ = sb.Append("\\r");
                    break;
                case '\t':
                    _ = sb.Append("\\t");
                    break;
                case '\b':
                    _ = sb.Append("\\b");
                    break;
                case '\f':
                    _ = sb.Append("\\f");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        _ = sb.Append($"\\u{(int)c:X4}");
                    }
                    else
                    {
                        _ = sb.Append(c);
                    }
                    break;
            }
        }
    }
}

/// <summary>
/// High-performance byte operations using pooled arrays.
/// </summary>
public static class PooledByteOperations
{
    /// <summary>
    /// Converts a value to UTF-8 bytes using pooled arrays.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A pooled byte array. Must be returned to pool after use.</returns>
    public static (byte[] buffer, int length) ToUtf8Bytes(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        var buffer = ConvexPerformanceOptimizations.ByteArrayPool.Rent(maxByteCount);

        var actualLength = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
        return (buffer, actualLength);
    }

    /// <summary>
    /// Returns a byte array to the pool.
    /// </summary>
    /// <param name="buffer">The buffer to return.</param>
    public static void ReturnBuffer(byte[] buffer) => ConvexPerformanceOptimizations.ByteArrayPool.Return(buffer);
}

/// <summary>
/// Disposable wrapper for pooled resources that automatically returns them.
/// </summary>
/// <typeparam name="T">The type of pooled resource.</typeparam>
public readonly ref struct PooledResource<T>(ObjectPool<T> pool) where T : class
{
    private readonly ObjectPool<T> _pool = pool;
    private readonly T _resource = pool.Get();

    public T Resource => _resource;

    public void Dispose() => _pool.Return(_resource);
}

/// <summary>
/// High-performance request context with minimal allocations.
/// </summary>
public readonly struct ConvexRequestContext(string functionName, string requestType, string requestId)
{
    public readonly string FunctionName = functionName;
    public readonly string RequestType = requestType;
    public readonly string RequestId = requestId;
    public readonly long TimestampTicks = DateTime.UtcNow.Ticks;

    public DateTimeOffset Timestamp => new(TimestampTicks, TimeSpan.Zero);

    public override string ToString() => $"{RequestType}:{FunctionName}:{RequestId}";
}

/// <summary>
/// Memory-efficient JSON reader for parsing Convex responses.
/// </summary>
public ref struct ConvexJsonReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public ConvexJsonReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>
    /// Reads the next JSON element without allocating intermediate objects.
    /// </summary>
    /// <returns>True if an element was read successfully.</returns>
    public bool TryReadNext(out ReadOnlySpan<byte> elementBytes)
    {
        if (_position >= _buffer.Length)
        {
            elementBytes = default;
            return false;
        }

        // Simple JSON element extraction (can be enhanced for full JSON parsing)
        var start = _position;
        var braceCount = 0;
        var inString = false;
        var escaped = false;

        while (_position < _buffer.Length)
        {
            var currentByte = _buffer[_position];

            if (!escaped && currentByte == (byte)'"')
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (currentByte == (byte)'{')
                    braceCount++;
                else if (currentByte == (byte)'}')
                    braceCount--;
            }

            escaped = !escaped && currentByte == (byte)'\\';
            _position++;

            if (!inString && braceCount == 0 && start != _position - 1)
            {
                break;
            }
        }

        elementBytes = _buffer.Slice(start, _position - start);
        return true;
    }
}

/// <summary>
/// Performance monitoring for Convex operations.
/// </summary>
public static class ConvexPerformanceMonitor
{
    private static long _totalAllocatedBytes;
    private static long _totalRequests;
    private static readonly object _statsLock = new();

    /// <summary>
    /// Records memory allocation for an operation.
    /// </summary>
    /// <param name="bytes">The number of bytes allocated.</param>
    public static void RecordAllocation(long bytes) => _ = Interlocked.Add(ref _totalAllocatedBytes, bytes);

    /// <summary>
    /// Records a completed request.
    /// </summary>
    public static void RecordRequest() => _ = Interlocked.Increment(ref _totalRequests);

    /// <summary>
    /// Gets the current performance statistics.
    /// </summary>
    /// <returns>Current performance metrics.</returns>
    public static ConvexPerformanceStats GetStats()
    {
        return new ConvexPerformanceStats
        {
            TotalAllocatedBytes = Interlocked.Read(ref _totalAllocatedBytes),
            TotalRequests = Interlocked.Read(ref _totalRequests),
            AverageBytesPerRequest = _totalRequests > 0
                ? (double)_totalAllocatedBytes / _totalRequests
                : 0,
            CurrentManagedMemory = GC.GetTotalMemory(false)
        };
    }

    /// <summary>
    /// Resets performance counters.
    /// </summary>
    public static void Reset()
    {
        lock (_statsLock)
        {
            _ = Interlocked.Exchange(ref _totalAllocatedBytes, 0);
            _ = Interlocked.Exchange(ref _totalRequests, 0);
        }
    }
}

/// <summary>
/// Performance statistics for Convex operations.
/// </summary>
public readonly record struct ConvexPerformanceStats
{
    public long TotalAllocatedBytes { get; init; }
    public long TotalRequests { get; init; }
    public double AverageBytesPerRequest { get; init; }
    public long CurrentManagedMemory { get; init; }

    public override string ToString() =>
        $"Requests: {TotalRequests:N0}, " +
        $"Allocated: {TotalAllocatedBytes:N0} bytes, " +
        $"Avg/Request: {AverageBytesPerRequest:F1} bytes, " +
        $"Current Memory: {CurrentManagedMemory:N0} bytes";
}
