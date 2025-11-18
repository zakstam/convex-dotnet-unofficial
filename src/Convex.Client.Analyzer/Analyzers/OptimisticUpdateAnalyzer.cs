using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX009: Optimistic update best practices.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OptimisticUpdateAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX009_OptimisticUpdateIssue);

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

        // Check for optimistic update methods
        if (methodSymbol.Name is "OptimisticWithAutoRollback" or "Optimistic" or "OptimisticWith")
        {
            // Check if it's on a query builder (should be mutation only)
            if (IsQueryBuilder(methodSymbol.ContainingType))
            {
                var message = "Optimistic updates should only be used with mutations, not queries.";
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX009_OptimisticUpdateIssue,
                    invocation.GetLocation(),
                    message);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Check if rollback handler is provided
            if (methodSymbol.Name == "Optimistic" && !HasRollbackHandler(context, invocation))
            {
                var message = "Optimistic updates should include a rollback handler to handle failures gracefully.";
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX009_OptimisticUpdateIssue,
                    invocation.GetLocation(),
                    message);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsQueryBuilder(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        return typeSymbol.Name == "IQueryBuilder" ||
               typeSymbol.AllInterfaces.Any(i => i.Name == "IQueryBuilder" &&
                   i.ContainingNamespace?.ToDisplayString() == "Convex.Client.Slices.Queries");
    }

    private static bool HasRollbackHandler(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        // Check if the invocation has a rollback parameter
        if (invocation.ArgumentList.Arguments.Count > 1)
        {
            return true;
        }

        // Check if there's a rollback handler in the chain
        var current = invocation;
        while (current != null)
        {
            if (current.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.ValueText.Contains("Rollback"))
                {
                    return true;
                }
            }

            if (current.Parent is InvocationExpressionSyntax parentInvocation)
            {
                current = parentInvocation;
            }
            else
            {
                break;
            }
        }

        return false;
    }
}

