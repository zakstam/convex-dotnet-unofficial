#nullable enable

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Convex.SourceGenerator.Core.Models;

namespace Convex.SourceGenerator.Core.Parsing;

/// <summary>
/// Parses Convex validator expressions (v.string(), v.object(), etc.)
/// </summary>
public class ValidatorParser : TypeScriptParserBase
{
    /// <summary>
    /// Parses a validator expression and returns a ValidatorType.
    /// </summary>
    public ValidatorType Parse(string expr)
    {
        expr = expr.Trim();

        // Simple types
        if (expr.StartsWith("v.string()"))
        {
            return ValidatorType.Simple(ValidatorKind.String);
        }

        if (expr.StartsWith("v.number()"))
        {
            return ValidatorType.Simple(ValidatorKind.Number);
        }

        if (expr.StartsWith("v.float64()"))
        {
            return ValidatorType.Simple(ValidatorKind.Float64);
        }

        if (expr.StartsWith("v.int64()"))
        {
            return ValidatorType.Simple(ValidatorKind.Int64);
        }

        if (expr.StartsWith("v.boolean()"))
        {
            return ValidatorType.Simple(ValidatorKind.Boolean);
        }

        if (expr.StartsWith("v.bytes()"))
        {
            return ValidatorType.Simple(ValidatorKind.Bytes);
        }

        if (expr.StartsWith("v.null()"))
        {
            return ValidatorType.Simple(ValidatorKind.Null);
        }

        if (expr.StartsWith("v.any()"))
        {
            return ValidatorType.Simple(ValidatorKind.Any);
        }

        // v.id("tableName")
        var idMatch = Regex.Match(expr, @"^v\.id\s*\(\s*[""']([^""']+)[""']\s*\)");
        if (idMatch.Success)
        {
            return ValidatorType.Id(idMatch.Groups[1].Value);
        }

        // v.literal("value") or v.literal(123)
        var literalMatch = Regex.Match(expr, @"^v\.literal\s*\(\s*(?:[""']([^""']*)[""']|([^)]+))\s*\)");
        if (literalMatch.Success)
        {
            var value = literalMatch.Groups[1].Success ? literalMatch.Groups[1].Value : literalMatch.Groups[2].Value;
            return ValidatorType.Literal(value);
        }

        // v.optional(...)
        if (expr.StartsWith("v.optional("))
        {
            var innerContent = ExtractParenContent(expr, "v.optional(".Length - 1);
            var innerType = Parse(innerContent);
            return ValidatorType.Optional(innerType);
        }

        // v.array(...)
        if (expr.StartsWith("v.array("))
        {
            var innerContent = ExtractParenContent(expr, "v.array(".Length - 1);
            var elementType = Parse(innerContent);
            return ValidatorType.Array(elementType);
        }

        // v.object({...})
        if (expr.StartsWith("v.object("))
        {
            var innerContent = ExtractParenContent(expr, "v.object(".Length - 1);
            var objectContent = ExtractObjectContent("(" + innerContent + ")");
            var fields = ParseFields(objectContent);
            return ValidatorType.Object(fields);
        }

        // v.union(...)
        if (expr.StartsWith("v.union("))
        {
            var innerContent = ExtractParenContent(expr, "v.union(".Length - 1);
            var members = ParseUnionMembers(innerContent);
            return ValidatorType.Union(members);
        }

        // v.record(keyValidator, valueValidator)
        if (expr.StartsWith("v.record("))
        {
            var innerContent = ExtractParenContent(expr, "v.record(".Length - 1);
            var parts = SplitTopLevelCommas(innerContent);
            if (parts.Count >= 2)
            {
                var keyType = Parse(parts[0]);
                var valueType = Parse(parts[1]);
                return ValidatorType.Record(keyType, valueType);
            }
        }

        // Default to any
        return ValidatorType.Simple(ValidatorKind.Any);
    }

    /// <summary>
    /// Parses fields from an object content string.
    /// </summary>
    public List<FieldDefinition> ParseFields(string objectContent)
    {
        var fields = new List<FieldDefinition>();

        var i = 0;
        while (i < objectContent.Length)
        {
            // Skip whitespace
            while (i < objectContent.Length && char.IsWhiteSpace(objectContent[i]))
            {
                i++;
            }

            if (i >= objectContent.Length)
            {
                break;
            }

            // Match field name
            var fieldNameMatch = Regex.Match(objectContent.Substring(i), @"^(\w+)\s*:");
            if (!fieldNameMatch.Success)
            {
                i++;
                continue;
            }

            var fieldName = fieldNameMatch.Groups[1].Value;
            i += fieldNameMatch.Length;

            // Skip whitespace
            while (i < objectContent.Length && char.IsWhiteSpace(objectContent[i]))
            {
                i++;
            }

            // Extract validator expression
            var fullValidator = ExtractFullValidator(objectContent, i);
            if (string.IsNullOrEmpty(fullValidator))
            {
                break;
            }

            i += fullValidator.Length;

            // Skip trailing comma
            while (i < objectContent.Length && (char.IsWhiteSpace(objectContent[i]) || objectContent[i] == ','))
            {
                i++;
            }

            var validator = Parse(fullValidator);
            var isOptional = validator.Kind == ValidatorKind.Optional;

            fields.Add(new FieldDefinition
            {
                Name = fieldName,
                Type = validator,
                IsOptional = isOptional
            });
        }

        return fields;
    }

    private List<ValidatorType> ParseUnionMembers(string content)
    {
        var members = new List<ValidatorType>();
        var parts = SplitTopLevelCommas(content);

        foreach (var part in parts)
        {
            members.Add(Parse(part.Trim()));
        }

        return members;
    }

    private static List<string> SplitTopLevelCommas(string content)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inString)
            {
                if (c == stringChar && (i == 0 || content[i - 1] != '\\'))
                {
                    inString = false;
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                inString = true;
                stringChar = c;
                continue;
            }

            if (c == '(' || c == '{' || c == '[')
            {
                depth++;
            }
            else if (c == ')' || c == '}' || c == ']')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(content.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        if (start < content.Length)
        {
            parts.Add(content.Substring(start).Trim());
        }

        return parts;
    }
}
