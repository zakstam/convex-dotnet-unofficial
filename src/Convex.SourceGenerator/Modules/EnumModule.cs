#nullable enable

using System.Collections.Generic;
using System.Linq;
using Convex.SourceGenerator.Core.Models;
using Convex.SourceGenerator.Core.TypeMapping;
using Convex.SourceGenerator.Core.Utilities;

namespace Convex.SourceGenerator.Modules;

/// <summary>
/// Generates C# enums from string literal unions in Convex schemas and functions.
/// </summary>
public class EnumModule : IGenerationModule
{
    public string Name => "Enums";

    public bool IsEnabled(GeneratorOptions options) => options.GenerateModels;

    public IEnumerable<GeneratedFile> Generate(
        IReadOnlyList<TableDefinition> tables,
        IReadOnlyList<FunctionDefinition> functions,
        GeneratorOptions options)
    {
        var enums = new Dictionary<string, EnumDefinition>();

        // Collect enums from table fields
        foreach (var table in tables)
        {
            CollectEnumsFromFields(table.Fields, table.PascalName, enums);
        }

        // Collect enums from function arguments
        foreach (var func in functions)
        {
            var funcContext = $"{NamingConventions.ToModuleClassName(func.ModulePath)}{func.Name}";
            foreach (var arg in func.Arguments)
            {
                if (arg.ValidatorType != null)
                {
                    CollectEnumsFromValidator(arg.ValidatorType, funcContext, arg.Name, enums);
                }
            }

            // Collect enums from function return types
            if (func.ReturnType != null)
            {
                CollectEnumsFromValidator(func.ReturnType, funcContext, "Result", enums);
            }
        }

        if (enums.Count == 0)
        {
            yield break;
        }

        var sb = new SourceBuilder();

        sb.EmitFileHeader("This file contains enums generated from string literal unions in your Convex schema.");
        sb.EmitUsings("System.Runtime.Serialization", "System.Text.Json.Serialization");
        sb.OpenNamespace(options.ModelsNamespace);

        foreach (var enumDef in enums.Values.OrderBy(e => e.Name))
        {
            EmitEnum(sb, enumDef);
        }

        sb.CloseNamespace();

        yield return new GeneratedFile
        {
            FileName = "ConvexEnums.g.cs",
            Content = sb.ToString()
        };
    }

    private void CollectEnumsFromFields(List<FieldDefinition> fields, string parentName, Dictionary<string, EnumDefinition> enums)
    {
        foreach (var field in fields)
        {
            CollectEnumsFromValidator(field.Type, parentName, field.Name, enums);
        }
    }

    private void CollectEnumsFromValidator(ValidatorType validator, string parentName, string fieldName, Dictionary<string, EnumDefinition> enums)
    {
        var context = new TypeMappingContext
        {
            ParentName = parentName,
            FieldName = fieldName,
            EnumCallback = enumDef =>
            {
                if (!enums.ContainsKey(enumDef.Name))
                {
                    enums[enumDef.Name] = enumDef;
                }
            }
        };

        // This will trigger enum collection via the callback
        ConvexTypeMapper.MapToCSharpTypeWithContext(validator, context);

        // Also recursively check nested types
        switch (validator.Kind)
        {
            case ValidatorKind.Array when validator.ElementType != null:
                CollectEnumsFromValidator(validator.ElementType, parentName, fieldName, enums);
                break;
            case ValidatorKind.Optional when validator.InnerType != null:
                CollectEnumsFromValidator(validator.InnerType, parentName, fieldName, enums);
                break;
            case ValidatorKind.Object when validator.Fields != null:
                var nestedTypeName = $"{parentName}{NamingConventions.ToPascalCase(fieldName)}";
                CollectEnumsFromFields(validator.Fields, nestedTypeName, enums);
                break;
            case ValidatorKind.Record when validator.ValueType != null:
                CollectEnumsFromValidator(validator.ValueType, parentName, fieldName, enums);
                break;
        }
    }

    private static void EmitEnum(SourceBuilder sb, EnumDefinition enumDef)
    {
        if (!string.IsNullOrEmpty(enumDef.Description))
        {
            sb.EmitSummary(enumDef.Description!);
        }

        sb.AppendLine("[JsonConverter(typeof(JsonStringEnumConverter))]");
        sb.AppendLine($"public enum {enumDef.Name}");
        sb.AppendLine("{");
        sb.Indent();

        for (var i = 0; i < enumDef.Members.Count; i++)
        {
            var member = enumDef.Members[i];
            var isLast = i == enumDef.Members.Count - 1;

            // Add EnumMember attribute for proper serialization
            sb.AppendLine($"[EnumMember(Value = \"{member.Value}\")]");
            sb.AppendLine($"{member.Name}{(isLast ? "" : ",")}");

            if (!isLast)
            {
                sb.AppendLine();
            }
        }

        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();
    }
}
