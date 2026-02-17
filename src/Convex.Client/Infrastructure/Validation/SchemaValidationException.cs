using Convex.Client.Infrastructure.ErrorHandling;

namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Exception thrown when schema validation fails.
/// </summary>
/// <remarks>
/// Executes the schema validation exception operation.
/// </remarks>
public sealed class SchemaValidationException(
    string functionName,
    string expectedType,
    string actualType,
    IReadOnlyList<string> validationErrors) : ConvexException($"Schema validation failed for '{functionName}': Expected {expectedType}, got {actualType}")
{
    /// <summary>
    /// Gets the function name.
    /// </summary>
    public string FunctionName { get; } = functionName;

    /// <summary>
    /// Gets the expected type.
    /// </summary>
    public string ExpectedType { get; } = expectedType;

    /// <summary>
    /// Gets the actual type.
    /// </summary>
    public string ActualType { get; } = actualType;

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; } = validationErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
    /// </summary>
    public SchemaValidationException(
        string functionName,
        string expectedType,
        string actualType,
        string validationError)
        : this(functionName, expectedType, actualType, new[] { validationError })
    {
    }
}
