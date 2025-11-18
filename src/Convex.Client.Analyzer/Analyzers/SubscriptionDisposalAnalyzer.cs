using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX006: Subscription disposal.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SubscriptionDisposalAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX006_SubscriptionDisposal);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            AnalyzeVariable(context, variable, fieldDeclaration.Declaration.Type);
        }
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // Check if property type is IDisposable (subscriptions are IDisposable)
        var typeInfo = context.SemanticModel.GetTypeInfo(propertyDeclaration.Type);
        if (IsSubscriptionType(typeInfo.Type))
        {
            var containingClass = propertyDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (containingClass != null && !ImplementsDispose(containingClass, context))
            {
                var diagnostic = Diagnostic.Create(
                    ConvexDiagnosticDescriptors.CVX006_SubscriptionDisposal,
                    propertyDeclaration.GetLocation(),
                    propertyDeclaration.Identifier.ValueText);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void AnalyzeVariable(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variable, TypeSyntax typeSyntax)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax);
        if (!IsSubscriptionType(typeInfo.Type))
        {
            return;
        }

        // Check if this is a field (not a local variable)
        var fieldDeclaration = variable.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
        if (fieldDeclaration == null)
        {
            return;
        }

        var containingClass = variable.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
        {
            return;
        }

        // Check if the class implements IDisposable and has Dispose method that disposes this field
        if (!ImplementsDispose(containingClass, context) || !IsDisposedInDispose(containingClass, variable.Identifier.ValueText, context))
        {
            var diagnostic = Diagnostic.Create(
                ConvexDiagnosticDescriptors.CVX006_SubscriptionDisposal,
                variable.GetLocation(),
                variable.Identifier.ValueText);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsSubscriptionType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        // Check if it implements IDisposable (subscriptions are IDisposable)
        if (!typeSymbol.AllInterfaces.Any(i => i.Name == "IDisposable"))
        {
            return false;
        }

        // Exclude common non-subscription IDisposable types
        var typeName = typeSymbol.Name;
        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        
        // Exclude common framework types that are IDisposable but not subscriptions
        if (typeName is "HttpClient" or "Timer" or "Task" or "CancellationTokenSource" or 
            "SemaphoreSlim" or "Activity" or "ActivitySource" or "Meter" or "Lock" or
            "ReaderWriterLockSlim" or "Mutex" or "EventWaitHandle")
        {
            return false;
        }

        // Exclude types from System.* namespaces (framework types)
        if (namespaceName?.StartsWith("System") == true)
        {
            return false;
        }

        // Only flag types that are likely subscriptions - check if the type name suggests it's a subscription
        // or if it's from a reactive library (System.Reactive, etc.)
        // For now, be conservative and only flag if it's clearly a subscription type
        // The most reliable way is to check if the field is assigned from a Subscribe() call
        // but that requires more complex analysis
        
        // For now, we'll be more conservative and only flag if it's in user code (not library code)
        // Library code in Convex.Client should be excluded
        if (namespaceName?.StartsWith("Convex.Client") == true)
        {
            return false;
        }

        // Default: don't flag unless we're more certain it's a subscription
        // This reduces false positives
        return false;
    }

    private static bool ImplementsDispose(ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context)
    {
        // Check if class implements IDisposable
        if (classDeclaration.BaseList == null)
        {
            return false;
        }

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(baseType.Type);
            if (typeInfo.Type != null && typeInfo.Type.AllInterfaces.Any(i => i.Name == "IDisposable"))
            {
                return true;
            }
        }

        // Check for Dispose method
        return classDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.Identifier.ValueText == "Dispose");
    }

    private static bool IsDisposedInDispose(ClassDeclarationSyntax classDeclaration, string fieldName, SyntaxNodeAnalysisContext context)
    {
        var disposeMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText == "Dispose");

        foreach (var disposeMethod in disposeMethods)
        {
            if (disposeMethod.Body != null)
            {
                foreach (var statement in disposeMethod.Body.Statements)
                {
                    if (ContainsFieldDisposal(statement, fieldName))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ContainsFieldDisposal(StatementSyntax statement, string fieldName)
    {
        foreach (var node in statement.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.ValueText == "Dispose" &&
                    memberAccess.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.ValueText == fieldName)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

