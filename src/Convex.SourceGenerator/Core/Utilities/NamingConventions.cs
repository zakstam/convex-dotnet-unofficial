#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Convex.SourceGenerator.Core.Utilities;

/// <summary>
/// Provides naming convention utilities for code generation.
/// </summary>
public static class NamingConventions
{
    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle snake_case and camelCase
        var words = Regex.Split(input, @"[_\s]+");
        var result = new StringBuilder();

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

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal))
        {
            return pascal;
        }

        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    /// <summary>
    /// Singularizes a word (converts plural to singular).
    /// </summary>
    public static string Singularize(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 3)
        {
            return word;
        }

        // Common irregular plurals
        var irregulars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "People", "Person" },
            { "Men", "Man" },
            { "Women", "Woman" },
            { "Children", "Child" },
            { "Mice", "Mouse" },
            { "Geese", "Goose" },
            { "Teeth", "Tooth" },
            { "Feet", "Foot" },
            { "Data", "Data" },
            { "Media", "Media" },
            { "Criteria", "Criterion" },
        };

        if (irregulars.TryGetValue(word, out var irregular))
        {
            return irregular;
        }

        // Words ending in -ies -> -y
        if (word.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && word.Length > 4)
        {
            return word.Substring(0, word.Length - 3) + "y";
        }

        // Words ending in -es after s, x, z, ch, sh -> remove -es
        if (word.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("zes", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("ches", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("shes", StringComparison.OrdinalIgnoreCase))
        {
            return word.Substring(0, word.Length - 2);
        }

        // Known -ves words
        var knownVesWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Wolves", "Lives", "Knives", "Wives", "Leaves", "Halves", "Calves", "Selves",
            "Shelves", "Thieves", "Loaves", "Scarves", "Hooves", "Elves"
        };
        if (word.EndsWith("ves", StringComparison.OrdinalIgnoreCase) && knownVesWords.Contains(word))
        {
            var stem = word.Substring(0, word.Length - 3);
            if (word.EndsWith("ives", StringComparison.OrdinalIgnoreCase))
            {
                return stem + "ife";
            }
            return stem + "f";
        }

        // Words ending in -s (but not -ss, -us, -is) -> remove -s
        if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
            !word.EndsWith("us", StringComparison.OrdinalIgnoreCase) &&
            !word.EndsWith("is", StringComparison.OrdinalIgnoreCase))
        {
            return word.Substring(0, word.Length - 1);
        }

        return word;
    }

    /// <summary>
    /// Pluralizes a word (converts singular to plural).
    /// </summary>
    public static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (word.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }

        return word + "s";
    }

    /// <summary>
    /// Converts a module path to a class name.
    /// </summary>
    public static string ToModuleClassName(string modulePath)
    {
        var parts = modulePath.Split('/');
        var lastPart = parts[parts.Length - 1];
        return ToPascalCase(lastPart);
    }

    /// <summary>
    /// Ensures a string is a valid C# identifier.
    /// </summary>
    public static string ToValidIdentifier(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "_";
        }

        var result = new StringBuilder();

        // First character must be letter or underscore
        var first = input[0];
        if (char.IsLetter(first) || first == '_')
        {
            result.Append(first);
        }
        else
        {
            result.Append('_');
        }

        // Subsequent characters can be letters, digits, or underscores
        for (var i = 1; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                result.Append(c);
            }
            else
            {
                result.Append('_');
            }
        }

        return result.ToString();
    }
}
