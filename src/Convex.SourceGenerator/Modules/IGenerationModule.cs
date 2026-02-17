#nullable enable

using System.Collections.Generic;
using Convex.SourceGenerator.Core.Models;

namespace Convex.SourceGenerator.Modules;

/// <summary>
/// Defines operations for i generation module.
/// </summary>
public interface IGenerationModule
{
    /// <summary>
    /// The name of this module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this module is enabled based on the options.
    /// </summary>
    bool IsEnabled(GeneratorOptions options);

    /// <summary>
    /// Generates source files from the parsed data.
    /// </summary>
    IEnumerable<GeneratedFile> Generate(
        IReadOnlyList<TableDefinition> tables,
        IReadOnlyList<FunctionDefinition> functions,
        GeneratorOptions options);
}

/// <summary>
/// Represents generated file.
/// </summary>
public class GeneratedFile
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Options for the generator.
/// </summary>
public class GeneratorOptions
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = "Convex.Generated";

    // Models and Args go in the base namespace for backward compatibility
    /// <summary>
    /// Gets the models namespace.
    /// </summary>
    public string ModelsNamespace => Namespace;
    /// <summary>
    /// Gets the args namespace.
    /// </summary>
    public string ArgsNamespace => Namespace;
    /// <summary>
    /// Gets the services namespace.
    /// </summary>
    public string ServicesNamespace => $"{Namespace}.Services";

    /// <summary>
    /// Gets or sets a value indicating whether model types are generated.
    /// </summary>
    public bool GenerateModels { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether function constants are generated.
    /// </summary>
    public bool GenerateFunctions { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether argument types are generated.
    /// </summary>
    public bool GenerateArgs { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether service wrappers are generated.
    /// </summary>
    public bool GenerateServices { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether dependency injection helpers are generated.
    /// </summary>
    public bool GenerateDI { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether typed identifier wrappers are generated.
    /// </summary>
    public bool GenerateTypedIds { get; set; } = false;
}
