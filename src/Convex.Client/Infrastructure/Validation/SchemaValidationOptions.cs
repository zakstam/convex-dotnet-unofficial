namespace Convex.Client.Infrastructure.Validation;

/// <summary>
/// Options for configuring schema validation behavior.
/// </summary>
public sealed class SchemaValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether validate on query.
    /// Default: false.
    /// </summary>
    public bool ValidateOnQuery { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validate on mutation.
    /// Default: false.
    /// </summary>
    public bool ValidateOnMutation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validate on action.
    /// Default: false.
    /// </summary>
    public bool ValidateOnAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validate on subscription.
    /// Default: false.
    /// </summary>
    public bool ValidateOnSubscription { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether throw on validation error.
    /// If false, validation errors are logged and reported via events.
    /// Default: false (log only).
    /// </summary>
    public bool ThrowOnValidationError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether strict type checking.
    /// When true, validates exact type matches including nullability.
    /// When false, allows compatible types (e.g., int -> long, null -> nullable).
    /// Default: false.
    /// </summary>
    public bool StrictTypeChecking { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationOptions"/> class.
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
    /// Executes the strict operation.
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
    /// Executes the log only operation.
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
