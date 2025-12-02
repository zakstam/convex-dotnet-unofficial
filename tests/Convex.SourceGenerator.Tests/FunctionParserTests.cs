using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.Parsing;
using Xunit;

namespace Convex.SourceGenerator.Tests;

public class FunctionParserTests
{
    private readonly FunctionParser _parser = new();

    [Theory]
    [InlineData("/some/path/convex/functions.ts", "functions")]
    [InlineData("/project/convex/api/users.ts", "api/users")]
    [InlineData("C:\\project\\convex\\messages.ts", "messages")]
    [InlineData("/project/convex/rooms/queries.ts", "rooms/queries")]
    public void ExtractModulePath_ValidPaths_ReturnsCorrectModulePath(string filePath, string expectedModulePath)
    {
        var result = FunctionParser.ExtractModulePath(filePath);
        Assert.Equal(expectedModulePath, result);
    }

    [Fact]
    public void Parse_QueryFunction_ExtractsCorrectly()
    {
        var content = @"
import { query } from ""./_generated/server"";
import { v } from ""convex/values"";

export const getUser = query({
    args: { userId: v.id(""users"") },
    handler: async (ctx, { userId }) => {
        return await ctx.db.get(userId);
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        Assert.Equal("users:getUser", functions[0].Path);
        Assert.Equal("GetUser", functions[0].Name);
        Assert.Equal("Query", functions[0].Type);
        Assert.False(functions[0].IsDefaultExport);
        Assert.Single(functions[0].Arguments);
        Assert.Equal("userId", functions[0].Arguments[0].Name);
    }

    [Fact]
    public void Parse_MutationFunction_ExtractsCorrectly()
    {
        var content = @"
import { mutation } from ""./_generated/server"";
import { v } from ""convex/values"";

export const createMessage = mutation({
    args: {
        roomId: v.id(""rooms""),
        content: v.string(),
    },
    handler: async (ctx, { roomId, content }) => {
        return await ctx.db.insert(""messages"", { roomId, content });
    },
});
";
        var functions = _parser.Parse(content, "messages");

        Assert.Single(functions);
        Assert.Equal("messages:createMessage", functions[0].Path);
        Assert.Equal("CreateMessage", functions[0].Name);
        Assert.Equal("Mutation", functions[0].Type);
        Assert.Equal(2, functions[0].Arguments.Count);
    }

    [Fact]
    public void Parse_ActionFunction_ExtractsCorrectly()
    {
        var content = @"
import { action } from ""./_generated/server"";
import { v } from ""convex/values"";

export const sendEmail = action({
    args: {
        to: v.string(),
        subject: v.string(),
        body: v.string(),
    },
    handler: async (ctx, { to, subject, body }) => {
        // Send email
    },
});
";
        var functions = _parser.Parse(content, "email");

        Assert.Single(functions);
        Assert.Equal("email:sendEmail", functions[0].Path);
        Assert.Equal("SendEmail", functions[0].Name);
        Assert.Equal("Action", functions[0].Type);
        Assert.Equal(3, functions[0].Arguments.Count);
    }

    [Fact]
    public void Parse_MultipleFunctions_ExtractsAll()
    {
        var content = @"
import { query, mutation } from ""./_generated/server"";
import { v } from ""convex/values"";

export const getRoom = query({
    args: { roomId: v.id(""rooms"") },
    handler: async (ctx, { roomId }) => {
        return await ctx.db.get(roomId);
    },
});

export const createRoom = mutation({
    args: { name: v.string() },
    handler: async (ctx, { name }) => {
        return await ctx.db.insert(""rooms"", { name });
    },
});

export const deleteRoom = mutation({
    args: { roomId: v.id(""rooms"") },
    handler: async (ctx, { roomId }) => {
        await ctx.db.delete(roomId);
    },
});
";
        var functions = _parser.Parse(content, "rooms");

        Assert.Equal(3, functions.Count);
        Assert.Contains(functions, f => f.Name == "GetRoom" && f.Type == "Query");
        Assert.Contains(functions, f => f.Name == "CreateRoom" && f.Type == "Mutation");
        Assert.Contains(functions, f => f.Name == "DeleteRoom" && f.Type == "Mutation");
    }

    [Fact]
    public void Parse_DefaultExport_ExtractsCorrectly()
    {
        var content = @"
import { query } from ""./_generated/server"";
import { v } from ""convex/values"";

export default query({
    args: { id: v.id(""items"") },
    handler: async (ctx, { id }) => {
        return await ctx.db.get(id);
    },
});
";
        var functions = _parser.Parse(content, "items/get");

        Assert.Single(functions);
        Assert.Equal("items/get", functions[0].Path);
        Assert.Equal("Get", functions[0].Name);
        Assert.Equal("Query", functions[0].Type);
        Assert.True(functions[0].IsDefaultExport);
    }

    [Fact]
    public void Parse_FunctionWithOptionalArgs_MarksAsOptional()
    {
        var content = @"
export const search = query({
    args: {
        query: v.string(),
        limit: v.optional(v.number()),
    },
    handler: async (ctx, args) => {
        // Search logic
    },
});
";
        var functions = _parser.Parse(content, "search");

        Assert.Single(functions);
        Assert.Equal(2, functions[0].Arguments.Count);

        var queryArg = functions[0].Arguments.Find(a => a.Name == "query");
        var limitArg = functions[0].Arguments.Find(a => a.Name == "limit");

        Assert.NotNull(queryArg);
        Assert.NotNull(limitArg);

        Assert.False(queryArg!.IsOptional);
        Assert.True(limitArg!.IsOptional);
    }

    [Fact]
    public void Parse_FunctionWithNoArgs_ReturnsEmptyArgsList()
    {
        var content = @"
export const listAll = query({
    handler: async (ctx) => {
        return await ctx.db.query(""items"").collect();
    },
});
";
        var functions = _parser.Parse(content, "items");

        Assert.Single(functions);
        Assert.Empty(functions[0].Arguments);
    }

    [Fact]
    public void Parse_FunctionWithEmptyArgs_ReturnsEmptyArgsList()
    {
        var content = @"
export const listAll = query({
    args: {},
    handler: async (ctx) => {
        return await ctx.db.query(""items"").collect();
    },
});
";
        var functions = _parser.Parse(content, "items");

        Assert.Single(functions);
        Assert.Empty(functions[0].Arguments);
    }

    [Fact]
    public void Parse_FunctionWithComplexArgs_ParsesCorrectly()
    {
        var content = @"
export const createGame = mutation({
    args: {
        playerIds: v.array(v.id(""users"")),
        settings: v.object({
            maxPlayers: v.number(),
            timeLimit: v.optional(v.number()),
        }),
    },
    handler: async (ctx, args) => {
        // Create game
    },
});
";
        var functions = _parser.Parse(content, "games");

        Assert.Single(functions);
        Assert.Equal(2, functions[0].Arguments.Count);

        var playerIdsArg = functions[0].Arguments.Find(a => a.Name == "playerIds");
        var settingsArg = functions[0].Arguments.Find(a => a.Name == "settings");

        Assert.NotNull(playerIdsArg);
        Assert.NotNull(settingsArg);
        Assert.NotNull(playerIdsArg!.ValidatorType);
        Assert.NotNull(settingsArg!.ValidatorType);

        Assert.Equal(ValidatorKind.Array, playerIdsArg.ValidatorType!.Kind);
        Assert.Equal(ValidatorKind.Object, settingsArg.ValidatorType!.Kind);
    }

    [Fact]
    public void Parse_SnakeCaseFunctionName_ConvertsToPascalCase()
    {
        var content = @"
export const get_user_by_email = query({
    args: { email: v.string() },
    handler: async (ctx, { email }) => {
        // Get user
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        Assert.Equal("GetUserByEmail", functions[0].Name);
    }

    [Fact]
    public void Parse_MixedNamedAndDefaultExports_ExtractsBoth()
    {
        var content = @"
import { query, mutation } from ""./_generated/server"";
import { v } from ""convex/values"";

export const getById = query({
    args: { id: v.id(""items"") },
    handler: async (ctx, { id }) => ctx.db.get(id),
});

export default mutation({
    args: { name: v.string() },
    handler: async (ctx, { name }) => ctx.db.insert(""items"", { name }),
});
";
        var functions = _parser.Parse(content, "items");

        Assert.Equal(2, functions.Count);

        var namedExport = functions.Find(f => !f.IsDefaultExport);
        var defaultExport = functions.Find(f => f.IsDefaultExport);

        Assert.NotNull(namedExport);
        Assert.NotNull(defaultExport);

        Assert.Equal("GetById", namedExport!.Name);
        Assert.Equal("Query", namedExport.Type);

        Assert.Equal("Items", defaultExport!.Name);
        Assert.Equal("Mutation", defaultExport.Type);
    }

    [Fact]
    public void Parse_NestedModulePath_ExtractsCorrectPath()
    {
        var content = @"
export const send = mutation({
    args: { message: v.string() },
    handler: async (ctx, { message }) => {},
});
";
        var functions = _parser.Parse(content, "api/v1/messages");

        Assert.Single(functions);
        Assert.Equal("api/v1/messages:send", functions[0].Path);
        Assert.Equal("api/v1/messages", functions[0].ModulePath);
    }

    [Fact]
    public void Parse_CaseInsensitiveFunctionTypes_ParsesCorrectly()
    {
        var content = @"
export const q1 = Query({ args: {}, handler: async (ctx) => {} });
export const m1 = MUTATION({ args: {}, handler: async (ctx) => {} });
export const a1 = Action({ args: {}, handler: async (ctx) => {} });
";
        var functions = _parser.Parse(content, "test");

        Assert.Equal(3, functions.Count);
        Assert.Contains(functions, f => f.Type == "Query");
        Assert.Contains(functions, f => f.Type == "Mutation");
        Assert.Contains(functions, f => f.Type == "Action");
    }

    [Fact]
    public void Parse_FunctionWithReturnsValidator_ParsesReturnType()
    {
        var content = @"
export const getUser = query({
    args: { userId: v.id(""users"") },
    returns: v.object({
        name: v.string(),
        email: v.string(),
    }),
    handler: async (ctx, { userId }) => {
        return await ctx.db.get(userId);
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        var returnType = functions[0].ReturnType;
        Assert.NotNull(returnType);
        Assert.Equal(ValidatorKind.Object, returnType!.Kind);
        Assert.NotNull(returnType.Fields);
        Assert.Equal(2, returnType.Fields!.Count);
    }

    [Fact]
    public void Parse_FunctionWithSimpleReturnType_ParsesCorrectly()
    {
        var content = @"
export const getMessage = query({
    args: { messageId: v.id(""messages"") },
    returns: v.string(),
    handler: async (ctx, { messageId }) => {
        return ""hello"";
    },
});
";
        var functions = _parser.Parse(content, "messages");

        Assert.Single(functions);
        var returnType = functions[0].ReturnType;
        Assert.NotNull(returnType);
        Assert.Equal(ValidatorKind.String, returnType!.Kind);
    }

    [Fact]
    public void Parse_FunctionWithNullableReturnType_ParsesCorrectly()
    {
        var content = @"
export const findUser = query({
    args: { email: v.string() },
    returns: v.union(v.object({ name: v.string() }), v.null()),
    handler: async (ctx, { email }) => {
        return null;
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        var returnType = functions[0].ReturnType;
        Assert.NotNull(returnType);
        Assert.Equal(ValidatorKind.Union, returnType!.Kind);
        Assert.NotNull(returnType.UnionMembers);
        Assert.Equal(2, returnType.UnionMembers!.Count);
    }

    [Fact]
    public void Parse_FunctionWithArrayReturnType_ParsesCorrectly()
    {
        var content = @"
export const listUsers = query({
    args: {},
    returns: v.array(v.object({ id: v.id(""users""), name: v.string() })),
    handler: async (ctx) => {
        return [];
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        var returnType = functions[0].ReturnType;
        Assert.NotNull(returnType);
        Assert.Equal(ValidatorKind.Array, returnType!.Kind);
        Assert.NotNull(returnType.ElementType);
        Assert.Equal(ValidatorKind.Object, returnType.ElementType!.Kind);
    }

    [Fact]
    public void Parse_FunctionWithoutReturnsValidator_ReturnsNullReturnType()
    {
        var content = @"
export const createUser = mutation({
    args: { name: v.string() },
    handler: async (ctx, { name }) => {
        return await ctx.db.insert(""users"", { name });
    },
});
";
        var functions = _parser.Parse(content, "users");

        Assert.Single(functions);
        Assert.Null(functions[0].ReturnType);
    }

    [Fact]
    public void Parse_DefaultExportWithReturnsValidator_ParsesReturnType()
    {
        var content = @"
export default query({
    args: { id: v.id(""items"") },
    returns: v.optional(v.string()),
    handler: async (ctx, { id }) => {
        return null;
    },
});
";
        var functions = _parser.Parse(content, "items/get");

        Assert.Single(functions);
        var returnType = functions[0].ReturnType;
        Assert.NotNull(returnType);
        Assert.Equal(ValidatorKind.Optional, returnType!.Kind);
        Assert.NotNull(returnType.InnerType);
        Assert.Equal(ValidatorKind.String, returnType.InnerType!.Kind);
    }
}
