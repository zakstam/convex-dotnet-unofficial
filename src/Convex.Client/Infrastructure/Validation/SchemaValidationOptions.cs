namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Options for configuring schema validation behavior.
/// </summary>
public sealed class SchemaValidationOptions
{
    /// <summary>
    /// Gets or sets whether to validate query responses.
    /// Default: false.
    /// </summary>
    public bool ValidateOnQuery { get; set; }

    /// <summary>
    /// Gets or sets whether to validate mutation responses.
    /// Default: false.
    /// </summary>
    public bool ValidateOnMutation { get; set; }

    /// <summary>
    /// Gets or sets whether to validate action responses.
    /// Default: false.
    /// </summary>
    public bool ValidateOnAction { get; set; }

    /// <summary>
    /// Gets or sets whether to validate subscription updates.
    /// Default: false.
    /// </summary>
    public bool ValidateOnSubscription { get; set; }

    /// <summary>
    /// Gets or sets whether to throw an exception on validation errors.
    /// If false, validation errors are logged and reported via events.
    /// Default: false (log only).
    /// </summary>
    public bool ThrowOnValidationError { get; set; }

    /// <summary>
    /// Gets or sets whether to perform strict type checking.
    /// When true, validates exact type matches including nullability.
    /// When false, allows compatible types (e.g., int -> long, null -> nullable).
    /// Default: false.
    /// </summary>
    public bool StrictTypeChecking { get; set; }

    /// <summary>
    /// Creates default validation options with all validation disabled.
    /// </summary>
    public SchemaValidationOptions()
    {
        ValidateOnQuery = false;
        ValidateOnMutation = false;
        ValidateOnAction = false;
        ValidateOnSubscription = false;
        ThrowOnValidationError = false;
        StrictTypeChecking = false;
    }

    /// <summary>
    /// Creates validation options that enable all validation and throw on errors.
    /// Useful for development/testing environments.
    /// </summary>
    public static SchemaValidationOptions Strict()
    {
        return new SchemaValidationOptions
        {
            ValidateOnQuery = true,
            ValidateOnMutation = true,
            ValidateOnAction = true,
            ValidateOnSubscription = true,
            ThrowOnValidationError = true,
            StrictTypeChecking = true
        };
    }

    /// <summary>
    /// Creates validation options that enable all validation but log instead of throwing.
    /// Useful for production environments.
    /// </summary>
    public static SchemaValidationOptions LogOnly()
    {
        return new SchemaValidationOptions
        {
            ValidateOnQuery = true,
            ValidateOnMutation = true,
            ValidateOnAction = true,
            ValidateOnSubscription = true,
            ThrowOnValidationError = false,
            StrictTypeChecking = false
        };
    }
}
