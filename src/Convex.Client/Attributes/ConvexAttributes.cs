using System;

namespace Convex.Client.Attributes;

/// <summary>
/// Enumeration of Convex function types.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Query function - read-only operations.
    /// </summary>
    Query,

    /// <summary>
    /// Mutation function - state-changing operations.
    /// </summary>
    Mutation,

    /// <summary>
    /// Action function - side-effect operations.
    /// </summary>
    Action
}

/// <summary>
/// Base attribute for marking Convex functions that should be generated.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConvexFunctionAttribute"/> class.
/// </remarks>
/// <param name="functionName">The Convex function name.</param>
/// <param name="functionType">The function type.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ConvexFunctionAttribute(string functionName, FunctionType functionType = FunctionType.Query) : Attribute
{
    /// <summary>
    /// Gets the Convex function name.
    /// </summary>
    public string FunctionName { get; } = functionName ?? throw new ArgumentNullException(nameof(functionName));

    /// <summary>
    /// Gets the function type.
    /// </summary>
    public FunctionType FunctionType { get; } = functionType;
}

/// <summary>
/// Attribute for marking Convex query functions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConvexQueryAttribute"/> class.
/// </remarks>
/// <param name="functionName">The Convex function name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ConvexQueryAttribute(string functionName) : ConvexFunctionAttribute(functionName, FunctionType.Query)
{
}

/// <summary>
/// Attribute for marking Convex mutation functions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConvexMutationAttribute"/> class.
/// </remarks>
/// <param name="functionName">The Convex function name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ConvexMutationAttribute(string functionName) : ConvexFunctionAttribute(functionName, FunctionType.Mutation)
{
}

/// <summary>
/// Attribute for marking Convex action functions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConvexActionAttribute"/> class.
/// </remarks>
/// <param name="functionName">The Convex function name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ConvexActionAttribute(string functionName) : ConvexFunctionAttribute(functionName, FunctionType.Action)
{
}

/// <summary>
/// Attribute for marking a class as a Convex table.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class ConvexTableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the table name override. If not specified, the class name will be used.
    /// </summary>
    public string? TableName { get; set; }
}

/// <summary>
/// Attribute for marking a property as indexed in Convex.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConvexIndexAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the index name. If not specified, the property name will be used.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Gets or sets whether this is a unique index.
    /// </summary>
    public bool IsUnique { get; set; }
}

/// <summary>
/// Attribute for marking a property as searchable in Convex.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConvexSearchIndexAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the search index name. If not specified, the property name will be used.
    /// </summary>
    public string? IndexName { get; set; }
}

/// <summary>
/// Attribute for marking a property as a foreign key reference to another table.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConvexForeignKeyAttribute"/> class.
/// </remarks>
/// <param name="tableName">The name of the referenced table.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConvexForeignKeyAttribute(string tableName) : Attribute
{
    /// <summary>
    /// Gets the name of the referenced table.
    /// </summary>
    public string TableName { get; } = tableName ?? throw new ArgumentNullException(nameof(tableName));
}

/// <summary>
/// Attribute for specifying validation constraints on a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConvexValidationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the minimum value (for numbers). Default is double.MinValue.
    /// </summary>
    public double Min { get; set; } = double.MinValue;

    /// <summary>
    /// Gets or sets the maximum value (for numbers). Default is double.MaxValue.
    /// </summary>
    public double Max { get; set; } = double.MaxValue;

    /// <summary>
    /// Gets or sets the minimum length (for strings and arrays). Default is 0.
    /// </summary>
    public int MinLength { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum length (for strings and arrays). Default is int.MaxValue.
    /// </summary>
    public int MaxLength { get; set; } = int.MaxValue;
}

/// <summary>
/// Attribute for excluding a property from the Convex schema.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConvexIgnoreAttribute : Attribute
{
}

