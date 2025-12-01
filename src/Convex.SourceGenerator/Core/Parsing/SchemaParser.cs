#nullable enable

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Core.Parsing;

/// <summary>
/// Parses Convex schema.ts files to extract table definitions.
/// </summary>
public class SchemaParser : TypeScriptParserBase
{
    private static readonly Regex IndexPattern = new(
        @"\.index\s*\(\s*[""']([^""']+)[""']\s*,\s*\[([\s\S]*?)\]\s*\)",
        RegexOptions.Compiled);

    private readonly ValidatorParser _validatorParser = new();

    /// <summary>
    /// Parses a schema.ts file content and returns table definitions.
    /// </summary>
    public List<TableDefinition> Parse(string content)
    {
        var tables = new List<TableDefinition>();

        content = RemoveComments(content);

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
            var pascalName = NamingConventions.ToPascalCase(tableName);
            pascalName = NamingConventions.Singularize(pascalName);

            var table = new TableDefinition
            {
                Name = tableName,
                PascalName = pascalName
            };

            table.Fields = _validatorParser.ParseFields(tableBody);
            table.Indexes = ParseIndexes(indexChain);

            tables.Add(table);
        }

        return tables;
    }

    private static List<(string TableName, string TableBody, string IndexChain)> FindTableDefinitions(string schemaContent)
    {
        var results = new List<(string, string, string)>();

        var pattern = @"(\w+)\s*:\s*defineTable\s*\(";
        var matches = Regex.Matches(schemaContent, pattern);

        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            var startIndex = match.Index + match.Length;

            var tableBody = ExtractBalancedContent(schemaContent, startIndex - 1, '(', ')');
            if (string.IsNullOrEmpty(tableBody))
            {
                continue;
            }

            var objectContent = ExtractObjectContent(tableBody);

            var afterDefineTable = startIndex + tableBody.Length - 1;
            var indexChain = "";
            if (afterDefineTable < schemaContent.Length)
            {
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

    private static List<IndexDefinition> ParseIndexes(string indexChain)
    {
        var indexes = new List<IndexDefinition>();

        var matches = IndexPattern.Matches(indexChain);
        foreach (Match match in matches)
        {
            var indexName = match.Groups[1].Value;
            var fieldsStr = match.Groups[2].Value;

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
}
