using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX003: Avoid generic Exception types in Convex operations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GenericExceptionAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX003_AvoidGenericExceptionTypes);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        var catchClause = (CatchClauseSyntax)context.Node;

        // Check if this is a generic Exception catch
        if (catchClause.Declaration == null)
        {
            // catch without type declaration - this is catch (Exception) implicitly
            return;
        }

        var exceptionType = catchClause.Declaration.Type;
        var typeInfo = context.SemanticModel.GetTypeInfo(exceptionType);
        var exceptionTypeSymbol = typeInfo.Type as INamedTypeSymbol;

        // Check if it's catching generic Exception
        if (exceptionTypeSymbol == null)
        {
            return;
        }

        // Check if it's System.Exception
        var isSystemException = exceptionTypeSymbol.Name == "Exception" &&
                                exceptionTypeSymbol.ContainingNamespace?.ToDisplayString() == "System" &&
                                exceptionTypeSymbol.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace == true;
        
        if (!isSystemException)
        {
            return;
        }

        // Skip library code - internal implementations handle errors themselves
        var containingMethod = catchClause.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            var methodSymbolInfo = context.SemanticModel.GetDeclaredSymbol(containingMethod);
            if (methodSymbolInfo?.ContainingNamespace?.ToDisplayString()?.StartsWith("Convex.Client") == true)
            {
                return;
            }
        }

        // Check if the try block contains Convex operations
        var tryStatement = catchClause.Parent as TryStatementSyntax;
        if (tryStatement == null || !ContainsConvexOperations(context, tryStatement.Block))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            ConvexDiagnosticDescriptors.CVX003_AvoidGenericExceptionTypes,
            catchClause.Declaration.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool ContainsConvexOperations(SyntaxNodeAnalysisContext context, BlockSyntax? block)
    {
        if (block == null)
        {
            return false;
        }

        foreach (var statement in block.Statements)
        {
            if (ContainsConvexOperationsInNode(context, statement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsConvexOperationsInNode(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        foreach (var descendant in node.DescendantNodes())
        {
            if (descendant is InvocationExpressionSyntax invocation)
            {
                if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
                {
                    if (ConvexAnalyzerHelpers.IsConvexOperationMethod(methodSymbol))
                    {
                        return true;
                    }

                    // Check for ExecuteAsync on builders
                    if (methodSymbol.Name == "ExecuteAsync" && ConvexAnalyzerHelpers.IsBuilderType(methodSymbol.ContainingType))
                    {
                        return true;
                    }

                    // Check for Subscribe on observables
                    if (methodSymbol.Name == "Subscribe")
                    {
                        var containingType = methodSymbol.ContainingType;
                        if (containingType != null && containingType.AllInterfaces.Any(i => i.Name == "IObservable"))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}

