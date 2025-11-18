using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX010: Cache invalidation patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CacheInvalidationAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX010_CacheInvalidation);

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

        // Check if this is a Mutate call
        if (methodSymbol.Name != "Mutate" || !ConvexAnalyzerHelpers.IsConvexClientRelated(methodSymbol.ContainingType))
        {
            return;
        }

        // Get the function name
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        string? functionName = null;
        if (invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
        {
            functionName = literal.Token.ValueText;
        }

        if (string.IsNullOrEmpty(functionName))
        {
            return;
        }

        // Check if DefineQueryDependency is called for this mutation
        if (HasCacheInvalidationDefined(context, invocation, functionName!))
        {
            return;
        }

        // Suggest cache invalidation for mutations that modify data
        // This is a heuristic - mutations that create/update/delete should invalidate related queries
        if (IsDataModifyingMutation(functionName!))
        {
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX010_CacheInvalidation,
                invocation.GetLocation(),
                functionName!);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasCacheInvalidationDefined(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax mutationCall, string functionName)
    {
        // Check if DefineQueryDependency is called in the same method or class
        var containingMethod = mutationCall.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            if (ContainsDefineQueryDependency(context, containingMethod, functionName))
            {
                return true;
            }
        }

        // Check class-level initialization
        var containingClass = mutationCall.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass != null)
        {
            // Check constructor or initialization methods
            foreach (var member in containingClass.Members.OfType<MethodDeclarationSyntax>())
            {
                if (member.Identifier.ValueText is "ctor" or ".ctor" or "Initialize" or "OnInitialized")
                {
                    if (ContainsDefineQueryDependency(context, member, functionName))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ContainsDefineQueryDependency(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, string functionName)
    {
        if (method.Body == null)
        {
            return false;
        }

        foreach (var statement in method.Body.Statements)
        {
            foreach (var invocation in statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.Name == "DefineQueryDependency")
                    {
                        // Check if the first argument matches our function name
                        if (invocation.ArgumentList.Arguments.Count > 0)
                        {
                            if (invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
                            {
                                if (literal.Token.ValueText == functionName)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool IsDataModifyingMutation(string functionName)
    {
        // Heuristic: mutations that create/update/delete typically have these prefixes
        var lowerName = functionName.ToLowerInvariant();
        return lowerName.Contains("create") ||
               lowerName.Contains("update") ||
               lowerName.Contains("delete") ||
               lowerName.Contains("remove") ||
               lowerName.Contains("add") ||
               lowerName.Contains("set") ||
               lowerName.Contains("edit") ||
               lowerName.Contains("modify");
    }
}

