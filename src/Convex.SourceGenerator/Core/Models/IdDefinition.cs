#nullable enable

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents a strongly-typed document ID type definition.
/// </summary>
public class IdDefinition
{
    /// <summary>
    /// The PascalCase name for the ID type (e.g., "UserId", "MessageId").
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// The original Convex table name (e.g., "users", "messages").
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    public override bool Equals(object? obj) => obj is IdDefinition other && TableName == other.TableName;

    public override int GetHashCode() => TableName.GetHashCode();
}
