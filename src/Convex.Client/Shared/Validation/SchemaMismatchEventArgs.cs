namespace Convex.Client.Shared.Validation;

/// <summary>
/// Event arguments for schema mismatch events.
/// </summary>
/// <remarks>
/// Creates new schema mismatch event arguments.
/// </remarks>
public sealed class SchemaMismatchEventArgs(
    string functionName,
    string expectedType,
    string actualType,
    IReadOnlyList<string> validationErrors,
    object? actualValue = null) : EventArgs
{
    /// <summary>
    /// Gets the function name where the mismatch occurred.
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
    /// Gets the raw response value.
    /// </summary>
    public object? ActualValue { get; } = actualValue;
}
