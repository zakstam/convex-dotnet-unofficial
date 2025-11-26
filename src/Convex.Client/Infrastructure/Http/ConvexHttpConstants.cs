namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Constants and helpers for Convex HTTP protocol.
/// </summary>
public static class ConvexHttpConstants
{
    /// <summary>
    /// Special custom HTTP status code (560) used by Convex to indicate that a UDF (User Defined Function) returned an error.
    /// This is not a network error - the response body contains valid JSON with error details that should be parsed.
    /// </summary>
    /// <remarks>
    /// This matches the constant STATUS_CODE_UDF_FAILED in convex-js/src/browser/http_client.ts
    /// </remarks>
    public const int StatusCodeUdfFailed = 560;

    /// <summary>
    /// Checks if an HTTP response should be processed as a valid Convex response.
    /// Returns true for success status codes (2xx) and StatusCodeUdfFailed (560).
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>True if the response should be parsed (contains valid Convex JSON), false otherwise.</returns>
    public static bool ShouldProcessResponse(HttpResponseMessage response)
    {
        return response.IsSuccessStatusCode ||
               (int)response.StatusCode == StatusCodeUdfFailed;
    }

    /// <summary>
    /// Ensures that an HTTP response is either successful or StatusCodeUdfFailed.
    /// Throws an exception if the response is neither.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <exception cref="HttpRequestException">Thrown if the response is not successful and not StatusCodeUdfFailed.</exception>
    public static void EnsureConvexResponse(HttpResponseMessage response)
    {
        if (!ShouldProcessResponse(response))
        {
            throw new HttpRequestException(
                $"HTTP request failed with status code {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
    }
}

