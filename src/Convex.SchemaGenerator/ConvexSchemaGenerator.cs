#nullable enable

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Convex.SchemaGenerator.CodeGen;
using Convex.SchemaGenerator.Parsing;

namespace Convex.SchemaGenerator;

/// <summary>
/// Source generator that creates type-safe C# classes from Convex schema.ts files.
/// </summary>
[Generator]
public class ConvexSchemaGenerator : IIncrementalGenerator
{
    private const string DefaultNamespace = "Convex.Generated.Models";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all additional text files named "schema.ts"
        var schemaFiles = context.AdditionalTextsProvider
            .Where(file => System.IO.Path.GetFileName(file.Path)
                .Equals("schema.ts", StringComparison.OrdinalIgnoreCase));

        // Get the namespace configuration
        var namespaceProvider = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(
                    "build_property.ConvexGeneratedNamespace",
                    out var ns);
                return ns ?? DefaultNamespace;
            });

        // Combine schema files with namespace
        var combined = schemaFiles
            .Combine(namespaceProvider)
            .Select((pair, ct) =>
            {
                var (file, ns) = pair;
                var content = file.GetText(ct)?.ToString() ?? string.Empty;
                return (Content: content, Namespace: ns, Path: file.Path);
            });

        // Collect all schema files
        var collectedFiles = combined.Collect();

        // Generate source for each schema file
        context.RegisterSourceOutput(collectedFiles, (spc, schemaInfos) =>
        {
            // Track if we need to emit the polyfill
            var needsPolyfill = false;

            foreach (var schemaInfo in schemaInfos)
            {
                if (string.IsNullOrEmpty(schemaInfo.Content))
                {
                    continue;
                }

                try
                {
                    var parser = new SchemaParser();
                    var tables = parser.Parse(schemaInfo.Content);

                    if (tables.Count == 0)
                    {
                        continue;
                    }

                    needsPolyfill = true;

                    var emitter = new CSharpEmitter(schemaInfo.Namespace);

                    foreach (var table in tables)
                    {
                        var source = emitter.EmitTableClass(table);
                        var fileName = $"{table.PascalName}.g.cs";

                        spc.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
                    }
                }
                catch (Exception ex)
                {
                    // Report diagnostic for parse errors
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "CVX101",
                            "Schema Parse Error",
                            "Failed to parse schema.ts: {0}",
                            "Convex.SchemaGenerator",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        Location.None,
                        ex.Message));
                }
            }

            // Emit the polyfill if we generated any types
            if (needsPolyfill)
            {
                var polyfill = CSharpEmitter.EmitPolyfill();
                spc.AddSource("IsExternalInit.g.cs", SourceText.From(polyfill, Encoding.UTF8));
            }
        });
    }
}
