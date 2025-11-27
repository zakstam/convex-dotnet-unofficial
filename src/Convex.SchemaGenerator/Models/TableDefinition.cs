#nullable enable

using System.Collections.Generic;

namespace Convex.SchemaGenerator.Models;

/// <summary>
/// Represents a table definition in a Convex schema.
/// </summary>
public class TableDefinition
{
    /// <summary>
    /// The name of the table as it appears in the schema.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The PascalCase name for the generated C# class.
    /// </summary>
    public string PascalName { get; set; } = string.Empty;

    /// <summary>
    /// The fields defined in the table.
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new();

    /// <summary>
    /// The indexes defined on the table.
    /// </summary>
    public List<IndexDefinition> Indexes { get; set; } = new();
}
