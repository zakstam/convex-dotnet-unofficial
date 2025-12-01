#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents a Convex function (query, mutation, or action).
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// The full path to call the function (e.g., "users:create" or "functions/messages:send").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The PascalCase name for the generated constant (e.g., "Create", "Send").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The module path without the function name (e.g., "users" or "functions/messages").
    /// </summary>
    public string ModulePath { get; set; } = string.Empty;

    /// <summary>
    /// The type of function: "Query", "Mutation", or "Action".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The arguments for this function.
    /// </summary>
    public List<ArgumentDefinition> Arguments { get; set; } = new();

    /// <summary>
    /// Whether this is a default export.
    /// </summary>
    public bool IsDefaultExport { get; set; }

    public override bool Equals(object? obj) => obj is FunctionDefinition other && Path == other.Path;

    public override int GetHashCode() => Path.GetHashCode();
}
