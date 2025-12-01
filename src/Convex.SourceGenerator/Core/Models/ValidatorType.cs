#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Represents the kind of Convex validator.
/// </summary>
public enum ValidatorKind
{
    String,
    Number,
    Float64,
    Int64,
    Boolean,
    Bytes,
    Null,
    Any,
    Id,
    Literal,
    Array,
    Object,
    Optional,
    Union,
    Record
}

/// <summary>
/// Represents a Convex validator type parsed from TypeScript files.
/// </summary>
public class ValidatorType
{
    public ValidatorKind Kind { get; set; }

    /// <summary>
    /// For Id validators: the referenced table name.
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// For Literal validators: the literal value.
    /// </summary>
    public string? LiteralValue { get; set; }

    /// <summary>
    /// For Array validators: the element type.
    /// </summary>
    public ValidatorType? ElementType { get; set; }

    /// <summary>
    /// For Object validators: the field definitions.
    /// </summary>
    public List<FieldDefinition>? Fields { get; set; }

    /// <summary>
    /// For Optional validators: the inner type.
    /// </summary>
    public ValidatorType? InnerType { get; set; }

    /// <summary>
    /// For Union validators: the member types.
    /// </summary>
    public List<ValidatorType>? UnionMembers { get; set; }

    /// <summary>
    /// For Record validators: the key type.
    /// </summary>
    public ValidatorType? KeyType { get; set; }

    /// <summary>
    /// For Record validators: the value type.
    /// </summary>
    public ValidatorType? ValueType { get; set; }

    /// <summary>
    /// Creates a simple validator type.
    /// </summary>
    public static ValidatorType Simple(ValidatorKind kind) => new() { Kind = kind };

    /// <summary>
    /// Creates an Id validator type.
    /// </summary>
    public static ValidatorType Id(string tableName) => new() { Kind = ValidatorKind.Id, TableName = tableName };

    /// <summary>
    /// Creates a Literal validator type.
    /// </summary>
    public static ValidatorType Literal(string value) => new() { Kind = ValidatorKind.Literal, LiteralValue = value };

    /// <summary>
    /// Creates an Array validator type.
    /// </summary>
    public static ValidatorType Array(ValidatorType elementType) => new() { Kind = ValidatorKind.Array, ElementType = elementType };

    /// <summary>
    /// Creates an Object validator type.
    /// </summary>
    public static ValidatorType Object(List<FieldDefinition> fields) => new() { Kind = ValidatorKind.Object, Fields = fields };

    /// <summary>
    /// Creates an Optional validator type.
    /// </summary>
    public static ValidatorType Optional(ValidatorType innerType) => new() { Kind = ValidatorKind.Optional, InnerType = innerType };

    /// <summary>
    /// Creates a Union validator type.
    /// </summary>
    public static ValidatorType Union(List<ValidatorType> members) => new() { Kind = ValidatorKind.Union, UnionMembers = members };

    /// <summary>
    /// Creates a Record validator type.
    /// </summary>
    public static ValidatorType Record(ValidatorType keyType, ValidatorType valueType) =>
        new() { Kind = ValidatorKind.Record, KeyType = keyType, ValueType = valueType };
}
