using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX001: Avoid direct IConvexClient method calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DirectClientCallAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX001_AvoidDirectClientCalls);

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

        // Check if this is a direct call to IConvexClient methods
        // Note: Since extension methods were removed, this analyzer focuses on detecting
        // patterns where builders are created but not properly used, or where direct
        // client access patterns could be improved
        if (!IsDirectClientCall(methodSymbol, invocation))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            ConvexDiagnosticDescriptors.CVX001_AvoidDirectClientCalls,
            invocation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsDirectClientCall(IMethodSymbol methodSymbol, InvocationExpressionSyntax invocation)
    {
        // Check if this is a method on IConvexClient interface
        if (!ConvexAnalyzerHelpers.IsConvexClientRelated(methodSymbol.ContainingType))
        {
            return false;
        }

        // Check for direct calls to Query, Mutate, Action that might bypass best practices
        // This is a conservative check - we only flag if it's clearly a direct interface call
        // and not through a builder pattern
        var methodName = methodSymbol.Name;
        if (methodName is "Query" or "Mutate" or "Action")
        {
            // Check if the result is immediately used in a way that suggests it's not following builder pattern
            // For now, we'll be conservative and not flag this since the fluent API is the recommended approach
            // This analyzer can be enhanced later with more sophisticated detection
            return false;
        }

        // Check for other direct interface methods that should use extensions
        return methodName is "Observe" && methodSymbol.ContainingType.TypeKind == TypeKind.Interface;
    }
}

