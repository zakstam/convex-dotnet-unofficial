#nullable enable

using System.Collections.Generic;
using System.Linq;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.TypeMapping;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Modules;

/// <summary>
/// Generates strongly-typed document ID wrapper types from Convex schemas and functions.
/// </summary>
public class IdModule : IGenerationModule
{
    public string Name => "Ids";

    public bool IsEnabled(GeneratorOptions options) => options.GenerateTypedIds;

    public IEnumerable<GeneratedFile> Generate(
        IReadOnlyList<TableDefinition> tables,
        IReadOnlyList<FunctionDefinition> functions,
        GeneratorOptions options)
    {
        var idTypes = new Dictionary<string, IdDefinition>();

        // Collect IDs from table definitions (tables themselves have IDs)
        foreach (var table in tables)
        {
            var typeName = NamingConventions.Singularize(table.PascalName) + "Id";
            if (!idTypes.ContainsKey(table.Name))
            {
                idTypes[table.Name] = new IdDefinition
                {
                    TypeName = typeName,
                    TableName = table.Name
                };
            }
        }

        // Collect IDs from table fields that reference other tables
        foreach (var table in tables)
        {
            CollectIdsFromFields(table.Fields, idTypes);
        }

        // Collect IDs from function arguments
        foreach (var func in functions)
        {
            foreach (var arg in func.Arguments)
            {
                if (arg.ValidatorType != null)
                {
                    CollectIdsFromValidator(arg.ValidatorType, idTypes);
                }
            }

            // Collect IDs from return types
            if (func.ReturnType != null)
            {
                CollectIdsFromValidator(func.ReturnType, idTypes);
            }
        }

        if (idTypes.Count == 0)
        {
            yield break;
        }

        var sb = new SourceBuilder();

        sb.EmitFileHeader("This file contains strongly-typed document ID types for your Convex tables.");
        sb.EmitUsings("System", "System.Text.Json", "System.Text.Json.Serialization");
        sb.OpenNamespace(options.ModelsNamespace);

        foreach (var idDef in idTypes.Values.OrderBy(i => i.TypeName))
        {
            EmitIdType(sb, idDef);
        }

        sb.CloseNamespace();

        yield return new GeneratedFile
        {
            FileName = "ConvexIds.g.cs",
            Content = sb.ToString()
        };
    }

    private void CollectIdsFromFields(List<FieldDefinition> fields, Dictionary<string, IdDefinition> idTypes)
    {
        foreach (var field in fields)
        {
            CollectIdsFromValidator(field.Type, idTypes);
        }
    }

    private void CollectIdsFromValidator(ValidatorType validator, Dictionary<string, IdDefinition> idTypes)
    {
        switch (validator.Kind)
        {
            case ValidatorKind.Id when !string.IsNullOrEmpty(validator.TableName):
                var tableName = validator.TableName!;
                if (!idTypes.ContainsKey(tableName))
                {
                    var pascalName = NamingConventions.ToPascalCase(tableName);
                    idTypes[tableName] = new IdDefinition
                    {
                        TypeName = NamingConventions.Singularize(pascalName) + "Id",
                        TableName = tableName
                    };
                }
                break;

            case ValidatorKind.Array when validator.ElementType != null:
                CollectIdsFromValidator(validator.ElementType, idTypes);
                break;

            case ValidatorKind.Optional when validator.InnerType != null:
                CollectIdsFromValidator(validator.InnerType, idTypes);
                break;

            case ValidatorKind.Object when validator.Fields != null:
                CollectIdsFromFields(validator.Fields, idTypes);
                break;

            case ValidatorKind.Union when validator.UnionMembers != null:
                foreach (var member in validator.UnionMembers)
                {
                    CollectIdsFromValidator(member, idTypes);
                }
                break;

            case ValidatorKind.Record:
                if (validator.KeyType != null)
                {
                    CollectIdsFromValidator(validator.KeyType, idTypes);
                }
                if (validator.ValueType != null)
                {
                    CollectIdsFromValidator(validator.ValueType, idTypes);
                }
                break;
        }
    }

    private static void EmitIdType(SourceBuilder sb, IdDefinition idDef)
    {
        sb.EmitSummary($"Strongly-typed document ID for the '{idDef.TableName}' table.");
        sb.AppendLine("[JsonConverter(typeof(" + idDef.TypeName + "JsonConverter))]");
        sb.AppendLine($"public readonly record struct {idDef.TypeName}(string Value)");
        sb.AppendLine("{");
        sb.Indent();

        // Implicit conversion from string
        sb.AppendLine($"public static implicit operator {idDef.TypeName}(string value) => new(value);");
        sb.AppendLine();

        // Implicit conversion to string
        sb.AppendLine($"public static implicit operator string({idDef.TypeName} id) => id.Value;");
        sb.AppendLine();

        // ToString override
        sb.AppendLine("public override string ToString() => Value;");

        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();

        // JSON Converter
        EmitJsonConverter(sb, idDef);
    }

    private static void EmitJsonConverter(SourceBuilder sb, IdDefinition idDef)
    {
        sb.EmitSummary($"JSON converter for {idDef.TypeName}.");
        sb.AppendLine($"public class {idDef.TypeName}JsonConverter : JsonConverter<{idDef.TypeName}>");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine($"public override {idDef.TypeName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine("var value = reader.GetString() ?? throw new JsonException(\"Expected string value\");");
        sb.AppendLine($"return new {idDef.TypeName}(value);");
        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"public override void Write(Utf8JsonWriter writer, {idDef.TypeName} value, JsonSerializerOptions options)");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine("writer.WriteStringValue(value.Value);");
        sb.Outdent();
        sb.AppendLine("}");

        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();
    }
}
