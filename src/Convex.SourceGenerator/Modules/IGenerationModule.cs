#nullable enable

using System.Collections.Generic;
using Convex.SourceGenerator.Core.Models;

namespace Convex.SourceGenerator.Modules;

/// <summary>
/// Represents a generation module that produces source code.
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
/// Represents a generated source file.
/// </summary>
public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Options for the generator.
/// </summary>
public class GeneratorOptions
{
    public string Namespace { get; set; } = "Convex.Generated";

    // Models and Args go in the base namespace for backward compatibility
    public string ModelsNamespace => Namespace;
    public string ArgsNamespace => Namespace;
    public string ServicesNamespace => $"{Namespace}.Services";

    public bool GenerateModels { get; set; } = true;
    public bool GenerateFunctions { get; set; } = true;
    public bool GenerateArgs { get; set; } = true;
    public bool GenerateServices { get; set; } = false;
    public bool GenerateDI { get; set; } = false;
    public bool GenerateTypedIds { get; set; } = false;
}
