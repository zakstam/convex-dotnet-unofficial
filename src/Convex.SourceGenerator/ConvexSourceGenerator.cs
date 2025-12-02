#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Parsing;
using Convex.SourceGenerator.Modules;

namespace Convex.SourceGenerator;

/// <summary>
/// Unified source generator for Convex .NET SDK.
/// Generates type-safe constants, model classes, argument types, and service wrappers.
/// </summary>
[Generator]
public class ConvexSourceGenerator : IIncrementalGenerator
{
    private const string AttributeText = @"
namespace Convex.Generated
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ConvexBackendAttribute : System.Attribute
    {
        public ConvexBackendAttribute(string path) { }
    }
}";

    private readonly List<IGenerationModule> _modules = new()
    {
        new EnumModule(),
        new IdModule(),
        new SchemaModule(),
        new FunctionsModule(),
        new ArgumentsModule(),
        new ServiceModule(),
        new DIModule()
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute unconditionally
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("ConvexBackendAttribute.g.cs", SourceText.From(AttributeText, Encoding.UTF8));
        });

        // Get configuration options
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) => ParseOptions(provider.GlobalOptions));

        // Find all TypeScript files
        var tsFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) &&
                          !file.Path.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) => new FileWithPath(file.Path, file.GetText(ct)?.ToString() ?? string.Empty))
            .Collect();

        // Combine files with options
        var combined = tsFiles.Combine(optionsProvider);

        // Generate source
        context.RegisterSourceOutput(combined, (spc, data) =>
        {
            var (files, options) = data;
            GenerateSource(spc, files, options);
        });
    }

    private static GeneratorOptions ParseOptions(AnalyzerConfigOptions globalOptions)
    {
        var options = new GeneratorOptions();

        if (globalOptions.TryGetValue("build_property.ConvexGeneratedNamespace", out var ns) && !string.IsNullOrEmpty(ns))
        {
            options.Namespace = ns;
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateModels", out var genModels))
        {
            options.GenerateModels = !string.Equals(genModels, "false", StringComparison.OrdinalIgnoreCase);
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateFunctions", out var genFuncs))
        {
            options.GenerateFunctions = !string.Equals(genFuncs, "false", StringComparison.OrdinalIgnoreCase);
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateArgs", out var genArgs))
        {
            options.GenerateArgs = !string.Equals(genArgs, "false", StringComparison.OrdinalIgnoreCase);
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateServices", out var genServices))
        {
            options.GenerateServices = string.Equals(genServices, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateDI", out var genDI))
        {
            options.GenerateDI = string.Equals(genDI, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (globalOptions.TryGetValue("build_property.ConvexGenerateTypedIds", out var genTypedIds))
        {
            options.GenerateTypedIds = string.Equals(genTypedIds, "true", StringComparison.OrdinalIgnoreCase);
        }

        return options;
    }

    private void GenerateSource(
        SourceProductionContext context,
        ImmutableArray<FileWithPath> files,
        GeneratorOptions options)
    {
        if (files.IsDefaultOrEmpty)
        {
            return;
        }

        // Parse all files
        var schemaParser = new SchemaParser();
        var functionParser = new FunctionParser();

        var tables = new List<TableDefinition>();
        var functions = new HashSet<FunctionDefinition>();

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content))
            {
                continue;
            }

            try
            {
                // Check if this is a schema file
                if (file.Path.EndsWith("schema.ts", StringComparison.OrdinalIgnoreCase))
                {
                    var parsedTables = schemaParser.Parse(file.Content);
                    tables.AddRange(parsedTables);
                }
                else
                {
                    // Parse as function file
                    var modulePath = FunctionParser.ExtractModulePath(file.Path);
                    if (!string.IsNullOrEmpty(modulePath))
                    {
                        var parsedFunctions = functionParser.Parse(file.Content, modulePath);
                        foreach (var func in parsedFunctions)
                        {
                            functions.Add(func);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ParseError,
                    Location.None,
                    file.Path,
                    ex.Message));
            }
        }

        // Run each enabled module
        foreach (var module in _modules)
        {
            if (!module.IsEnabled(options))
            {
                continue;
            }

            try
            {
                var generatedFiles = module.Generate(tables, functions.ToList(), options);
                foreach (var generatedFile in generatedFiles)
                {
                    context.AddSource(generatedFile.FileName, SourceText.From(generatedFile.Content, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ModuleGenerationError,
                    Location.None,
                    module.Name,
                    ex.Message));
            }
        }
    }

    private readonly struct FileWithPath
    {
        public string Path { get; }
        public string Content { get; }

        public FileWithPath(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }
}
