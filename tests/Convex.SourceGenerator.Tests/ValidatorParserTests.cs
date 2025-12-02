using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Parsing;
using Xunit;

namespace Convex.SourceGenerator.Tests;

public class ValidatorParserTests
{
    private readonly ValidatorParser _parser = new();

    [Theory]
    [InlineData("v.string()", ValidatorKind.String)]
    [InlineData("v.number()", ValidatorKind.Number)]
    [InlineData("v.float64()", ValidatorKind.Float64)]
    [InlineData("v.int64()", ValidatorKind.Int64)]
    [InlineData("v.boolean()", ValidatorKind.Boolean)]
    [InlineData("v.bytes()", ValidatorKind.Bytes)]
    [InlineData("v.null()", ValidatorKind.Null)]
    [InlineData("v.any()", ValidatorKind.Any)]
    public void Parse_SimpleTypes_ReturnsCorrectKind(string expression, ValidatorKind expectedKind)
    {
        var result = _parser.Parse(expression);
        Assert.Equal(expectedKind, result.Kind);
    }

    [Theory]
    [InlineData("v.id(\"users\")", "users")]
    [InlineData("v.id('messages')", "messages")]
    [InlineData("v.id(\"rooms\")", "rooms")]
    public void Parse_IdType_ReturnsTableName(string expression, string expectedTableName)
    {
        var result = _parser.Parse(expression);
        Assert.Equal(ValidatorKind.Id, result.Kind);
        Assert.Equal(expectedTableName, result.TableName);
    }

    [Theory]
    [InlineData("v.literal(\"active\")", "active")]
    [InlineData("v.literal('pending')", "pending")]
    [InlineData("v.literal(123)", "123")]
    [InlineData("v.literal(true)", "true")]
    public void Parse_LiteralType_ReturnsValue(string expression, string expectedValue)
    {
        var result = _parser.Parse(expression);
        Assert.Equal(ValidatorKind.Literal, result.Kind);
        Assert.Equal(expectedValue, result.LiteralValue);
    }

    [Fact]
    public void Parse_OptionalString_ReturnsOptionalWithStringInner()
    {
        var result = _parser.Parse("v.optional(v.string())");
        Assert.Equal(ValidatorKind.Optional, result.Kind);
        Assert.NotNull(result.InnerType);
        Assert.Equal(ValidatorKind.String, result.InnerType!.Kind);
    }

    [Fact]
    public void Parse_OptionalId_ReturnsOptionalWithIdInner()
    {
        var result = _parser.Parse("v.optional(v.id(\"users\"))");
        Assert.Equal(ValidatorKind.Optional, result.Kind);
        Assert.NotNull(result.InnerType);
        Assert.Equal(ValidatorKind.Id, result.InnerType!.Kind);
        Assert.Equal("users", result.InnerType.TableName);
    }

    [Fact]
    public void Parse_ArrayOfStrings_ReturnsArrayWithStringElement()
    {
        var result = _parser.Parse("v.array(v.string())");
        Assert.Equal(ValidatorKind.Array, result.Kind);
        Assert.NotNull(result.ElementType);
        Assert.Equal(ValidatorKind.String, result.ElementType!.Kind);
    }

    [Fact]
    public void Parse_ArrayOfIds_ReturnsArrayWithIdElement()
    {
        var result = _parser.Parse("v.array(v.id(\"messages\"))");
        Assert.Equal(ValidatorKind.Array, result.Kind);
        Assert.NotNull(result.ElementType);
        Assert.Equal(ValidatorKind.Id, result.ElementType!.Kind);
        Assert.Equal("messages", result.ElementType.TableName);
    }

    [Fact]
    public void Parse_NestedArray_ReturnsCorrectStructure()
    {
        var result = _parser.Parse("v.array(v.array(v.number()))");
        Assert.Equal(ValidatorKind.Array, result.Kind);
        Assert.NotNull(result.ElementType);
        Assert.Equal(ValidatorKind.Array, result.ElementType!.Kind);
        Assert.NotNull(result.ElementType.ElementType);
        Assert.Equal(ValidatorKind.Number, result.ElementType.ElementType!.Kind);
    }

    [Fact]
    public void Parse_SimpleObject_ReturnsObjectWithFields()
    {
        var result = _parser.Parse("v.object({ name: v.string(), age: v.number() })");
        Assert.Equal(ValidatorKind.Object, result.Kind);
        Assert.NotNull(result.Fields);
        Assert.Equal(2, result.Fields!.Count);

        Assert.Equal("name", result.Fields[0].Name);
        Assert.Equal(ValidatorKind.String, result.Fields[0].Type.Kind);

        Assert.Equal("age", result.Fields[1].Name);
        Assert.Equal(ValidatorKind.Number, result.Fields[1].Type.Kind);
    }

    [Fact]
    public void Parse_ObjectWithOptionalField_MarksFieldAsOptional()
    {
        var result = _parser.Parse("v.object({ name: v.string(), nickname: v.optional(v.string()) })");
        Assert.Equal(ValidatorKind.Object, result.Kind);
        Assert.NotNull(result.Fields);
        Assert.Equal(2, result.Fields!.Count);

        Assert.Equal("name", result.Fields[0].Name);
        Assert.False(result.Fields[0].IsOptional);

        Assert.Equal("nickname", result.Fields[1].Name);
        Assert.True(result.Fields[1].IsOptional);
    }

    [Fact]
    public void Parse_NestedObject_ReturnsNestedFields()
    {
        var result = _parser.Parse("v.object({ profile: v.object({ bio: v.string() }) })");
        Assert.Equal(ValidatorKind.Object, result.Kind);
        Assert.NotNull(result.Fields);
        Assert.Single(result.Fields!);

        var profileField = result.Fields[0];
        Assert.Equal("profile", profileField.Name);
        Assert.Equal(ValidatorKind.Object, profileField.Type.Kind);
        Assert.NotNull(profileField.Type.Fields);
        Assert.Single(profileField.Type.Fields!);
        Assert.Equal("bio", profileField.Type.Fields[0].Name);
    }

    [Fact]
    public void Parse_UnionOfLiterals_ReturnsUnionMembers()
    {
        var result = _parser.Parse("v.union(v.literal(\"waiting\"), v.literal(\"playing\"), v.literal(\"finished\"))");
        Assert.Equal(ValidatorKind.Union, result.Kind);
        Assert.NotNull(result.UnionMembers);
        Assert.Equal(3, result.UnionMembers!.Count);

        Assert.All(result.UnionMembers, m => Assert.Equal(ValidatorKind.Literal, m.Kind));
        Assert.Equal("waiting", result.UnionMembers[0].LiteralValue);
        Assert.Equal("playing", result.UnionMembers[1].LiteralValue);
        Assert.Equal("finished", result.UnionMembers[2].LiteralValue);
    }

    [Fact]
    public void Parse_NullableUnion_ReturnsUnionWithNull()
    {
        var result = _parser.Parse("v.union(v.string(), v.null())");
        Assert.Equal(ValidatorKind.Union, result.Kind);
        Assert.NotNull(result.UnionMembers);
        Assert.Equal(2, result.UnionMembers!.Count);
        Assert.Equal(ValidatorKind.String, result.UnionMembers[0].Kind);
        Assert.Equal(ValidatorKind.Null, result.UnionMembers[1].Kind);
    }

    [Fact]
    public void Parse_Record_ReturnsKeyAndValueTypes()
    {
        var result = _parser.Parse("v.record(v.string(), v.number())");
        Assert.Equal(ValidatorKind.Record, result.Kind);
        Assert.NotNull(result.KeyType);
        Assert.NotNull(result.ValueType);
        Assert.Equal(ValidatorKind.String, result.KeyType!.Kind);
        Assert.Equal(ValidatorKind.Number, result.ValueType!.Kind);
    }

    [Fact]
    public void Parse_RecordWithObjectValue_ReturnsComplexValueType()
    {
        var result = _parser.Parse("v.record(v.string(), v.object({ count: v.number() }))");
        Assert.Equal(ValidatorKind.Record, result.Kind);
        Assert.NotNull(result.KeyType);
        Assert.NotNull(result.ValueType);
        Assert.Equal(ValidatorKind.String, result.KeyType!.Kind);
        Assert.Equal(ValidatorKind.Object, result.ValueType!.Kind);
        Assert.NotNull(result.ValueType.Fields);
        Assert.Single(result.ValueType.Fields!);
    }

    [Fact]
    public void Parse_UnknownExpression_ReturnsAny()
    {
        var result = _parser.Parse("v.unknown()");
        Assert.Equal(ValidatorKind.Any, result.Kind);
    }

    [Theory]
    [InlineData("  v.string()  ")]
    [InlineData("\tv.string()\n")]
    [InlineData("\n  v.string()  \n")]
    public void Parse_WithWhitespace_TrimsAndParsesCorrectly(string expression)
    {
        var result = _parser.Parse(expression);
        Assert.Equal(ValidatorKind.String, result.Kind);
    }

    [Fact]
    public void ParseFields_MultipleFields_ParsesAllFields()
    {
        var content = "name: v.string(), age: v.number(), active: v.boolean()";
        var fields = _parser.ParseFields(content);

        Assert.Equal(3, fields.Count);
        Assert.Equal("name", fields[0].Name);
        Assert.Equal("age", fields[1].Name);
        Assert.Equal("active", fields[2].Name);
    }

    [Fact]
    public void ParseFields_WithNestedObject_ParsesCorrectly()
    {
        var content = "user: v.object({ name: v.string() }), count: v.number()";
        var fields = _parser.ParseFields(content);

        Assert.Equal(2, fields.Count);
        Assert.Equal("user", fields[0].Name);
        Assert.Equal(ValidatorKind.Object, fields[0].Type.Kind);
        Assert.Equal("count", fields[1].Name);
        Assert.Equal(ValidatorKind.Number, fields[1].Type.Kind);
    }
}
