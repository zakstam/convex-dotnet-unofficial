#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Core.TypeMapping;

/// <summary>
/// Context for type mapping operations, containing callbacks for nested types, enums, and typed IDs.
/// </summary>
public class TypeMappingContext
{
    /// <summary>
    /// Callback for registering nested object types.
    /// </summary>
    public Action<string, List<FieldDefinition>>? NestedTypeCallback { get; set; }

    /// <summary>
    /// Callback for registering enum types from string literal unions.
    /// </summary>
    public Action<EnumDefinition>? EnumCallback { get; set; }

    /// <summary>
    /// Callback for registering typed ID types. Takes the table name and returns the ID type name.
    /// </summary>
    public Func<string, string>? IdTypeCallback { get; set; }

    /// <summary>
    /// The parent type name for generating nested type names.
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// The field name for generating nested type names.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Creates a new context with updated parent/field names.
    /// </summary>
    public TypeMappingContext WithField(string? parentName, string? fieldName)
    {
        return new TypeMappingContext
        {
            NestedTypeCallback = NestedTypeCallback,
            EnumCallback = EnumCallback,
            IdTypeCallback = IdTypeCallback,
            ParentName = parentName,
            FieldName = fieldName
        };
    }
}

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
        var context = new TypeMappingContext
        {
            NestedTypeCallback = nestedTypeCallback,
            ParentName = parentName,
            FieldName = fieldName
        };
        return MapToCSharpTypeWithContext(validator, context);
    }

    /// <summary>
    /// Maps a ValidatorType to its C# type string with full context support.
    /// </summary>
    public static string MapToCSharpTypeWithContext(ValidatorType validator, TypeMappingContext context)
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
            ValidatorKind.Id => MapIdType(validator, context),
            ValidatorKind.Literal => MapLiteralType(validator.LiteralValue),
            ValidatorKind.Array => MapArrayType(validator, context),
            ValidatorKind.Object => MapObjectType(validator, context),
            ValidatorKind.Optional => MapOptionalType(validator, context),
            ValidatorKind.Union => MapUnionType(validator, context),
            ValidatorKind.Record => MapRecordType(validator, context),
            _ => "object"
        };
    }

    private static string MapIdType(ValidatorType validator, TypeMappingContext context)
    {
        // If we have an ID type callback and a valid table name, use typed ID
        if (context.IdTypeCallback != null && !string.IsNullOrEmpty(validator.TableName))
        {
            return context.IdTypeCallback(validator.TableName!);
        }

        // Fall back to string
        return "string";
    }

    /// <summary>
    /// Determines if a field name represents a timestamp based on naming conventions.
    /// Matches: timestamp, *At (createdAt, updatedAt), *Time (startTime, endTime), *Date (createdDate).
    /// </summary>
    public static bool IsTimestampField(string? fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return false;
        }

        // Exact match for "timestamp"
        if (fieldName!.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Ends with "At" (createdAt, updatedAt, editedAt, finishedAt, deletedAt, etc.)
        if (fieldName.EndsWith("At", StringComparison.Ordinal) && fieldName.Length > 2)
        {
            return true;
        }

        // Ends with "Time" (creationTime, startTime, endTime, etc.)
        if (fieldName.EndsWith("Time", StringComparison.Ordinal) && fieldName.Length > 4)
        {
            return true;
        }

        // Ends with "Date" (createdDate, updatedDate, etc.)
        if (fieldName.EndsWith("Date", StringComparison.Ordinal) && fieldName.Length > 4)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a C# type is a value type (needs ? for nullable).
    /// </summary>
    public static bool IsValueType(string csharpType)
    {
        var normalizedType = csharpType.Replace("global::", string.Empty);
        if (normalizedType.StartsWith("System.", StringComparison.Ordinal))
        {
            normalizedType = normalizedType.Substring("System.".Length);
        }

        return normalizedType switch
        {
            "double" => true,
            "long" => true,
            "bool" => true,
            "int" => true,
            "float" => true,
            "decimal" => true,
            "DateTimeOffset" => true,
            "DateTime" => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines if a C# type is an enum value type (needs ? for nullable).
    /// Pass a set of known enum names generated for this context.
    /// </summary>
    public static bool IsValueType(string csharpType, HashSet<string> enumNames)
    {
        // Remove nullable suffix if present
        var baseType = csharpType.EndsWith("?") ? csharpType.Substring(0, csharpType.Length - 1) : csharpType;

        if (IsValueType(baseType))
        {
            return true;
        }

        return enumNames.Contains(baseType);
    }

    /// <summary>
    /// Determines if a validator type represents a string literal union that could become an enum.
    /// </summary>
    public static bool IsEnumUnion(ValidatorType validator)
    {
        if (validator.Kind != ValidatorKind.Union || validator.UnionMembers == null || validator.UnionMembers.Count == 0)
        {
            return false;
        }

        // Check if all members are string literals
        if (validator.UnionMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return true;
        }

        // Check if all non-null members are string literals
        var nonNullMembers = validator.UnionMembers.Where(m => m.Kind != ValidatorKind.Null).ToList();
        return nonNullMembers.Count > 0 &&
               nonNullMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue));
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

    private static string MapArrayType(ValidatorType validator, TypeMappingContext context)
    {
        if (validator.ElementType == null)
        {
            return "System.Collections.Generic.List<object>";
        }

        var elementType = MapToCSharpTypeWithContext(validator.ElementType, context);
        return $"System.Collections.Generic.List<{elementType}>";
    }

    private static string MapObjectType(ValidatorType validator, TypeMappingContext context)
    {
        if (validator.Fields == null || validator.Fields.Count == 0)
        {
            return "object";
        }

        var nestedTypeName = GenerateNestedTypeName(context.ParentName, context.FieldName);
        context.NestedTypeCallback?.Invoke(nestedTypeName, validator.Fields);

        return nestedTypeName;
    }

    private static string MapOptionalType(ValidatorType validator, TypeMappingContext context)
    {
        if (validator.InnerType == null)
        {
            return "object?";
        }

        var innerType = MapToCSharpTypeWithContext(validator.InnerType, context);

        if (!innerType.EndsWith("?"))
        {
            return $"{innerType}?";
        }

        return innerType;
    }

    private static string MapUnionType(ValidatorType validator, TypeMappingContext context)
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
                var innerType = MapToCSharpTypeWithContext(nonNullMember, context);
                return $"{innerType}?";
            }
        }

        // Check if all members are string literals - generate an enum
        if (validator.UnionMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return GenerateEnumForUnion(validator.UnionMembers, context, isNullable: false);
        }

        // Check if all non-null members are string literals - generate a nullable enum
        var nonNullMembers = validator.UnionMembers.Where(m => m.Kind != ValidatorKind.Null).ToList();
        var hasNullMember = validator.UnionMembers.Any(m => m.Kind == ValidatorKind.Null);
        if (nonNullMembers.All(m => m.Kind == ValidatorKind.Literal && IsStringLiteral(m.LiteralValue)))
        {
            return GenerateEnumForUnion(nonNullMembers, context, isNullable: hasNullMember);
        }

        return "object";
    }

    private static string GenerateEnumForUnion(List<ValidatorType> members, TypeMappingContext context, bool isNullable)
    {
        // If no enum callback is provided, fall back to string
        if (context.EnumCallback == null)
        {
            return isNullable ? "string?" : "string";
        }

        // Generate enum name from parent and field name
        var enumName = GenerateEnumName(context.ParentName, context.FieldName);

        // Create enum members from literal values
        var enumMembers = new List<EnumMember>();
        foreach (var member in members)
        {
            if (member.LiteralValue != null)
            {
                enumMembers.Add(new EnumMember
                {
                    Name = NamingConventions.ToPascalCase(member.LiteralValue),
                    Value = member.LiteralValue
                });
            }
        }

        // Register the enum
        var enumDef = new EnumDefinition
        {
            Name = enumName,
            Members = enumMembers,
            Description = $"Enum generated from string literal union for {context.FieldName ?? "unknown field"}"
        };
        context.EnumCallback(enumDef);

        return isNullable ? $"{enumName}?" : enumName;
    }

    private static string GenerateEnumName(string? parentName, string? fieldName)
    {
        if (string.IsNullOrEmpty(parentName) && string.IsNullOrEmpty(fieldName))
        {
            return "GeneratedEnum";
        }

        if (string.IsNullOrEmpty(fieldName))
        {
            return $"{parentName}Enum";
        }

        var pascalFieldName = NamingConventions.ToPascalCase(fieldName!);

        if (string.IsNullOrEmpty(parentName))
        {
            return pascalFieldName;
        }

        return $"{parentName}{pascalFieldName}";
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

    private static string MapRecordType(ValidatorType validator, TypeMappingContext context)
    {
        if (validator.KeyType == null || validator.ValueType == null)
        {
            return "System.Collections.Generic.Dictionary<string, object>";
        }

        var keyType = MapToCSharpTypeWithContext(validator.KeyType, context);
        var valueType = MapToCSharpTypeWithContext(validator.ValueType, context);

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
