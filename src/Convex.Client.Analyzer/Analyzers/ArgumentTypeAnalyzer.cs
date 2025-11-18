using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX008: Type safety for function arguments.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArgumentTypeAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX008_ArgumentTypeSafety);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Get the method symbol
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check if this is WithArgs call on a builder
        if (methodSymbol.Name != "WithArgs" || !ConvexAnalyzerHelpers.IsBuilderType(methodSymbol.ContainingType))
        {
            return;
        }

        // Check if the argument is an anonymous object
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        var argExpression = invocation.ArgumentList.Arguments[0].Expression;

        // Check for anonymous object creation
        if (argExpression is AnonymousObjectCreationExpressionSyntax)
        {
            // Report diagnostic suggesting typed arguments
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX008_ArgumentTypeSafety,
                argExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}

