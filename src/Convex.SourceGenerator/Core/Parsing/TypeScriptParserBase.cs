#nullable enable

using System.Text.RegularExpressions;

namespace Convex.SourceGenerator.Core.Parsing;

/// <summary>
/// Base class with shared TypeScript parsing utilities.
/// </summary>
public abstract class TypeScriptParserBase
{
    /// <summary>
    /// Removes single-line and multi-line comments from TypeScript content.
    /// </summary>
    protected static string RemoveComments(string content)
    {
        // Remove single-line comments
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);

        // Remove multi-line comments
        content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");

        return content;
    }

    /// <summary>
    /// Extracts balanced content between open and close characters.
    /// </summary>
    protected static string ExtractBalancedContent(string content, int startIndex, char openChar, char closeChar)
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
                        i++;
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

    /// <summary>
    /// Extracts the content of parentheses starting at the given index.
    /// </summary>
    protected static string ExtractParenContent(string expr, int openParenIndex)
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

    /// <summary>
    /// Extracts the object content (without outer braces) from a string.
    /// </summary>
    protected static string ExtractObjectContent(string content)
    {
        var braceStart = content.IndexOf('{');
        if (braceStart < 0)
        {
            return string.Empty;
        }

        var objectContent = ExtractBalancedContent(content, braceStart, '{', '}');
        if (objectContent.Length >= 2)
        {
            return objectContent.Substring(1, objectContent.Length - 2);
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the full validator expression starting at the given position.
    /// </summary>
    protected static string ExtractFullValidator(string content, int startIndex)
    {
        if (startIndex >= content.Length)
        {
            return string.Empty;
        }

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
}
