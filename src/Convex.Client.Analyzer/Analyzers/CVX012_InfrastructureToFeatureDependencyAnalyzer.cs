using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX012: No infrastructure-to-feature dependencies.
/// Ensures that Infrastructure layer does not reference Features layer.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InfrastructureToFeatureDependencyAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX012_NoInfrastructureToFeatureDependencies);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register action for named types (classes, interfaces, structs)
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze types in Infrastructure namespace
        var containingNamespace = namedType.ContainingNamespace?.ToDisplayString();
        if (!ConvexAnalyzerHelpers.IsInfrastructureNamespace(containingNamespace))
        {
            return;
        }

        // Analyze all type references
        AnalyzeTypeReferences(context, namedType);
    }

    private static void AnalyzeTypeReferences(SymbolAnalysisContext context, INamedTypeSymbol namedType)
    {
        // Check base type
        if (namedType.BaseType != null)
        {
            CheckForFeatureDependency(context, namedType, namedType.BaseType);
        }

        // Check interfaces
        foreach (var interfaceType in namedType.Interfaces)
        {
            CheckForFeatureDependency(context, namedType, interfaceType);
        }

        // Check fields
        foreach (var member in namedType.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                CheckForFeatureDependency(context, namedType, field.Type);
            }
            else if (member is IPropertySymbol property)
            {
                CheckForFeatureDependency(context, namedType, property.Type);
            }
            else if (member is IMethodSymbol method)
            {
                // Check return type
                CheckForFeatureDependency(context, namedType, method.ReturnType);

                // Check parameters
                foreach (var parameter in method.Parameters)
                {
                    CheckForFeatureDependency(context, namedType, parameter.Type);
                }
            }
        }
    }

    private static void CheckForFeatureDependency(SymbolAnalysisContext context, INamedTypeSymbol infrastructureType, ITypeSymbol referencedType)
    {
        var referencedNamespace = referencedType.ContainingNamespace?.ToDisplayString();

        // Check if referencing a feature type
        if (ConvexAnalyzerHelpers.IsFeatureNamespace(referencedNamespace))
        {
            // Allow ConvexClient facade references
            if (referencedType is INamedTypeSymbol namedTypeSymbol &&
                ConvexAnalyzerHelpers.IsConvexFacade(namedTypeSymbol))
            {
                return;
            }

            // Report violation
            var location = infrastructureType.Locations.Length > 0 ? infrastructureType.Locations[0] : Location.None;
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX012_NoInfrastructureToFeatureDependencies,
                location,
                infrastructureType.ToDisplayString(),
                referencedType.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
