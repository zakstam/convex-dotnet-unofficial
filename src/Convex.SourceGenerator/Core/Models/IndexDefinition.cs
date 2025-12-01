#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents an index definition on a Convex table.
/// </summary>
public class IndexDefinition
{
    /// <summary>
    /// The name of the index.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The fields included in the index.
    /// </summary>
    public List<string> Fields { get; set; } = new();
}
