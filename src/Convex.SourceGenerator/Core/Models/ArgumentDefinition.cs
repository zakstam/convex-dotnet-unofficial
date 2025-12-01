#nullable enable

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents an argument definition for a Convex function.
/// </summary>
public class ArgumentDefinition
{
    /// <summary>
    /// The name of the argument as it appears in the function definition.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The C# type string for this argument.
    /// </summary>
    public string CSharpType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this argument is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// The underlying validator type (for more complex type generation).
    /// </summary>
    public ValidatorType? ValidatorType { get; set; }
}
