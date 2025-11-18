namespace Convex.Client.Shared.Validation;

/// <summary>
/// Result of schema validation.
/// </summary>
public sealed class SchemaValidationResult
{
    /// <summary>
    /// Gets whether validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the expected type name.
    /// </summary>
    public string ExpectedType { get; }

    /// <summary>
    /// Gets the actual type name.
    /// </summary>
    public string ActualType { get; }

    private SchemaValidationResult(
        bool isValid,
        string expectedType,
        string actualType,
        IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        ExpectedType = expectedType;
        ActualType = actualType;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static SchemaValidationResult Success(string expectedType, string actualType) => new SchemaValidationResult(true, expectedType, actualType, []);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static SchemaValidationResult Failure(
        string expectedType,
        string actualType,
        params string[] errors) => new SchemaValidationResult(false, expectedType, actualType, errors);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static SchemaValidationResult Failure(
        string expectedType,
        string actualType,
        IReadOnlyList<string> errors) => new SchemaValidationResult(false, expectedType, actualType, errors);
}
