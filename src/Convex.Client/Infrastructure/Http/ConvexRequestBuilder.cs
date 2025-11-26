using System.Text;
using Convex.Client.Infrastructure.Serialization;

namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Helper class for building Convex API HTTP requests.
/// </summary>
public static class ConvexRequestBuilder
{
    /// <summary>
    /// Builds an HTTP request for a Convex query operation.
    /// </summary>
    /// <param name="deploymentUrl">The base deployment URL.</param>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="args">Optional arguments for the query.</param>
    /// <param name="serializer">The serializer to use for serializing arguments.</param>
    /// <param name="componentPath">Optional component path for component isolation.</param>
    /// <returns>An HTTP request message ready to send.</returns>
    public static HttpRequestMessage BuildQueryRequest(
        string deploymentUrl,
        string functionName,
        object? args,
        IConvexSerializer serializer,
        string? componentPath = null)
    {
        var url = $"{deploymentUrl}/api/query";

        var requestBody = new Dictionary<string, object?>
        {
            { "path", functionName },
            { "format", "convex_encoded_json" },
            { "args", new[] { args ?? new { } } }
        };

        if (!string.IsNullOrEmpty(componentPath))
        {
            requestBody["componentPath"] = componentPath;
        }

        var json = serializer.Serialize(requestBody) ?? throw new InvalidOperationException($"Failed to serialize query request body for function '{functionName}'. Serializer returned null.");
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
    }

    /// <summary>
    /// Builds an HTTP request for a Convex mutation operation.
    /// </summary>
    /// <param name="deploymentUrl">The base deployment URL.</param>
    /// <param name="functionName">The name of the mutation function.</param>
    /// <param name="args">Optional arguments for the mutation.</param>
    /// <param name="serializer">The serializer to use for serializing arguments.</param>
    /// <param name="componentPath">Optional component path for component isolation.</param>
    /// <returns>An HTTP request message ready to send.</returns>
    public static HttpRequestMessage BuildMutationRequest(
        string deploymentUrl,
        string functionName,
        object? args,
        IConvexSerializer serializer,
        string? componentPath = null)
    {
        var url = $"{deploymentUrl}/api/mutation";

        var requestBody = new Dictionary<string, object?>
        {
            { "path", functionName },
            { "format", "convex_encoded_json" },
            { "args", new[] { args ?? new { } } }
        };

        if (!string.IsNullOrEmpty(componentPath))
        {
            requestBody["componentPath"] = componentPath;
        }

        var json = serializer.Serialize(requestBody) ?? throw new InvalidOperationException($"Failed to serialize mutation request body for function '{functionName}'. Serializer returned null.");
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
    }

    /// <summary>
    /// Builds an HTTP request for a Convex action operation.
    /// </summary>
    /// <param name="deploymentUrl">The base deployment URL.</param>
    /// <param name="functionName">The name of the action function.</param>
    /// <param name="args">Optional arguments for the action.</param>
    /// <param name="serializer">The serializer to use for serializing arguments.</param>
    /// <param name="componentPath">Optional component path for component isolation.</param>
    /// <returns>An HTTP request message ready to send.</returns>
    public static HttpRequestMessage BuildActionRequest(
        string deploymentUrl,
        string functionName,
        object? args,
        IConvexSerializer serializer,
        string? componentPath = null)
    {
        var url = $"{deploymentUrl}/api/action";

        var requestBody = new Dictionary<string, object?>
        {
            { "path", functionName },
            { "format", "convex_encoded_json" },
            { "args", new[] { args ?? new { } } }
        };

        if (!string.IsNullOrEmpty(componentPath))
        {
            requestBody["componentPath"] = componentPath;
        }

        var json = serializer.Serialize(requestBody) ?? throw new InvalidOperationException($"Failed to serialize action request body for function '{functionName}'. Serializer returned null.");
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
    }
}

