#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents a generated C# enum from a string literal union.
/// </summary>
public class EnumDefinition
{
    /// <summary>
    /// The PascalCase name for the enum (e.g., "GameStatus").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The enum values with their original string literal values.
    /// Key: PascalCase enum member name, Value: Original string literal value.
    /// </summary>
    public List<EnumMember> Members { get; set; } = new();

    /// <summary>
    /// Optional description for documentation.
    /// </summary>
    public string? Description { get; set; }

    public override bool Equals(object? obj) => obj is EnumDefinition other && Name == other.Name;

    public override int GetHashCode() => Name.GetHashCode();
}

/// <summary>
/// Represents a member of a generated enum.
/// </summary>
public class EnumMember
{
    /// <summary>
    /// The PascalCase name for the enum member (e.g., "Waiting").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The original string literal value (e.g., "waiting").
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
