using System.Text.Json;
using Convex.Client.Shared.Serialization;

namespace Convex.Client.Shared.Http;

/// <summary>
/// Helper class for parsing Convex API responses.
/// Handles the standard Convex response format with status, value, and errorMessage fields.
/// </summary>
public static class ConvexResponseParser
{
    /// <summary>
    /// Parses a Convex API response JSON string into a typed result.
    /// </summary>
    /// <typeparam name="TResult">The type of result to deserialize.</typeparam>
    /// <param name="json">The JSON response string from Convex API.</param>
    /// <param name="functionName">The name of the function that was called (for error messages).</param>
    /// <param name="operationType">The type of operation (query, mutation, action) for error messages.</param>
    /// <param name="serializer">The serializer to use for deserialization.</param>
    /// <returns>The deserialized result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response format is invalid or deserialization fails.</exception>
    public static TResult ParseResponse<TResult>(
        string json,
        string functionName,
        string operationType,
        IConvexSerializer serializer)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Empty response from {operationType} '{functionName}'");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out var status))
        {
            var statusValue = status.GetString();
            if (statusValue == "success" && root.TryGetProperty("value", out var value))
            {
                var result = serializer.Deserialize<TResult>(value.GetRawText());
                return result == null
                    ? throw new InvalidOperationException(
                        $"Failed to deserialize {operationType} result for function '{functionName}'")
                    : result;
            }
            else if (statusValue == "error")
            {
                var errorMessage = root.TryGetProperty("errorMessage", out var errMsg)
                    ? errMsg.GetString()
                    : $"Unknown {operationType} error";
                throw new InvalidOperationException(
                    $"{operationType.FirstCharToUpper()} '{functionName}' failed: {errorMessage}");
            }
        }

        throw new InvalidOperationException(
            $"Invalid response format from {operationType} '{functionName}'");
    }

    /// <summary>
    /// Parses a Convex API response from an HttpResponseMessage.
    /// </summary>
    /// <typeparam name="TResult">The type of result to deserialize.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="functionName">The name of the function that was called (for error messages).</param>
    /// <param name="operationType">The type of operation (query, mutation, action) for error messages.</param>
    /// <param name="serializer">The serializer to use for deserialization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response format is invalid or deserialization fails.</exception>
    public static async Task<TResult> ParseResponseAsync<TResult>(
        HttpResponseMessage response,
        string functionName,
        string operationType,
        IConvexSerializer serializer,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
        // This matches convex-js behavior where HTTP 560 is treated as a valid response
        ConvexHttpConstants.EnsureConvexResponse(response);

        var responseJson = await response.ReadContentAsStringAsync(cancellationToken);
        return ParseResponse<TResult>(responseJson, functionName, operationType, serializer);
    }
}

// Extension methods for string
internal static class StringExtensions
{
    public static string FirstCharToUpper(this string input) => string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input[1..];
}

