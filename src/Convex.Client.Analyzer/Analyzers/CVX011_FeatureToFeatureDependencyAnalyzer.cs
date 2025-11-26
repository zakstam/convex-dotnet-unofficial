using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX011: No feature-to-feature dependencies.
/// Ensures strict feature isolation - features cannot reference other features, even within the same module.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FeatureToFeatureDependencyAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX011_NoFeatureToFeatureDependencies);

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

        // Only analyze types in Features namespace
        var containingNamespace = namedType.ContainingNamespace?.ToDisplayString();
        if (!ConvexAnalyzerHelpers.IsFeatureNamespace(containingNamespace))
        {
            return;
        }

        // Get this feature's identity (module + feature)
        var (currentModule, currentFeature) = ConvexAnalyzerHelpers.ParseFeatureNamespace(containingNamespace);
        if (currentModule == null || currentFeature == null)
        {
            return;
        }

        // Analyze all type references
        AnalyzeTypeReferences(context, namedType, currentModule!, currentFeature!);
    }

    private static void AnalyzeTypeReferences(
        SymbolAnalysisContext context,
        INamedTypeSymbol namedType,
        string currentModule,
        string currentFeature)
    {
        // Check base type
        if (namedType.BaseType != null)
        {
            CheckForFeatureDependency(context, namedType, namedType.BaseType, currentModule, currentFeature);
        }

        // Check interfaces
        foreach (var interfaceType in namedType.Interfaces)
        {
            CheckForFeatureDependency(context, namedType, interfaceType, currentModule, currentFeature);
        }

        // Check members (fields, properties, methods)
        foreach (var member in namedType.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                CheckForFeatureDependency(context, namedType, field.Type, currentModule, currentFeature);
            }
            else if (member is IPropertySymbol property)
            {
                CheckForFeatureDependency(context, namedType, property.Type, currentModule, currentFeature);
            }
            else if (member is IMethodSymbol method)
            {
                // Check return type
                CheckForFeatureDependency(context, namedType, method.ReturnType, currentModule, currentFeature);

                // Check parameters
                foreach (var parameter in method.Parameters)
                {
                    CheckForFeatureDependency(context, namedType, parameter.Type, currentModule, currentFeature);
                }
            }
        }
    }

    private static void CheckForFeatureDependency(
        SymbolAnalysisContext context,
        INamedTypeSymbol featureType,
        ITypeSymbol referencedType,
        string currentModule,
        string currentFeature)
    {
        var referencedNamespace = referencedType.ContainingNamespace?.ToDisplayString();

        // Check if referencing another feature
        if (ConvexAnalyzerHelpers.IsFeatureNamespace(referencedNamespace))
        {
            var (referencedModule, referencedFeature) = ConvexAnalyzerHelpers.ParseFeatureNamespace(referencedNamespace);

            // STRICT ISOLATION: Features cannot reference ANY other feature,
            // even within the same module
            if (referencedModule != currentModule || referencedFeature != currentFeature)
            {
                // Allow ConvexClient facade references
                if (referencedType is INamedTypeSymbol namedTypeSymbol &&
                    ConvexAnalyzerHelpers.IsConvexFacade(namedTypeSymbol))
                {
                    return;
                }

                // Report violation
                var currentFeaturePath = $"{currentModule}.{currentFeature}";
                var referencedFeaturePath = $"{referencedModule}.{referencedFeature}";

                var location = featureType.Locations.Length > 0 ? featureType.Locations[0] : Location.None;
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX011_NoFeatureToFeatureDependencies,
                    location,
                    currentFeaturePath,
                    referencedFeaturePath);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
