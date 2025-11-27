using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Convex.FunctionGenerator;

/// <summary>
/// Source generator that creates type-safe constants for Convex function names.
/// </summary>
[Generator]
public class ConvexFunctionGenerator : IIncrementalGenerator
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute unconditionally
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("ConvexBackendAttribute.g.cs", SourceText.From(AttributeText, Encoding.UTF8));
        });

        // Find all TypeScript files (not .d.ts)
        var tsFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".ts", System.StringComparison.OrdinalIgnoreCase) &&
                          !file.Path.EndsWith(".d.ts", System.StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) => new FileWithPath(file.Path, file.GetText(ct)?.ToString() ?? string.Empty))
            .Collect();

        // Generate the constants class
        context.RegisterSourceOutput(tsFiles, (spc, tsFileList) =>
        {
            var allFunctions = new HashSet<ConvexFunction>();

            // Parse TypeScript function files
            foreach (var tsFile in tsFileList)
            {
                if (string.IsNullOrEmpty(tsFile.Content))
                {
                    continue;
                }

                // Extract module path from file path
                var modulePath = ExtractModulePath(tsFile.Path);
                if (string.IsNullOrEmpty(modulePath))
                {
                    continue;
                }

                var functions = ParseTypeScriptFile(tsFile.Content, modulePath);
                foreach (var func in functions)
                {
                    _ = allFunctions.Add(func);
                }
            }

            if (allFunctions.Count == 0)
            {
                return;
            }

            // Generate the source
            var source = GenerateConstants(allFunctions);
            spc.AddSource("ConvexFunctions.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string ExtractModulePath(string filePath)
    {
        // Normalize path separators
        var normalizedPath = filePath.Replace("\\", "/");

        // Look for "convex/" in the path
        var convexIndex = normalizedPath.LastIndexOf("/convex/", System.StringComparison.OrdinalIgnoreCase);
        if (convexIndex < 0)
        {
            // Try without leading slash
            convexIndex = normalizedPath.IndexOf("convex/", System.StringComparison.OrdinalIgnoreCase);
            if (convexIndex < 0)
            {
                return string.Empty;
            }

            // Get relative path from after "convex/"
            var relativePath = normalizedPath.Substring(convexIndex + 7);

            // Remove .ts extension
            if (relativePath.EndsWith(".ts", System.StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 3);
            }

            return relativePath;
        }

        // Get relative path from after "convex/"
        var relPath = normalizedPath.Substring(convexIndex + 8);

        // Remove .ts extension
        if (relPath.EndsWith(".ts", System.StringComparison.OrdinalIgnoreCase))
        {
            relPath = relPath.Substring(0, relPath.Length - 3);
        }

        return relPath;
    }

    private static List<ConvexFunction> ParseTypeScriptFile(string content, string modulePath)
    {
        var functions = new List<ConvexFunction>();

        // Parse exported const declarations with query/mutation/action types
        // Pattern: export const functionName = query|mutation|action({ ... })
        var namedExportPattern = @"export\s+const\s+(\w+)\s*=\s*(query|mutation|action)\s*\(";
        var matches = Regex.Matches(content, namedExportPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var functionName = match.Groups[1].Value;
                var functionTypeRaw = match.Groups[2].Value.ToLowerInvariant();

                var functionType = functionTypeRaw switch
                {
                    "query" => "Query",
                    "mutation" => "Mutation",
                    "action" => "Action",
                    _ => "Action"
                };

                var fullPath = $"{modulePath}:{functionName}";

                functions.Add(new ConvexFunction
                {
                    Path = fullPath,
                    Name = ToPascalCase(functionName),
                    ModulePath = modulePath,
                    Type = functionType
                });
            }
        }

        // Also parse export default declarations
        // Pattern: export default query|mutation|action({ ... })
        var defaultExportPattern = @"export\s+default\s+(query|mutation|action)\s*\(";
        var defaultMatch = Regex.Match(content, defaultExportPattern, RegexOptions.IgnoreCase);

        if (defaultMatch.Success && defaultMatch.Groups.Count >= 2)
        {
            var functionTypeRaw = defaultMatch.Groups[1].Value.ToLowerInvariant();

            var functionType = functionTypeRaw switch
            {
                "query" => "Query",
                "mutation" => "Mutation",
                "action" => "Action",
                _ => "Action"
            };

            // For default exports, the function name is "default" and the path is just the module path
            var fullPath = modulePath;

            // Extract function name from module path (e.g., "functions/getMessages" -> "GetMessages")
            var parts = modulePath.Split('/');
            var functionName = ToPascalCase(parts.Last());

            functions.Add(new ConvexFunction
            {
                Path = fullPath,
                Name = functionName,
                ModulePath = modulePath,
                Type = functionType
            });
        }

        return functions;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Convert camelCase or snake_case to PascalCase
        var words = Regex.Split(input, @"[_\s]+");
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                _ = result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    _ = result.Append(word.Substring(1));
                }
            }
        }

        return result.ToString();
    }

    private static string ToModuleClassName(string modulePath)
    {
        // Convert "functions/clickEffects" to "ClickEffects"
        var parts = modulePath.Split('/');
        var lastPart = parts.Last();
        return ToPascalCase(lastPart);
    }

    private static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        // Handle common irregular plurals
        if (word.EndsWith("y", System.StringComparison.OrdinalIgnoreCase))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }

        // Default: just add 's'
        return word + "s";
    }

    private static string GenerateConstants(IEnumerable<ConvexFunction> functions)
    {
        var sb = new StringBuilder();

        _ = sb.AppendLine("// <auto-generated>");
        _ = sb.AppendLine("// This file is automatically generated from your Convex backend.");
        _ = sb.AppendLine("// Do not modify this file directly - changes will be overwritten.");
        _ = sb.AppendLine("// </auto-generated>");
        _ = sb.AppendLine();
        _ = sb.AppendLine("#nullable enable");
        _ = sb.AppendLine();
        _ = sb.AppendLine("namespace Convex.Generated");
        _ = sb.AppendLine("{");
        _ = sb.AppendLine("    /// <summary>");
        _ = sb.AppendLine("    /// Type-safe constants for Convex function names.");
        _ = sb.AppendLine("    /// </summary>");
        _ = sb.AppendLine("    public static class ConvexFunctions");
        _ = sb.AppendLine("    {");

        // Group by type first, then by module
        var grouped = functions.GroupBy(f => f.Type).OrderBy(g => g.Key);

        foreach (var typeGroup in grouped)
        {
            var className = Pluralize(typeGroup.Key);
            _ = sb.AppendLine($"        /// <summary>");
            _ = sb.AppendLine($"        /// {typeGroup.Key} functions");
            _ = sb.AppendLine($"        /// </summary>");
            _ = sb.AppendLine($"        public static class {className}");
            _ = sb.AppendLine($"        {{");

            // Group by module within the type
            var byModule = typeGroup.GroupBy(f => f.ModulePath).OrderBy(g => g.Key);

            foreach (var moduleGroup in byModule)
            {
                var moduleClassName = ToModuleClassName(moduleGroup.Key);
                var funcsInModule = moduleGroup.ToList();

                // Check if this module has only one function with the same name as the module
                // (i.e., a default export where the function name matches the file name)
                // In this case, emit the constant directly without a nested class
                if (funcsInModule.Count == 1 && funcsInModule[0].Name == moduleClassName)
                {
                    var func = funcsInModule[0];
                    _ = sb.AppendLine($"            /// <summary>{typeGroup.Key}: {func.Path}</summary>");
                    _ = sb.AppendLine($"            public const string {func.Name} = \"{func.Path}\";");
                    _ = sb.AppendLine();
                }
                else
                {
                    // Multiple functions or named exports - use nested class
                    _ = sb.AppendLine($"            /// <summary>{typeGroup.Key} functions from {moduleGroup.Key}</summary>");
                    _ = sb.AppendLine($"            public static class {moduleClassName}");
                    _ = sb.AppendLine($"            {{");

                    foreach (var func in funcsInModule.OrderBy(f => f.Name))
                    {
                        _ = sb.AppendLine($"                /// <summary>{typeGroup.Key}: {func.Path}</summary>");
                        _ = sb.AppendLine($"                public const string {func.Name} = \"{func.Path}\";");
                    }

                    _ = sb.AppendLine($"            }}");
                    _ = sb.AppendLine();
                }
            }

            _ = sb.AppendLine($"        }}");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("    }");
        _ = sb.AppendLine("}");

        return sb.ToString();
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

    private class ConvexFunction
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModulePath { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public override bool Equals(object? obj) => obj is ConvexFunction other && Path == other.Path;

        public override int GetHashCode() => Path.GetHashCode();
    }
}
