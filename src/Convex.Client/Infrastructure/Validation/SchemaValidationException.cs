using Convex.Client.Infrastructure.ErrorHandling;

namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Exception thrown when schema validation fails.
/// </summary>
/// <remarks>
/// Creates a new schema validation exception.
/// </remarks>
public sealed class SchemaValidationException(
    string functionName,
    string expectedType,
    string actualType,
    IReadOnlyList<string> validationErrors) : ConvexException($"Schema validation failed for '{functionName}': Expected {expectedType}, got {actualType}")
{
    /// <summary>
    /// Gets the function name that failed validation.
    /// </summary>
    public string FunctionName { get; } = functionName;

    /// <summary>
    /// Gets the expected type name.
    /// </summary>
    public string ExpectedType { get; } = expectedType;

    /// <summary>
    /// Gets the actual type name received.
    /// </summary>
    public string ActualType { get; } = actualType;

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; } = validationErrors;

    /// <summary>
    /// Creates a new schema validation exception with a single error.
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
