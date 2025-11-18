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
        // Register the attribute that users will use to mark their backend path
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("ConvexBackendAttribute.g.cs", SourceText.From(AttributeText, Encoding.UTF8));
        });

        // Find all additional text files named "api.d.ts"
        var apiFiles = context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path).Equals("api.d.ts", System.StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) => file.GetText(ct)?.ToString() ?? string.Empty);

        // Combine all api.d.ts files
        var combinedFiles = apiFiles.Collect();

        // Generate the constants class
        context.RegisterSourceOutput(combinedFiles, (spc, apiContents) =>
        {
            if (apiContents.Length == 0)
            {
                return;
            }

            // Parse all api.d.ts files and combine function names
            var allFunctions = new HashSet<ConvexFunction>();

            foreach (var content in apiContents)
            {
                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                var functions = ParseApiFile(content);
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

    private static List<ConvexFunction> ParseApiFile(string content)
    {
        var functions = new List<ConvexFunction>();

        // Parse the ApiFromModules<{ ... }> block
        // Looking for patterns like: "functions/deleteMessage": typeof functions_deleteMessage;
        var pattern = @"""([^""]+)""\s*:\s*typeof\s+([a-zA-Z0-9_]+)";
        var matches = Regex.Matches(content, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var functionPath = match.Groups[1].Value;

                // Skip non-function entries like "storage"
                if (!functionPath.Contains("/"))
                {
                    continue;
                }

                // Determine function type from the function file (would need to read actual files)
                // For now, we'll infer from common naming patterns
                var functionName = functionPath.Split('/').Last();
                var functionType = InferFunctionType(functionName);

                functions.Add(new ConvexFunction
                {
                    Path = functionPath,
                    Name = ToPascalCase(functionName),
                    Type = functionType
                });
            }
        }

        return functions;
    }

    private static string InferFunctionType(string functionName)
    {
        // Common patterns for inferring function types
        var lowerName = functionName.ToLowerInvariant();

        if (lowerName.StartsWith("get") || lowerName.Contains("list") || lowerName.Contains("search"))
        {
            return "Query";
        }
        else if (lowerName.StartsWith("send") || lowerName.StartsWith("create") ||
                 lowerName.StartsWith("update") || lowerName.StartsWith("delete") ||
                 lowerName.StartsWith("edit") || lowerName.Contains("toggle") ||
                 lowerName.Contains("set"))
        {
            return "Mutation";
        }

        // Default to Action if uncertain
        return "Action";
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

        // Group by type
        var grouped = functions.GroupBy(f => f.Type).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var className = Pluralize(group.Key);
            _ = sb.AppendLine($"        /// <summary>");
            _ = sb.AppendLine($"        /// {group.Key} functions");
            _ = sb.AppendLine($"        /// </summary>");
            _ = sb.AppendLine($"        public static class {className}");
            _ = sb.AppendLine($"        {{");

            foreach (var func in group.OrderBy(f => f.Name))
            {
                _ = sb.AppendLine($"            /// <summary>{group.Key}: {func.Path}</summary>");
                _ = sb.AppendLine($"            public const string {func.Name} = \"{func.Path}\";");
                _ = sb.AppendLine();
            }

            _ = sb.AppendLine($"        }}");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("    }");
        _ = sb.AppendLine("}");

        return sb.ToString();
    }

    private class ConvexFunction
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public override bool Equals(object? obj) => obj is ConvexFunction other && Path == other.Path;

        public override int GetHashCode() => Path.GetHashCode();
    }
}
