using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Parsing;
using Xunit;

namespace Convex.SourceGenerator.Tests;

public class SchemaParserTests
{
    private readonly SchemaParser _parser = new();

    [Fact]
    public void Parse_SimpleSchema_ExtractsTableDefinition()
    {
        var content = @"
import { defineSchema, defineTable } from ""convex/server"";
import { v } from ""convex/values"";

export default defineSchema({
    users: defineTable({
        name: v.string(),
        email: v.string(),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal("users", tables[0].Name);
        Assert.Equal("User", tables[0].PascalName);
        Assert.Equal(2, tables[0].Fields.Count);
        Assert.Equal("name", tables[0].Fields[0].Name);
        Assert.Equal("email", tables[0].Fields[1].Name);
    }

    [Fact]
    public void Parse_MultipleTables_ExtractsAllTables()
    {
        var content = @"
export default defineSchema({
    users: defineTable({
        name: v.string(),
    }),
    messages: defineTable({
        content: v.string(),
        authorId: v.id(""users""),
    }),
    rooms: defineTable({
        name: v.string(),
        createdAt: v.number(),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Equal(3, tables.Count);
        Assert.Contains(tables, t => t.Name == "users");
        Assert.Contains(tables, t => t.Name == "messages");
        Assert.Contains(tables, t => t.Name == "rooms");
    }

    [Fact]
    public void Parse_TableWithIndex_ExtractsIndexDefinition()
    {
        var content = @"
export default defineSchema({
    messages: defineTable({
        roomId: v.id(""rooms""),
        content: v.string(),
    }).index(""by_room"", [""roomId""]),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Single(tables[0].Indexes);
        Assert.Equal("by_room", tables[0].Indexes[0].Name);
        Assert.Single(tables[0].Indexes[0].Fields);
        Assert.Equal("roomId", tables[0].Indexes[0].Fields[0]);
    }

    [Fact]
    public void Parse_TableWithMultipleIndexes_ExtractsAllIndexes()
    {
        var content = @"
export default defineSchema({
    messages: defineTable({
        roomId: v.id(""rooms""),
        authorId: v.id(""users""),
        content: v.string(),
    })
    .index(""by_room"", [""roomId""])
    .index(""by_author"", [""authorId""]),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal(2, tables[0].Indexes.Count);
        Assert.Equal("by_room", tables[0].Indexes[0].Name);
        Assert.Equal("by_author", tables[0].Indexes[1].Name);
    }

    [Fact]
    public void Parse_TableWithCompositeIndex_ExtractsAllFields()
    {
        var content = @"
export default defineSchema({
    messages: defineTable({
        roomId: v.id(""rooms""),
        createdAt: v.number(),
        content: v.string(),
    }).index(""by_room_time"", [""roomId"", ""createdAt""]),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Single(tables[0].Indexes);
        Assert.Equal(2, tables[0].Indexes[0].Fields.Count);
        Assert.Equal("roomId", tables[0].Indexes[0].Fields[0]);
        Assert.Equal("createdAt", tables[0].Indexes[0].Fields[1]);
    }

    [Fact]
    public void Parse_TableWithOptionalFields_MarksAsOptional()
    {
        var content = @"
export default defineSchema({
    users: defineTable({
        name: v.string(),
        bio: v.optional(v.string()),
        age: v.optional(v.number()),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal(3, tables[0].Fields.Count);

        var nameField = tables[0].Fields.Find(f => f.Name == "name");
        var bioField = tables[0].Fields.Find(f => f.Name == "bio");
        var ageField = tables[0].Fields.Find(f => f.Name == "age");

        Assert.NotNull(nameField);
        Assert.NotNull(bioField);
        Assert.NotNull(ageField);

        Assert.False(nameField!.IsOptional);
        Assert.True(bioField!.IsOptional);
        Assert.True(ageField!.IsOptional);
    }

    [Fact]
    public void Parse_TableWithComplexTypes_ParsesCorrectly()
    {
        var content = @"
export default defineSchema({
    games: defineTable({
        status: v.union(v.literal(""waiting""), v.literal(""playing""), v.literal(""finished"")),
        players: v.array(v.id(""users"")),
        settings: v.object({
            maxPlayers: v.number(),
            timeLimit: v.optional(v.number()),
        }),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal("games", tables[0].Name);
        Assert.Equal("Game", tables[0].PascalName);
        Assert.Equal(3, tables[0].Fields.Count);

        var statusField = tables[0].Fields.Find(f => f.Name == "status");
        Assert.NotNull(statusField);
        Assert.Equal(ValidatorKind.Union, statusField!.Type.Kind);
        Assert.Equal(3, statusField.Type.UnionMembers!.Count);

        var playersField = tables[0].Fields.Find(f => f.Name == "players");
        Assert.NotNull(playersField);
        Assert.Equal(ValidatorKind.Array, playersField!.Type.Kind);

        var settingsField = tables[0].Fields.Find(f => f.Name == "settings");
        Assert.NotNull(settingsField);
        Assert.Equal(ValidatorKind.Object, settingsField!.Type.Kind);
    }

    [Fact]
    public void Parse_SchemaWithComments_IgnoresComments()
    {
        var content = @"
// This is a comment
export default defineSchema({
    /* Multi-line
       comment */
    users: defineTable({
        name: v.string(), // inline comment
        email: v.string(),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal(2, tables[0].Fields.Count);
    }

    [Fact]
    public void Parse_EmptySchema_ReturnsEmptyList()
    {
        var content = @"
export default defineSchema({
});
";
        var tables = _parser.Parse(content);
        Assert.Empty(tables);
    }

    [Fact]
    public void Parse_NoDefineSchema_ReturnsEmptyList()
    {
        var content = @"
export const something = { foo: 'bar' };
";
        var tables = _parser.Parse(content);
        Assert.Empty(tables);
    }

    [Fact]
    public void Parse_PluralTableName_SingularizesPascalName()
    {
        var content = @"
export default defineSchema({
    messages: defineTable({ content: v.string() }),
    categories: defineTable({ name: v.string() }),
    statuses: defineTable({ value: v.string() }),
});
";
        var tables = _parser.Parse(content);

        Assert.Equal(3, tables.Count);

        var messagesTable = tables.Find(t => t.Name == "messages");
        var categoriesTable = tables.Find(t => t.Name == "categories");
        var statusesTable = tables.Find(t => t.Name == "statuses");

        Assert.NotNull(messagesTable);
        Assert.NotNull(categoriesTable);
        Assert.NotNull(statusesTable);

        Assert.Equal("Message", messagesTable!.PascalName);
        Assert.Equal("Category", categoriesTable!.PascalName);
        Assert.Equal("Status", statusesTable!.PascalName);
    }

    [Fact]
    public void Parse_SnakeCaseTableName_ConvertsToPascalCase()
    {
        var content = @"
export default defineSchema({
    user_profiles: defineTable({ name: v.string() }),
    game_sessions: defineTable({ status: v.string() }),
});
";
        var tables = _parser.Parse(content);

        Assert.Equal(2, tables.Count);

        var userProfilesTable = tables.Find(t => t.Name == "user_profiles");
        var gameSessionsTable = tables.Find(t => t.Name == "game_sessions");

        Assert.NotNull(userProfilesTable);
        Assert.NotNull(gameSessionsTable);

        Assert.Equal("UserProfile", userProfilesTable!.PascalName);
        Assert.Equal("GameSession", gameSessionsTable!.PascalName);
    }

    [Fact]
    public void Parse_TableWithIdField_ParsesCorrectly()
    {
        var content = @"
export default defineSchema({
    posts: defineTable({
        authorId: v.id(""users""),
        categoryId: v.optional(v.id(""categories"")),
    }),
});
";
        var tables = _parser.Parse(content);

        Assert.Single(tables);
        Assert.Equal(2, tables[0].Fields.Count);

        var authorIdField = tables[0].Fields.Find(f => f.Name == "authorId");
        Assert.NotNull(authorIdField);
        Assert.Equal(ValidatorKind.Id, authorIdField!.Type.Kind);
        Assert.Equal("users", authorIdField.Type.TableName);
        Assert.False(authorIdField.IsOptional);

        var categoryIdField = tables[0].Fields.Find(f => f.Name == "categoryId");
        Assert.NotNull(categoryIdField);
        Assert.True(categoryIdField!.IsOptional);
        Assert.Equal(ValidatorKind.Optional, categoryIdField.Type.Kind);
        Assert.Equal(ValidatorKind.Id, categoryIdField.Type.InnerType!.Kind);
    }
}
