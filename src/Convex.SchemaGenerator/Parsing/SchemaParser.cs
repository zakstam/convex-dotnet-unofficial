#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Convex.SchemaGenerator.Models;

namespace Convex.SchemaGenerator.Parsing;

/// <summary>
/// Parses Convex schema.ts files to extract table definitions.
/// </summary>
public class SchemaParser
{
    // Regex to match table definitions: tableName: defineTable({...})
    private static readonly Regex TablePattern = new(
        @"(\w+)\s*:\s*defineTable\s*\(\s*(\{[\s\S]*?\})\s*\)",
        RegexOptions.Compiled);

    // Regex to match index definitions: .index("name", ["field1", "field2"])
    private static readonly Regex IndexPattern = new(
        @"\.index\s*\(\s*[""']([^""']+)[""']\s*,\s*\[([\s\S]*?)\]\s*\)",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a schema.ts file content and returns table definitions.
    /// </summary>
    public List<TableDefinition> Parse(string content)
    {
        var tables = new List<TableDefinition>();

        // Remove single-line comments
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);

        // Remove multi-line comments
        content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

        // Find the defineSchema block
        var schemaMatch = Regex.Match(content, @"defineSchema\s*\(\s*\{([\s\S]*)\}\s*\)");
        if (!schemaMatch.Success)
        {
            return tables;
        }

        var schemaContent = schemaMatch.Groups[1].Value;

        // Parse each table definition
        var tableMatches = FindTableDefinitions(schemaContent);
        foreach (var (tableName, tableBody, indexChain) in tableMatches)
        {
            var table = new TableDefinition
            {
                Name = tableName,
                PascalName = ToPascalCase(tableName)
            };

            // Parse fields from the table body
            table.Fields = ParseFields(tableBody);

            // Parse indexes from the chain
            table.Indexes = ParseIndexes(indexChain);

            tables.Add(table);
        }

        return tables;
    }

    private List<(string TableName, string TableBody, string IndexChain)> FindTableDefinitions(string schemaContent)
    {
        var results = new List<(string, string, string)>();

        // Match table name followed by defineTable
        var pattern = @"(\w+)\s*:\s*defineTable\s*\(";
        var matches = Regex.Matches(schemaContent, pattern);

        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            var startIndex = match.Index + match.Length;

            // Find the matching closing paren for defineTable, accounting for nested parens
            var tableBody = ExtractBalancedContent(schemaContent, startIndex - 1, '(', ')');
            if (string.IsNullOrEmpty(tableBody))
            {
                continue;
            }

            // The tableBody includes the outer parens, extract the object inside
            var objectContent = ExtractObjectContent(tableBody);

            // Find any index chain after the defineTable call
            var afterDefineTable = startIndex + tableBody.Length - 1;
            var indexChain = "";
            if (afterDefineTable < schemaContent.Length)
            {
                // Look for .index() calls after defineTable
                var remaining = schemaContent.Substring(afterDefineTable);
                var indexMatch = Regex.Match(remaining, @"^((?:\s*\.(?:index|searchIndex|vectorIndex)\s*\([^)]*\))+)");
                if (indexMatch.Success)
                {
                    indexChain = indexMatch.Groups[1].Value;
                }
            }

            results.Add((tableName, objectContent, indexChain));
        }

        return results;
    }

    private string ExtractBalancedContent(string content, int startIndex, char openChar, char closeChar)
    {
        if (startIndex >= content.Length || content[startIndex] != openChar)
        {
            return string.Empty;
        }

        var depth = 0;
        var start = startIndex;

        for (var i = startIndex; i < content.Length; i++)
        {
            var c = content[i];

            // Skip string literals
            if (c == '"' || c == '\'')
            {
                var quote = c;
                i++;
                while (i < content.Length && content[i] != quote)
                {
                    if (content[i] == '\\')
                    {
                        i++; // Skip escaped character
                    }

                    i++;
                }

                continue;
            }

            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    return content.Substring(start, i - start + 1);
                }
            }
        }

        return string.Empty;
    }

    private string ExtractObjectContent(string content)
    {
        // Find the first { and extract until matching }
        var braceStart = content.IndexOf('{');
        if (braceStart < 0)
        {
            return string.Empty;
        }

        var objectContent = ExtractBalancedContent(content, braceStart, '{', '}');
        if (objectContent.Length >= 2)
        {
            // Remove outer braces
            return objectContent.Substring(1, objectContent.Length - 2);
        }

        return string.Empty;
    }

    private List<FieldDefinition> ParseFields(string objectContent)
    {
        var fields = new List<FieldDefinition>();

        // Parse fields manually, tracking depth to only match top-level fields
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

            // Try to match a field name (identifier followed by colon)
            var fieldNameMatch = Regex.Match(objectContent.Substring(i), @"^(\w+)\s*:");
            if (!fieldNameMatch.Success)
            {
                i++;
                continue;
            }

            var fieldName = fieldNameMatch.Groups[1].Value;
            i += fieldNameMatch.Length;

            // Skip whitespace after colon
            while (i < objectContent.Length && char.IsWhiteSpace(objectContent[i]))
            {
                i++;
            }

            // Extract the full validator expression at this position
            var fullValidator = ExtractFullValidator(objectContent, i);
            if (string.IsNullOrEmpty(fullValidator))
            {
                break;
            }

            // Skip past the validator expression
            i += fullValidator.Length;

            // Skip any trailing comma
            while (i < objectContent.Length && (char.IsWhiteSpace(objectContent[i]) || objectContent[i] == ','))
            {
                i++;
            }

            var validator = ParseValidator(fullValidator);
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

    private string ExtractFullValidator(string content, int startIndex)
    {
        if (startIndex >= content.Length)
        {
            return string.Empty;
        }

        // Find the end of the validator expression
        var depth = 0;
        var start = startIndex;
        var inString = false;
        var stringChar = '\0';

        for (var i = startIndex; i < content.Length; i++)
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
                if (depth < 0)
                {
                    // We've gone past our expression
                    return content.Substring(start, i - start).Trim();
                }
            }
            else if (c == ',' && depth == 0)
            {
                return content.Substring(start, i - start).Trim();
            }
        }

        return content.Substring(start).Trim();
    }

    private ValidatorType ParseValidator(string expr)
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
            var innerType = ParseValidator(innerContent);
            return ValidatorType.Optional(innerType);
        }

        // v.array(...)
        if (expr.StartsWith("v.array("))
        {
            var innerContent = ExtractParenContent(expr, "v.array(".Length - 1);
            var elementType = ParseValidator(innerContent);
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
                var keyType = ParseValidator(parts[0]);
                var valueType = ParseValidator(parts[1]);
                return ValidatorType.Record(keyType, valueType);
            }
        }

        // Default to any if we can't parse
        return ValidatorType.Simple(ValidatorKind.Any);
    }

    private string ExtractParenContent(string expr, int openParenIndex)
    {
        var depth = 0;
        var start = -1;

        for (var i = openParenIndex; i < expr.Length; i++)
        {
            var c = expr[i];

            if (c == '(')
            {
                if (depth == 0)
                {
                    start = i + 1;
                }

                depth++;
            }
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return expr.Substring(start, i - start);
                }
            }
        }

        return start >= 0 ? expr.Substring(start) : string.Empty;
    }

    private List<ValidatorType> ParseUnionMembers(string content)
    {
        var members = new List<ValidatorType>();
        var parts = SplitTopLevelCommas(content);

        foreach (var part in parts)
        {
            members.Add(ParseValidator(part.Trim()));
        }

        return members;
    }

    private List<string> SplitTopLevelCommas(string content)
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

    private List<IndexDefinition> ParseIndexes(string indexChain)
    {
        var indexes = new List<IndexDefinition>();

        var matches = IndexPattern.Matches(indexChain);
        foreach (Match match in matches)
        {
            var indexName = match.Groups[1].Value;
            var fieldsStr = match.Groups[2].Value;

            // Parse field names from the array
            var fieldMatches = Regex.Matches(fieldsStr, @"[""']([^""']+)[""']");
            var fields = new List<string>();
            foreach (Match fieldMatch in fieldMatches)
            {
                fields.Add(fieldMatch.Groups[1].Value);
            }

            indexes.Add(new IndexDefinition
            {
                Name = indexName,
                Fields = fields
            });
        }

        return indexes;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle snake_case and camelCase
        var words = Regex.Split(input, @"[_\s]+");
        var result = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    result.Append(word.Substring(1));
                }
            }
        }

        return result.ToString();
    }
}
