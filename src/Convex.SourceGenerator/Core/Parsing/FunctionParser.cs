#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.TypeMapping;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Core.Parsing;

/// <summary>
/// Parses TypeScript files to extract Convex function definitions.
/// </summary>
public class FunctionParser : TypeScriptParserBase
{
    private readonly ValidatorParser _validatorParser = new();

    /// <summary>
    /// Extracts the module path from a file path.
    /// </summary>
    public static string ExtractModulePath(string filePath)
    {
        var normalizedPath = filePath.Replace("\\", "/");

        var convexIndex = normalizedPath.LastIndexOf("/convex/", StringComparison.OrdinalIgnoreCase);
        if (convexIndex < 0)
        {
            convexIndex = normalizedPath.IndexOf("convex/", StringComparison.OrdinalIgnoreCase);
            if (convexIndex < 0)
            {
                return string.Empty;
            }

            var relativePath = normalizedPath.Substring(convexIndex + 7);
            if (relativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 3);
            }
            return relativePath;
        }

        var relPath = normalizedPath.Substring(convexIndex + 8);
        if (relPath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
        {
            relPath = relPath.Substring(0, relPath.Length - 3);
        }
        return relPath;
    }

    /// <summary>
    /// Parses a TypeScript file and returns function definitions.
    /// </summary>
    public List<FunctionDefinition> Parse(string content, string modulePath)
    {
        var functions = new List<FunctionDefinition>();

        // Parse named exports: export const functionName = query|mutation|action(...)
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
                var arguments = ParseArguments(content, match.Index);

                functions.Add(new FunctionDefinition
                {
                    Path = fullPath,
                    Name = NamingConventions.ToPascalCase(functionName),
                    ModulePath = modulePath,
                    Type = functionType,
                    Arguments = arguments,
                    IsDefaultExport = false
                });
            }
        }

        // Parse default exports: export default query|mutation|action(...)
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

            var fullPath = modulePath;
            var parts = modulePath.Split('/');
            var functionName = NamingConventions.ToPascalCase(parts.Last());
            var arguments = ParseArguments(content, defaultMatch.Index);

            functions.Add(new FunctionDefinition
            {
                Path = fullPath,
                Name = functionName,
                ModulePath = modulePath,
                Type = functionType,
                Arguments = arguments,
                IsDefaultExport = true
            });
        }

        return functions;
    }

    private List<ArgumentDefinition> ParseArguments(string content, int startIndex)
    {
        var arguments = new List<ArgumentDefinition>();

        // Find the args object: args: { ... }
        var argsPattern = @"args\s*:\s*\{([^}]*)\}";
        var argsMatch = Regex.Match(content.Substring(startIndex), argsPattern, RegexOptions.Singleline);

        if (!argsMatch.Success)
        {
            return arguments;
        }

        var argsContent = argsMatch.Groups[1].Value;
        var fields = _validatorParser.ParseFields(argsContent);

        foreach (var field in fields)
        {
            var csharpType = ConvexTypeMapper.MapToCSharpType(field.Type);

            arguments.Add(new ArgumentDefinition
            {
                Name = field.Name,
                CSharpType = csharpType,
                IsOptional = field.IsOptional,
                ValidatorType = field.Type
            });
        }

        return arguments;
    }
}
