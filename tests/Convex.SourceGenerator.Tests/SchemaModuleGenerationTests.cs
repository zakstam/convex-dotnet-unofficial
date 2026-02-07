using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Modules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Convex.SourceGenerator.Tests;

public class SchemaModuleGenerationTests
{
    [Fact]
    public void Generate_TimestampFields_UsesGlobalQualifiedDateTimeOffset()
    {
        var generated = GenerateSchemaSource();

        Assert.Contains("using System;", generated, StringComparison.Ordinal);
        Assert.Contains("public global::System.DateTimeOffset CreationTime { get; init; }", generated, StringComparison.Ordinal);
        Assert.Contains("public global::System.DateTimeOffset CreatedAt { get; init; }", generated, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(ConvexNullableDateTimeOffsetJsonConverter))]", generated, StringComparison.Ordinal);
        Assert.Contains("public global::System.DateTimeOffset? EditedAt { get; init; }", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_TimestampFields_CompilesWithoutImplicitUsings()
    {
        var generated = GenerateSchemaSource();
        var supportSource = GetConverterStubs();
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(generated, parseOptions),
            CSharpSyntaxTree.ParseText(supportSource, parseOptions)
        };

        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList()
            ?? new List<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedSchemaTests",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(diagnostics);
    }

    private static string GenerateSchemaSource()
    {
        var module = new SchemaModule();
        var table = new TableDefinition
        {
            Name = "messages",
            PascalName = "Message",
            Fields =
            [
                FieldDefinition.Create("createdAt", ValidatorType.Simple(ValidatorKind.Number)),
                FieldDefinition.Create("editedAt", ValidatorType.Optional(ValidatorType.Simple(ValidatorKind.Number)), isOptional: true),
                FieldDefinition.Create("content", ValidatorType.Simple(ValidatorKind.String))
            ]
        };

        var files = module.Generate(
            [table],
            [],
            new GeneratorOptions()).ToList();

        return Assert.Single(files, f => f.FileName == "Message.g.cs").Content;
    }

    private static string GetConverterStubs() =>
        """
        namespace Convex.Client.Infrastructure.Serialization
        {
            public sealed class ConvexDateTimeOffsetJsonConverter : System.Text.Json.Serialization.JsonConverter<global::System.DateTimeOffset>
            {
                public override global::System.DateTimeOffset Read(
                    ref System.Text.Json.Utf8JsonReader reader,
                    global::System.Type typeToConvert,
                    System.Text.Json.JsonSerializerOptions options) => default;

                public override void Write(
                    System.Text.Json.Utf8JsonWriter writer,
                    global::System.DateTimeOffset value,
                    System.Text.Json.JsonSerializerOptions options) { }
            }

            public sealed class ConvexNullableDateTimeOffsetJsonConverter : System.Text.Json.Serialization.JsonConverter<global::System.DateTimeOffset?>
            {
                public override global::System.DateTimeOffset? Read(
                    ref System.Text.Json.Utf8JsonReader reader,
                    global::System.Type typeToConvert,
                    System.Text.Json.JsonSerializerOptions options) => default;

                public override void Write(
                    System.Text.Json.Utf8JsonWriter writer,
                    global::System.DateTimeOffset? value,
                    System.Text.Json.JsonSerializerOptions options) { }
            }
        }
        """;
}
