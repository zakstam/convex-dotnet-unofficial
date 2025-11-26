using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX013: Namespace structure validation.
/// Ensures namespaces follow the vertical slice architecture convention:
/// - Features: Convex.Client.Features.[Module].[Feature]
/// - Infrastructure: Convex.Client.Infrastructure.[Concern]
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamespaceStructureAnalyzer : ConvexAnalyzerBase
{
    // Regex patterns for valid namespaces
    private static readonly Regex FeatureNamespacePattern = new(
        @"^Convex\.Client\.Features\.[A-Z][a-zA-Z]+\.[A-Z][a-zA-Z]+(\..+)?$",
        RegexOptions.Compiled);

    private static readonly Regex InfrastructureNamespacePattern = new(
        @"^Convex\.Client\.Infrastructure\.[A-Z][a-zA-Z]+(\..+)?$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX013_InvalidNamespaceStructure);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for both namespace declaration styles
        context.RegisterSyntaxNodeAction(AnalyzeNamespaceDeclaration, SyntaxKind.NamespaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeFileScopedNamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private static void AnalyzeNamespaceDeclaration(SyntaxNodeAnalysisContext context)
    {
        var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
        var namespaceName = namespaceDeclaration.Name.ToString();

        ValidateNamespaceStructure(context, namespaceName, namespaceDeclaration.GetLocation());
    }

    private static void AnalyzeFileScopedNamespaceDeclaration(SyntaxNodeAnalysisContext context)
    {
        var namespaceDeclaration = (FileScopedNamespaceDeclarationSyntax)context.Node;
        var namespaceName = namespaceDeclaration.Name.ToString();

        ValidateNamespaceStructure(context, namespaceName, namespaceDeclaration.GetLocation());
    }

    private static void ValidateNamespaceStructure(SyntaxNodeAnalysisContext context, string namespaceName, Location location)
    {
        // Skip if not in Convex.Client namespace
        if (!namespaceName.StartsWith("Convex.Client"))
        {
            return;
        }

        // Allow root namespace (Convex.Client)
        if (namespaceName == "Convex.Client")
        {
            return;
        }

        // Allow special-purpose namespaces that don't fit Features/Infrastructure pattern
        if (namespaceName.StartsWith("Convex.Client.Attributes") ||
            namespaceName.StartsWith("Convex.Client.DeveloperTools") ||
            namespaceName.StartsWith("Convex.Client.Extensions"))
        {
            return;
        }

        // Check if it's a Features namespace
        if (namespaceName.StartsWith("Convex.Client.Features."))
        {
            if (!FeatureNamespacePattern.IsMatch(namespaceName))
            {
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX013_InvalidNamespaceStructure,
                    location,
                    namespaceName);

                context.ReportDiagnostic(diagnostic);
            }

            return;
        }

        // Check if it's an Infrastructure namespace
        if (namespaceName.StartsWith("Convex.Client.Infrastructure."))
        {
            if (!InfrastructureNamespacePattern.IsMatch(namespaceName))
            {
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX013_InvalidNamespaceStructure,
                    location,
                    namespaceName);

                context.ReportDiagnostic(diagnostic);
            }

            return;
        }

        // If it's neither Features nor Infrastructure, and not the root namespace,
        // it's invalid (should only have Features/ and Infrastructure/ directories)
        if (namespaceName != "Convex.Client" &&
            !namespaceName.StartsWith("Convex.Client.Features.") &&
            !namespaceName.StartsWith("Convex.Client.Infrastructure."))
        {
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX013_InvalidNamespaceStructure,
                location,
                namespaceName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
