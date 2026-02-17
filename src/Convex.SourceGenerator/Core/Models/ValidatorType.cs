#nullable enable

using System.Collections.Generic;

namespace Convex.SourceGenerator.Core.Models;

/// <summary>
/// Defines the validator kind values.
/// </summary>
public enum ValidatorKind
{
    /// <summary>
    /// Specifies the string option.
    /// </summary>
    String,
    /// <summary>
    /// Specifies the number option.
    /// </summary>
    Number,
    /// <summary>
    /// Specifies the float 64 option.
    /// </summary>
    Float64,
    /// <summary>
    /// Specifies the int 64 option.
    /// </summary>
    Int64,
    /// <summary>
    /// Specifies the boolean option.
    /// </summary>
    Boolean,
    /// <summary>
    /// Specifies the bytes option.
    /// </summary>
    Bytes,
    /// <summary>
    /// Specifies the null option.
    /// </summary>
    Null,
    /// <summary>
    /// Specifies the any option.
    /// </summary>
    Any,
    /// <summary>
    /// Specifies the ID option.
    /// </summary>
    Id,
    /// <summary>
    /// Specifies the literal option.
    /// </summary>
    Literal,
    /// <summary>
    /// Specifies the array option.
    /// </summary>
    Array,
    /// <summary>
    /// Specifies the object option.
    /// </summary>
    Object,
    /// <summary>
    /// Specifies the optional option.
    /// </summary>
    Optional,
    /// <summary>
    /// Specifies the union option.
    /// </summary>
    Union,
    /// <summary>
    /// Represents record.
    /// </summary>
    Record
}

/// <summary>
/// Represents validator type.
/// </summary>
public class ValidatorType
{
    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
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
    /// Gets the simple.
    /// </summary>
    public static ValidatorType Simple(ValidatorKind kind) => new() { Kind = kind };

    /// <summary>
    /// Gets the unique ID.
    /// </summary>
    public static ValidatorType Id(string tableName) => new() { Kind = ValidatorKind.Id, TableName = tableName };

    /// <summary>
    /// Gets the literal.
    /// </summary>
    public static ValidatorType Literal(string value) => new() { Kind = ValidatorKind.Literal, LiteralValue = value };

    /// <summary>
    /// Gets the array.
    /// </summary>
    public static ValidatorType Array(ValidatorType elementType) => new() { Kind = ValidatorKind.Array, ElementType = elementType };

    /// <summary>
    /// Gets the object.
    /// </summary>
    public static ValidatorType Object(List<FieldDefinition> fields) => new() { Kind = ValidatorKind.Object, Fields = fields };

    /// <summary>
    /// Gets the optional.
    /// </summary>
    public static ValidatorType Optional(ValidatorType innerType) => new() { Kind = ValidatorKind.Optional, InnerType = innerType };

    /// <summary>
    /// Gets the union.
    /// </summary>
    public static ValidatorType Union(List<ValidatorType> members) => new() { Kind = ValidatorKind.Union, UnionMembers = members };

    /// <summary>
    /// Gets the record.
    /// </summary>
    public static ValidatorType Record(ValidatorType keyType, ValidatorType valueType) =>
        new() { Kind = ValidatorKind.Record, KeyType = keyType, ValueType = valueType };
}
