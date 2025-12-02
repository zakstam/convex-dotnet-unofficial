using System;
using System.Collections.Generic;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.TypeMapping;
using Xunit;

namespace Convex.SourceGenerator.Tests;

public class ConvexTypeMapperTests
{
    [Theory]
    [InlineData(ValidatorKind.String, "string")]
    [InlineData(ValidatorKind.Number, "double")]
    [InlineData(ValidatorKind.Float64, "double")]
    [InlineData(ValidatorKind.Int64, "long")]
    [InlineData(ValidatorKind.Boolean, "bool")]
    [InlineData(ValidatorKind.Bytes, "byte[]")]
    [InlineData(ValidatorKind.Null, "object")]
    [InlineData(ValidatorKind.Any, "System.Text.Json.JsonElement")]
    [InlineData(ValidatorKind.Id, "string")]
    public void MapToCSharpType_SimpleTypes_ReturnsCorrectType(ValidatorKind kind, string expectedType)
    {
        var validator = ValidatorType.Simple(kind);
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal(expectedType, result);
    }

    [Theory]
    [InlineData("active", "string")]
    [InlineData("123", "long")]
    [InlineData("3.14", "double")]
    [InlineData("true", "bool")]
    [InlineData("false", "bool")]
    public void MapToCSharpType_Literals_ReturnsCorrectType(string literalValue, string expectedType)
    {
        var validator = ValidatorType.Literal(literalValue);
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void MapToCSharpType_ArrayOfStrings_ReturnsListOfStrings()
    {
        var validator = ValidatorType.Array(ValidatorType.Simple(ValidatorKind.String));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<string>", result);
    }

    [Fact]
    public void MapToCSharpType_ArrayOfNumbers_ReturnsListOfDoubles()
    {
        var validator = ValidatorType.Array(ValidatorType.Simple(ValidatorKind.Number));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<double>", result);
    }

    [Fact]
    public void MapToCSharpType_ArrayOfIds_ReturnsListOfStrings()
    {
        var validator = ValidatorType.Array(ValidatorType.Id("users"));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<string>", result);
    }

    [Fact]
    public void MapToCSharpType_NestedArray_ReturnsNestedList()
    {
        var validator = ValidatorType.Array(ValidatorType.Array(ValidatorType.Simple(ValidatorKind.Number)));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<System.Collections.Generic.List<double>>", result);
    }

    [Fact]
    public void MapToCSharpType_OptionalString_ReturnsNullableString()
    {
        var validator = ValidatorType.Optional(ValidatorType.Simple(ValidatorKind.String));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("string?", result);
    }

    [Fact]
    public void MapToCSharpType_OptionalNumber_ReturnsNullableDouble()
    {
        var validator = ValidatorType.Optional(ValidatorType.Simple(ValidatorKind.Number));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("double?", result);
    }

    [Fact]
    public void MapToCSharpType_OptionalBoolean_ReturnsNullableBool()
    {
        var validator = ValidatorType.Optional(ValidatorType.Simple(ValidatorKind.Boolean));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("bool?", result);
    }

    [Fact]
    public void MapToCSharpType_OptionalArray_ReturnsNullableList()
    {
        var validator = ValidatorType.Optional(ValidatorType.Array(ValidatorType.Simple(ValidatorKind.String)));
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<string>?", result);
    }

    [Fact]
    public void MapToCSharpType_NullableUnion_ReturnsNullableType()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Simple(ValidatorKind.String),
            ValidatorType.Simple(ValidatorKind.Null)
        });
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("string?", result);
    }

    [Fact]
    public void MapToCSharpType_StringLiteralUnion_ReturnsString()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("waiting"),
            ValidatorType.Literal("playing"),
            ValidatorType.Literal("finished")
        });
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToCSharpType_NullableStringLiteralUnion_ReturnsNullableString()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("active"),
            ValidatorType.Literal("inactive"),
            ValidatorType.Simple(ValidatorKind.Null)
        });
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("string?", result);
    }

    [Fact]
    public void MapToCSharpType_ComplexUnion_ReturnsObject()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Simple(ValidatorKind.String),
            ValidatorType.Simple(ValidatorKind.Number)
        });
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("object", result);
    }

    [Fact]
    public void MapToCSharpType_RecordStringToNumber_ReturnsDictionary()
    {
        var validator = ValidatorType.Record(
            ValidatorType.Simple(ValidatorKind.String),
            ValidatorType.Simple(ValidatorKind.Number)
        );
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.Dictionary<string, double>", result);
    }

    [Fact]
    public void MapToCSharpType_RecordStringToBoolean_ReturnsDictionary()
    {
        var validator = ValidatorType.Record(
            ValidatorType.Simple(ValidatorKind.String),
            ValidatorType.Simple(ValidatorKind.Boolean)
        );
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.Dictionary<string, bool>", result);
    }

    [Fact]
    public void MapToCSharpType_Object_CallsNestedTypeCallback()
    {
        var fields = new List<FieldDefinition>
        {
            new() { Name = "name", Type = ValidatorType.Simple(ValidatorKind.String) },
            new() { Name = "age", Type = ValidatorType.Simple(ValidatorKind.Number) }
        };
        var validator = ValidatorType.Object(fields);

        string? capturedTypeName = null;
        List<FieldDefinition>? capturedFields = null;

        var result = ConvexTypeMapper.MapToCSharpType(
            validator,
            (typeName, typeFields) =>
            {
                capturedTypeName = typeName;
                capturedFields = typeFields;
            },
            "Parent",
            "child"
        );

        Assert.Equal("ParentChild", result);
        Assert.Equal("ParentChild", capturedTypeName);
        Assert.NotNull(capturedFields);
        Assert.Equal(2, capturedFields!.Count);
    }

    [Fact]
    public void MapToCSharpType_EmptyObject_ReturnsObject()
    {
        var validator = ValidatorType.Object(new List<FieldDefinition>());
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("object", result);
    }

    [Theory]
    [InlineData("double", true)]
    [InlineData("long", true)]
    [InlineData("bool", true)]
    [InlineData("int", true)]
    [InlineData("float", true)]
    [InlineData("decimal", true)]
    [InlineData("string", false)]
    [InlineData("byte[]", false)]
    [InlineData("object", false)]
    [InlineData("List<string>", false)]
    public void IsValueType_ReturnsCorrectResult(string csharpType, bool expectedIsValueType)
    {
        var result = ConvexTypeMapper.IsValueType(csharpType);
        Assert.Equal(expectedIsValueType, result);
    }

    [Fact]
    public void MapToCSharpType_ArrayWithNullElementType_ReturnsListOfObject()
    {
        var validator = new ValidatorType { Kind = ValidatorKind.Array, ElementType = null };
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.List<object>", result);
    }

    [Fact]
    public void MapToCSharpType_OptionalWithNullInnerType_ReturnsNullableObject()
    {
        var validator = new ValidatorType { Kind = ValidatorKind.Optional, InnerType = null };
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("object?", result);
    }

    [Fact]
    public void MapToCSharpType_RecordWithNullTypes_ReturnsDefaultDictionary()
    {
        var validator = new ValidatorType { Kind = ValidatorKind.Record, KeyType = null, ValueType = null };
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("System.Collections.Generic.Dictionary<string, object>", result);
    }

    [Fact]
    public void MapToCSharpType_UnionWithEmptyMembers_ReturnsObject()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>());
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("object", result);
    }

    [Fact]
    public void MapToCSharpType_LiteralWithNullValue_ReturnsObject()
    {
        var validator = new ValidatorType { Kind = ValidatorKind.Literal, LiteralValue = null };
        var result = ConvexTypeMapper.MapToCSharpType(validator);
        Assert.Equal("object", result);
    }

    [Fact]
    public void MapToCSharpType_NestedObjectInArray_GeneratesNestedType()
    {
        var innerFields = new List<FieldDefinition>
        {
            new() { Name = "value", Type = ValidatorType.Simple(ValidatorKind.Number) }
        };
        var validator = ValidatorType.Array(ValidatorType.Object(innerFields));

        string? capturedTypeName = null;

        var result = ConvexTypeMapper.MapToCSharpType(
            validator,
            (typeName, _) => capturedTypeName = typeName,
            "Item",
            "scores"
        );

        Assert.Equal("System.Collections.Generic.List<ItemScores>", result);
        Assert.Equal("ItemScores", capturedTypeName);
    }

    #region Enum Generation Tests

    [Fact]
    public void MapToCSharpTypeWithContext_StringLiteralUnion_WithEnumCallback_GeneratesEnum()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("waiting"),
            ValidatorType.Literal("playing"),
            ValidatorType.Literal("finished")
        });

        EnumDefinition? capturedEnum = null;
        var context = new TypeMappingContext
        {
            EnumCallback = enumDef => capturedEnum = enumDef,
            ParentName = "Game",
            FieldName = "status"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("GameStatus", result);
        Assert.NotNull(capturedEnum);
        Assert.Equal("GameStatus", capturedEnum!.Name);
        Assert.Equal(3, capturedEnum.Members.Count);
        Assert.Contains(capturedEnum.Members, m => m.Name == "Waiting" && m.Value == "waiting");
        Assert.Contains(capturedEnum.Members, m => m.Name == "Playing" && m.Value == "playing");
        Assert.Contains(capturedEnum.Members, m => m.Name == "Finished" && m.Value == "finished");
    }

    [Fact]
    public void MapToCSharpTypeWithContext_NullableStringLiteralUnion_WithEnumCallback_GeneratesNullableEnum()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("active"),
            ValidatorType.Literal("inactive"),
            ValidatorType.Simple(ValidatorKind.Null)
        });

        EnumDefinition? capturedEnum = null;
        var context = new TypeMappingContext
        {
            EnumCallback = enumDef => capturedEnum = enumDef,
            ParentName = "User",
            FieldName = "status"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("UserStatus?", result);
        Assert.NotNull(capturedEnum);
        Assert.Equal("UserStatus", capturedEnum!.Name);
        Assert.Equal(2, capturedEnum.Members.Count);
    }

    [Fact]
    public void MapToCSharpTypeWithContext_StringLiteralUnion_WithoutEnumCallback_ReturnsString()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("waiting"),
            ValidatorType.Literal("playing")
        });

        var context = new TypeMappingContext
        {
            ParentName = "Game",
            FieldName = "status"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("string", result);
    }

    [Fact]
    public void IsEnumUnion_StringLiteralUnion_ReturnsTrue()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("waiting"),
            ValidatorType.Literal("playing"),
            ValidatorType.Literal("finished")
        });

        Assert.True(ConvexTypeMapper.IsEnumUnion(validator));
    }

    [Fact]
    public void IsEnumUnion_NullableStringLiteralUnion_ReturnsTrue()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("active"),
            ValidatorType.Literal("inactive"),
            ValidatorType.Simple(ValidatorKind.Null)
        });

        Assert.True(ConvexTypeMapper.IsEnumUnion(validator));
    }

    [Fact]
    public void IsEnumUnion_ComplexUnion_ReturnsFalse()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Simple(ValidatorKind.String),
            ValidatorType.Simple(ValidatorKind.Number)
        });

        Assert.False(ConvexTypeMapper.IsEnumUnion(validator));
    }

    [Fact]
    public void IsEnumUnion_NonUnion_ReturnsFalse()
    {
        var validator = ValidatorType.Simple(ValidatorKind.String);

        Assert.False(ConvexTypeMapper.IsEnumUnion(validator));
    }

    [Fact]
    public void IsEnumUnion_NumericLiteralUnion_ReturnsFalse()
    {
        var validator = ValidatorType.Union(new List<ValidatorType>
        {
            ValidatorType.Literal("1"),
            ValidatorType.Literal("2"),
            ValidatorType.Literal("3")
        });

        Assert.False(ConvexTypeMapper.IsEnumUnion(validator));
    }

    [Fact]
    public void IsValueType_WithEnumNames_RecognizesEnums()
    {
        var enumNames = new HashSet<string> { "GameStatus", "UserRole" };

        Assert.True(ConvexTypeMapper.IsValueType("GameStatus", enumNames));
        Assert.True(ConvexTypeMapper.IsValueType("UserRole", enumNames));
        Assert.True(ConvexTypeMapper.IsValueType("GameStatus?", enumNames));
        Assert.False(ConvexTypeMapper.IsValueType("string", enumNames));
        Assert.False(ConvexTypeMapper.IsValueType("UnknownEnum", enumNames));
    }

    [Fact]
    public void TypeMappingContext_WithField_CreatesNewContextWithUpdatedNames()
    {
        EnumDefinition? capturedEnum = null;
        var originalContext = new TypeMappingContext
        {
            EnumCallback = enumDef => capturedEnum = enumDef,
            ParentName = "Original",
            FieldName = "field1"
        };

        var newContext = originalContext.WithField("NewParent", "field2");

        Assert.Equal("NewParent", newContext.ParentName);
        Assert.Equal("field2", newContext.FieldName);
        Assert.Same(originalContext.EnumCallback, newContext.EnumCallback);
    }

    #endregion

    #region Typed ID Tests

    [Fact]
    public void MapToCSharpTypeWithContext_IdWithCallback_ReturnsTypedId()
    {
        var validator = ValidatorType.Id("users");

        var context = new TypeMappingContext
        {
            IdTypeCallback = tableName => $"{char.ToUpperInvariant(tableName[0])}{tableName.Substring(1)}Id"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("UsersId", result);
    }

    [Fact]
    public void MapToCSharpTypeWithContext_IdWithoutCallback_ReturnsString()
    {
        var validator = ValidatorType.Id("users");

        var context = new TypeMappingContext();

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToCSharpTypeWithContext_IdWithNullTableName_ReturnsString()
    {
        var validator = new ValidatorType { Kind = ValidatorKind.Id, TableName = null };

        var context = new TypeMappingContext
        {
            IdTypeCallback = tableName => $"{tableName}Id"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("string", result);
    }

    [Fact]
    public void MapToCSharpTypeWithContext_ArrayOfIds_WithCallback_ReturnsListOfTypedIds()
    {
        var validator = ValidatorType.Array(ValidatorType.Id("messages"));

        var context = new TypeMappingContext
        {
            IdTypeCallback = tableName => $"{char.ToUpperInvariant(tableName[0])}{tableName.Substring(1)}Id"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("System.Collections.Generic.List<MessagesId>", result);
    }

    [Fact]
    public void MapToCSharpTypeWithContext_OptionalId_WithCallback_ReturnsNullableTypedId()
    {
        var validator = ValidatorType.Optional(ValidatorType.Id("rooms"));

        var context = new TypeMappingContext
        {
            IdTypeCallback = tableName => $"{char.ToUpperInvariant(tableName[0])}{tableName.Substring(1)}Id"
        };

        var result = ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        Assert.Equal("RoomsId?", result);
    }

    [Fact]
    public void TypeMappingContext_WithField_PreservesIdTypeCallback()
    {
        Func<string, string> idCallback = tableName => $"{tableName}Id";
        var originalContext = new TypeMappingContext
        {
            IdTypeCallback = idCallback,
            ParentName = "Original",
            FieldName = "field1"
        };

        var newContext = originalContext.WithField("NewParent", "field2");

        Assert.Same(idCallback, newContext.IdTypeCallback);
    }

    #endregion
}
