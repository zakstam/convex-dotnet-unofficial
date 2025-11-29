#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Convex.SchemaGenerator.Models;

namespace Convex.SchemaGenerator.CodeGen;

/// <summary>
/// Maps Convex validator types to C# types.
/// </summary>
public static class TypeMapper
{
    /// <summary>
    /// Maps a ValidatorType to its C# type string.
    /// </summary>
    /// <param name="validator">The validator type to map.</param>
    /// <param name="nestedTypeCallback">Callback to register nested types that need to be generated.</param>
    /// <param name="parentName">The name of the parent type (for generating nested type names).</param>
    /// <param name="fieldName">The name of the field (for generating nested type names).</param>
    /// <returns>The C# type string.</returns>
    public static string MapToCSharpType(
        ValidatorType validator,
        Action<string, List<FieldDefinition>>? nestedTypeCallback = null,
        string? parentName = null,
        string? fieldName = null)
    {
        return validator.Kind switch
        {
            ValidatorKind.String => "string",
            ValidatorKind.Number => "double",
            ValidatorKind.Float64 => "double",
            ValidatorKind.Int64 => "long",
            ValidatorKind.Boolean => "bool",
            ValidatorKind.Bytes => "byte[]",
            ValidatorKind.Null => "object",
            ValidatorKind.Any => "System.Text.Json.JsonElement",
            ValidatorKind.Id => "string", // IDs are strings
            ValidatorKind.Literal => MapLiteralType(validator.LiteralValue),
            ValidatorKind.Array => MapArrayType(validator, nestedTypeCallback, parentName, fieldName),
            ValidatorKind.Object => MapObjectType(validator, nestedTypeCallback, parentName, fieldName),
            ValidatorKind.Optional => MapOptionalType(validator, nestedTypeCallback, parentName, fieldName),
            ValidatorKind.Union => MapUnionType(validator),
            ValidatorKind.Record => MapRecordType(validator, nestedTypeCallback, parentName, fieldName),
            _ => "object"
        };
    }

    /// <summary>
    /// Determines if a C# type is a value type (needs ? for nullable).
    /// </summary>
    public static bool IsValueType(string csharpType)
    {
        return csharpType switch
        {
            "double" => true,
            "long" => true,
            "bool" => true,
            "int" => true,
            "float" => true,
            "decimal" => true,
            _ => false
        };
    }

    private static string MapLiteralType(string? value)
    {
        if (value == null)
        {
            return "object";
        }

        // Try to determine the literal type
        if (bool.TryParse(value, out _))
        {
            return "bool";
        }

        if (long.TryParse(value, out _))
        {
            return "long";
        }

        if (double.TryParse(value, out _))
        {
            return "double";
        }

        // Default to string for string literals
        return "string";
    }

    private static string MapArrayType(
        ValidatorType validator,
        Action<string, List<FieldDefinition>>? nestedTypeCallback,
        string? parentName,
        string? fieldName)
    {
        if (validator.ElementType == null)
        {
            return "System.Collections.Generic.List<object>";
        }

        var elementType = MapToCSharpType(validator.ElementType, nestedTypeCallback, parentName, fieldName);
        return $"System.Collections.Generic.List<{elementType}>";
    }

    private static string MapObjectType(
        ValidatorType validator,
        Action<string, List<FieldDefinition>>? nestedTypeCallback,
        string? parentName,
        string? fieldName)
    {
        if (validator.Fields == null || validator.Fields.Count == 0)
        {
            return "object";
        }

        // Generate a nested type name
        var nestedTypeName = GenerateNestedTypeName(parentName, fieldName);

        // Register the nested type for generation
        nestedTypeCallback?.Invoke(nestedTypeName, validator.Fields);

        return nestedTypeName;
    }

    private static string MapOptionalType(
        ValidatorType validator,
        Action<string, List<FieldDefinition>>? nestedTypeCallback,
        string? parentName,
        string? fieldName)
    {
        if (validator.InnerType == null)
        {
            return "object?";
        }

        var innerType = MapToCSharpType(validator.InnerType, nestedTypeCallback, parentName, fieldName);

        // Add ? for value types, reference types are already nullable with #nullable enable
        if (IsValueType(innerType))
        {
            return $"{innerType}?";
        }

        return $"{innerType}?";
    }

    private static string MapUnionType(ValidatorType validator)
    {
        if (validator.UnionMembers == null || validator.UnionMembers.Count == 0)
        {
            return "object";
        }

        // Check if it's a simple nullable union (T | null)
        if (validator.UnionMembers.Count == 2)
        {
            var nonNullMember = validator.UnionMembers.FirstOrDefault(m => m.Kind != ValidatorKind.Null);
            var hasNull = validator.UnionMembers.Any(m => m.Kind == ValidatorKind.Null);

            if (hasNull && nonNullMember != null)
            {
                var innerType = MapToCSharpType(nonNullMember);
                return IsValueType(innerType) ? $"{innerType}?" : $"{innerType}?";
            }
        }

        // Check if all members are string literals - use string type
        if (validator.UnionMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return "string";
        }

        // Check if all non-null members are string literals (nullable string union)
        var nonNullMembers = validator.UnionMembers.Where(m => m.Kind != ValidatorKind.Null).ToList();
        var hasNullMember = validator.UnionMembers.Any(m => m.Kind == ValidatorKind.Null);
        if (nonNullMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return hasNullMember ? "string?" : "string";
        }

        // For complex unions, we use object
        // A more sophisticated implementation could use discriminated unions
        return "object";
    }

    private static bool IsStringLiteral(string? value)
    {
        if (value == null)
        {
            return false;
        }

        // If it's not a number or boolean, treat it as a string literal
        if (bool.TryParse(value, out _))
        {
            return false;
        }

        if (double.TryParse(value, out _))
        {
            return false;
        }

        return true;
    }

    private static string MapRecordType(
        ValidatorType validator,
        Action<string, List<FieldDefinition>>? nestedTypeCallback,
        string? parentName,
        string? fieldName)
    {
        if (validator.KeyType == null || validator.ValueType == null)
        {
            return "System.Collections.Generic.Dictionary<string, object>";
        }

        var keyType = MapToCSharpType(validator.KeyType);
        var valueType = MapToCSharpType(validator.ValueType, nestedTypeCallback, parentName, fieldName);

        return $"System.Collections.Generic.Dictionary<{keyType}, {valueType}>";
    }

    private static string GenerateNestedTypeName(string? parentName, string? fieldName)
    {
        if (string.IsNullOrEmpty(parentName) || string.IsNullOrEmpty(fieldName))
        {
            return "NestedType";
        }

        // Convert field name to PascalCase and combine with parent name
        // fieldName is guaranteed non-null here due to the check above
        var pascalFieldName = ToPascalCase(fieldName!);
        return $"{parentName}{pascalFieldName}";
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle snake_case and ensure first letter is uppercase
        var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
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
