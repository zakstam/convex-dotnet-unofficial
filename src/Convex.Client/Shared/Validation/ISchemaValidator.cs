namespace Convex.Client.Shared.Validation;

/// <summary>
/// Interface for validating response data against expected schemas.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates that a value matches the expected type.
    /// </summary>
    /// <typeparam name="TExpected">The expected type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="functionName">The function name for error reporting.</param>
    /// <param name="options">Validation options.</param>
    /// <returns>A validation result indicating success or failure with errors.</returns>
    SchemaValidationResult Validate<TExpected>(
        object? value,
        string functionName,
        SchemaValidationOptions options);

    /// <summary>
    /// Validates that a value matches the expected type.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="expectedType">The expected type.</param>
    /// <param name="functionName">The function name for error reporting.</param>
    /// <param name="options">Validation options.</param>
    /// <returns>A validation result indicating success or failure with errors.</returns>
    SchemaValidationResult Validate(
        object? value,
        Type expectedType,
        string functionName,
        SchemaValidationOptions options);
}
