namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Event arguments for schema mismatch events.
/// </summary>
/// <remarks>
/// Executes the schema mismatch event args operation.
/// </remarks>
public sealed class SchemaMismatchEventArgs(
    string functionName,
    string expectedType,
    string actualType,
    IReadOnlyList<string> validationErrors,
    object? actualValue = null) : EventArgs
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
    /// Gets the actual value.
    /// </summary>
    public object? ActualValue { get; } = actualValue;
}
