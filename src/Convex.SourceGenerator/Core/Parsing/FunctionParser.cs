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
                var functionContent = ExtractFunctionContent(content, match.Index);
                var arguments = ParseArguments(functionContent);
                var returnType = ParseReturnType(functionContent);

                functions.Add(new FunctionDefinition
                {
                    Path = fullPath,
                    Name = NamingConventions.ToPascalCase(functionName),
                    ModulePath = modulePath,
                    Type = functionType,
                    Arguments = arguments,
                    ReturnType = returnType,
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
            var functionContent = ExtractFunctionContent(content, defaultMatch.Index);
            var arguments = ParseArguments(functionContent);
            var returnType = ParseReturnType(functionContent);

            functions.Add(new FunctionDefinition
            {
                Path = fullPath,
                Name = functionName,
                ModulePath = modulePath,
                Type = functionType,
                Arguments = arguments,
                ReturnType = returnType,
                IsDefaultExport = true
            });
        }

        return functions;
    }

    /// <summary>
    /// Extracts the content of a function definition (the parentheses content after query/mutation/action).
    /// </summary>
    private static string ExtractFunctionContent(string content, int startIndex)
    {
        // Find the opening parenthesis after query/mutation/action
        var parenIndex = content.IndexOf('(', startIndex);
        if (parenIndex < 0)
        {
            return string.Empty;
        }

        return ExtractBalancedContent(content, parenIndex, '(', ')');
    }

    private List<ArgumentDefinition> ParseArguments(string functionContent)
    {
        var arguments = new List<ArgumentDefinition>();

        if (string.IsNullOrEmpty(functionContent))
        {
            return arguments;
        }

        // Find the args object: args: { ... }
        // Use a more robust approach to handle nested braces
        var argsStartMatch = Regex.Match(functionContent, @"args\s*:\s*\{");
        if (!argsStartMatch.Success)
        {
            return arguments;
        }

        var braceStart = argsStartMatch.Index + argsStartMatch.Length - 1;
        var argsObjectContent = ExtractBalancedContent(functionContent, braceStart, '{', '}');

        if (string.IsNullOrEmpty(argsObjectContent) || argsObjectContent.Length < 2)
        {
            return arguments;
        }

        // Remove outer braces
        var argsContent = argsObjectContent.Substring(1, argsObjectContent.Length - 2);
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

    /// <summary>
    /// Parses the return type from a function definition.
    /// Looks for `returns: v.xxx()` pattern.
    /// </summary>
    private ValidatorType? ParseReturnType(string functionContent)
    {
        if (string.IsNullOrEmpty(functionContent))
        {
            return null;
        }

        // Find the returns property: returns: v.xxx(...)
        var returnsMatch = Regex.Match(functionContent, @"returns\s*:\s*(v\.[^,}]+)");
        if (!returnsMatch.Success)
        {
            return null;
        }

        // Extract the full validator expression
        var validatorStart = returnsMatch.Groups[1].Index;
        var validatorExpr = ExtractFullValidator(functionContent, validatorStart);

        if (string.IsNullOrEmpty(validatorExpr))
        {
            return null;
        }

        return _validatorParser.Parse(validatorExpr);
    }
}
