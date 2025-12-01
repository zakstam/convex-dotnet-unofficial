#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Core.TypeMapping;

/// <summary>
/// Maps Convex validator types to C# types.
/// </summary>
public static class ConvexTypeMapper
{
    /// <summary>
    /// Maps a ValidatorType to its C# type string.
    /// </summary>
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
            ValidatorKind.Id => "string",
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

        var nestedTypeName = GenerateNestedTypeName(parentName, fieldName);
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

        if (!innerType.EndsWith("?"))
        {
            return $"{innerType}?";
        }

        return innerType;
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
                return $"{innerType}?";
            }
        }

        // Check if all members are string literals
        if (validator.UnionMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return "string";
        }

        // Check if all non-null members are string literals
        var nonNullMembers = validator.UnionMembers.Where(m => m.Kind != ValidatorKind.Null).ToList();
        var hasNullMember = validator.UnionMembers.Any(m => m.Kind == ValidatorKind.Null);
        if (nonNullMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return hasNullMember ? "string?" : "string";
        }

        return "object";
    }

    private static bool IsStringLiteral(string? value)
    {
        if (value == null)
        {
            return false;
        }

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

        var pascalFieldName = NamingConventions.ToPascalCase(fieldName!);
        return $"{parentName}{pascalFieldName}";
    }
}
