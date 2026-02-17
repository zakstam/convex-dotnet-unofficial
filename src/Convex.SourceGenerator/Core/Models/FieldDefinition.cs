#nullable enable

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents field definition.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// The name of the field as it appears in the schema/function.
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
    /// Gets or sets a value indicating whether create.
    /// </summary>
    public static FieldDefinition Create(string name, ValidatorType type, bool isOptional = false) =>
        new() { Name = name, Type = type, IsOptional = isOptional };
}
