#nullable enable

using Microsoft.CodeAnalysis;

namespace Convex.SourceGenerator;

/// <summary>
/// Diagnostic descriptors for the Convex source generator.
/// </summary>
public static class Diagnostics
{
    private const string Category = "Convex.SourceGenerator";

    /// <summary>
    /// CVX001: Schema file not found or empty.
    /// </summary>
    public static readonly DiagnosticDescriptor SchemaNotFound = new(
        id: "CVX001",
        title: "Schema Not Found",
        messageFormat: "No schema.ts file found in the Convex backend folder. Models will not be generated.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source generator could not find a schema.ts file. Ensure your Convex backend folder is properly configured and contains a schema.ts file.");

    /// <summary>
    /// CVX002: Invalid table definition in schema.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidTableDefinition = new(
        id: "CVX002",
        title: "Invalid Table Definition",
        messageFormat: "Failed to parse table '{0}' in schema.ts: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The table definition could not be parsed. Check that the table follows the Convex schema format.");

    /// <summary>
    /// CVX003: Unsupported validator type.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedValidator = new(
        id: "CVX003",
        title: "Unsupported Validator",
        messageFormat: "Unsupported validator type '{0}' in field '{1}'. Defaulting to 'object'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The validator type is not supported by the source generator. The field will be typed as 'object'.");

    /// <summary>
    /// CVX004: Invalid function definition.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFunctionDefinition = new(
        id: "CVX004",
        title: "Invalid Function Definition",
        messageFormat: "Failed to parse function in '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The function definition could not be parsed. Check that the function follows the Convex function format.");

    /// <summary>
    /// CVX005: Type mapping failure.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMappingFailure = new(
        id: "CVX005",
        title: "Type Mapping Failure",
        messageFormat: "Failed to map type for '{0}': {1}. Using fallback type '{2}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The validator type could not be mapped to a C# type. A fallback type will be used.");

    /// <summary>
    /// CVX006: Return type parse failure.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnTypeParseFailure = new(
        id: "CVX006",
        title: "Return Type Parse Failure",
        messageFormat: "Failed to parse return type for function '{0}': {1}. Return type will be 'object?'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The function's return type could not be parsed. The return type will default to 'object?'.");

    /// <summary>
    /// CVX007: Module generation error.
    /// </summary>
    public static readonly DiagnosticDescriptor ModuleGenerationError = new(
        id: "CVX007",
        title: "Module Generation Error",
        messageFormat: "Generation module '{0}' failed: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A generation module encountered an error. Some generated code may be missing.");

    /// <summary>
    /// CVX008: Parse error in TypeScript file.
    /// </summary>
    public static readonly DiagnosticDescriptor ParseError = new(
        id: "CVX008",
        title: "TypeScript Parse Error",
        messageFormat: "Failed to parse '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The TypeScript file could not be parsed. Check the file for syntax errors.");

    /// <summary>
    /// CVX009: No functions found.
    /// </summary>
    public static readonly DiagnosticDescriptor NoFunctionsFound = new(
        id: "CVX009",
        title: "No Functions Found",
        messageFormat: "No Convex functions found in the backend folder. Function constants will not be generated.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source generator did not find any query, mutation, or action exports in the TypeScript files.");

    /// <summary>
    /// CVX010: Complex union type.
    /// </summary>
    public static readonly DiagnosticDescriptor ComplexUnionType = new(
        id: "CVX010",
        title: "Complex Union Type",
        messageFormat: "Complex union type for '{0}' cannot be represented as a C# type. Consider using string literal unions for better type safety.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The union type contains mixed types that cannot be easily represented in C#. The type will default to 'object'.");

    /// <summary>
    /// CVX011: Nested args parsing limitation.
    /// </summary>
    public static readonly DiagnosticDescriptor NestedArgsLimitation = new(
        id: "CVX011",
        title: "Nested Arguments Limitation",
        messageFormat: "Function '{0}' has deeply nested arguments that may not be fully parsed. Consider simplifying the argument structure.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The function arguments contain deeply nested objects that the parser may not fully capture.");

    /// <summary>
    /// CVX012: Generation successful.
    /// </summary>
    public static readonly DiagnosticDescriptor GenerationSuccessful = new(
        id: "CVX012",
        title: "Generation Successful",
        messageFormat: "Successfully generated {0} models and {1} function definitions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "The source generation completed successfully.");

    /// <summary>
    /// CVX013: No TypeScript files found.
    /// </summary>
    public static readonly DiagnosticDescriptor NoFilesFound = new(
        id: "CVX013",
        title: "No TypeScript Files Found",
        messageFormat: "No Convex TypeScript files found in AdditionalFiles. Set ConvexBackendPath or add .ts files as AdditionalFiles if you want to use the source generator.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source generator could not find any TypeScript files to process. This is expected if you're not using the source generator. To enable it, set ConvexBackendPath to your Convex backend folder.");

    /// <summary>
    /// CVX014: File skipped due to path.
    /// </summary>
    public static readonly DiagnosticDescriptor FileSkipped = new(
        id: "CVX014",
        title: "File Skipped",
        messageFormat: "File '{0}' was skipped because its path does not contain 'convex/'. Ensure the file is inside a 'convex' folder.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The TypeScript file was not processed because the generator could not determine its module path. Files must be inside a folder named 'convex' for the generator to extract the correct function path.");
}
