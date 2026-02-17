namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Result of schema validation.
/// </summary>
public sealed class SchemaValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the expected type.
    /// </summary>
    public string ExpectedType { get; }

    /// <summary>
    /// Gets the actual type.
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
    /// Gets the success.
    /// </summary>
    public static SchemaValidationResult Success(string expectedType, string actualType) => new SchemaValidationResult(true, expectedType, actualType, []);

    /// <summary>
    /// Executes the failure operation.
    /// </summary>
    public static SchemaValidationResult Failure(
        string expectedType,
        string actualType,
        params string[] errors) => new SchemaValidationResult(false, expectedType, actualType, errors);

    /// <summary>
    /// Executes the failure operation.
    /// </summary>
    public static SchemaValidationResult Failure(
        string expectedType,
        string actualType,
        IReadOnlyList<string> errors) => new SchemaValidationResult(false, expectedType, actualType, errors);
}
