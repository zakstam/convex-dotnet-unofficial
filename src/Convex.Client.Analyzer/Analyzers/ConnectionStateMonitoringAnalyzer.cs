using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX002: Ensure connection state monitoring for real-time features.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConnectionStateMonitoringAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX002_EnsureConnectionStateMonitoring);

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

        // Check if this is an Observe() call
        if (methodSymbol.Name != "Observe" || !ConvexAnalyzerHelpers.IsConvexClientRelated(methodSymbol.ContainingType))
        {
            return;
        }

        // Get the function name if available
        var functionName = "unknown";
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            if (invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
            {
                functionName = literal.Token.ValueText ?? "unknown";
            }
        }

        // Check if connection state is being monitored in the containing method/class
        if (HasConnectionStateMonitoring(context, invocation))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            ConvexDiagnosticDescriptors.CVX002_EnsureConnectionStateMonitoring,
            invocation.GetLocation(),
            functionName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasConnectionStateMonitoring(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax observeCall)
    {
        // Check if the observe call is part of CreateResilientSubscription
        var parent = observeCall.Parent;
        while (parent != null)
        {
            if (parent is InvocationExpressionSyntax parentInvocation)
            {
                if (context.SemanticModel.GetSymbolInfo(parentInvocation).Symbol is IMethodSymbol parentMethod)
                {
                    if (parentMethod.Name == "CreateResilientSubscription")
                    {
                        return true;
                    }
                }
            }
            parent = parent.Parent;
        }

        // Check if ConnectionStateChanges is subscribed to in the containing method or class
        var containingMethod = observeCall.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            var methodBody = containingMethod.Body ?? containingMethod.ExpressionBody?.Expression as SyntaxNode;
            if (methodBody != null && ContainsConnectionStateSubscription(context, methodBody))
            {
                return true;
            }
        }

        // Check class-level fields/properties for connection state subscriptions
        var containingClass = observeCall.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass != null)
        {
            foreach (var member in containingClass.Members)
            {
                if (member is FieldDeclarationSyntax field)
                {
                    // Check if field is related to connection state
                    var fieldType = context.SemanticModel.GetTypeInfo(field.Declaration.Type).Type;
                    if (fieldType != null && fieldType.Name.Contains("ConnectionState"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ContainsConnectionStateSubscription(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        foreach (var descendant in node.DescendantNodes())
        {
            if (descendant is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.ValueText == "ConnectionStateChanges")
                {
                    return true;
                }
            }
        }
        return false;
    }
}

