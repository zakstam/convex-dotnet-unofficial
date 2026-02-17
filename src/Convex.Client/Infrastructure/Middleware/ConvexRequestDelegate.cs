using System.Diagnostics.CodeAnalysis;

namespace Convex.Client.Infrastructure.Middleware;

/// <summary>
/// Executes the convex request delegate operation.
/// </summary>
/// <param name="request">The request to process.</param>
/// <returns>A task that completes with the response.</returns>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This naming follows the convention established by ASP.NET Core's RequestDelegate")]
public delegate Task<ConvexResponse> ConvexRequestDelegate(ConvexRequest request);
