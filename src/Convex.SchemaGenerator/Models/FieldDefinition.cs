#nullable enable

namespace Convex.SchemaGenerator.Models;

/// <summary>
/// Represents a field definition in a Convex table.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// The name of the field as it appears in the schema.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The validator type for this field.
    /// </summary>
    public ValidatorType Type { get; set; } = null!;

    /// <summary>
    /// Whether this field is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Creates a new field definition.
    /// </summary>
    public static FieldDefinition Create(string name, ValidatorType type, bool isOptional = false) =>
        new() { Name = name, Type = type, IsOptional = isOptional };
}
