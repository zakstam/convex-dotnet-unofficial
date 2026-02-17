#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents enum definition.
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

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj) => obj is EnumDefinition other && Name == other.Name;

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode() => Name.GetHashCode();
}

/// <summary>
/// Represents enum member.
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
