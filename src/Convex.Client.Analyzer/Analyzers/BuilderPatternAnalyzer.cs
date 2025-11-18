using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX007: Builder pattern best practices.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BuilderPatternAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX007_BuilderPatternIssue);

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

        // Check if this is a builder method
        if (!ConvexAnalyzerHelpers.IsBuilderType(methodSymbol.ContainingType))
        {
            return;
        }

        // Check for missing ExecuteAsync
        if (IsBuilderChainWithoutExecuteAsync(context, invocation, methodSymbol))
        {
            var message = "Builder chain does not call ExecuteAsync(). The operation will not be executed.";
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX007_BuilderPatternIssue,
                invocation.GetLocation(),
                message);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check for methods called after ExecuteAsync (invalid chaining)
        if (IsMethodCalledAfterExecuteAsync(context, invocation, methodSymbol))
        {
            var message = "Builder methods cannot be called after ExecuteAsync().";
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX007_BuilderPatternIssue,
                invocation.GetLocation(),
                message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsBuilderChainWithoutExecuteAsync(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
    {
        // Check if this is a Query/Mutate/Action call that creates a builder
        if (methodSymbol.ContainingType.Name is not ("IQueryBuilder" or "IMutationBuilder" or "IActionBuilder"))
        {
            return false;
        }

        // Check if the parent is an assignment or variable declaration without ExecuteAsync
        var parent = invocation.Parent;
        if (parent is AssignmentExpressionSyntax || parent is VariableDeclaratorSyntax)
        {
            // Check if ExecuteAsync is called anywhere in the chain
            if (!HasExecuteAsyncInChain(invocation))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasExecuteAsyncInChain(InvocationExpressionSyntax invocation)
    {
        // Check if this invocation or any parent invocation is ExecuteAsync
        var current = invocation;
        while (current != null)
        {
            if (current.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.ValueText == "ExecuteAsync")
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

    private static bool IsMethodCalledAfterExecuteAsync(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
    {
        // Check if this is a builder method (not ExecuteAsync itself)
        if (methodSymbol.Name == "ExecuteAsync")
        {
            return false;
        }

        // Check if ExecuteAsync appears earlier in the chain
        var current = invocation;
        while (current != null)
        {
            if (current.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.ValueText == "ExecuteAsync")
                {
                    // Found ExecuteAsync - check if there are more method calls after it
                    if (current.Parent is InvocationExpressionSyntax)
                    {
                        return true;
                    }
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

