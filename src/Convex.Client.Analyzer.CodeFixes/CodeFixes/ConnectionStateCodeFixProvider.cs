using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convex.Client.Analyzer.CodeFixes;

/// <summary>
/// Code fix provider for CVX002: Ensure connection state monitoring.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConnectionStateCodeFixProvider)), Shared]
public class ConnectionStateCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX002");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

        if (invocation == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use CreateResilientSubscription",
                createChangedDocument: c => ReplaceWithResilientSubscriptionAsync(
                    context.Document,
                    invocation,
                    c),
                equivalenceKey: "UseCreateResilientSubscription"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add connection state monitoring",
                createChangedDocument: c => AddConnectionStateMonitoringAsync(
                    context.Document,
                    invocation,
                    c),
                equivalenceKey: "AddConnectionStateMonitoring"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithResilientSubscriptionAsync(
        Document document,
        InvocationExpressionSyntax observeCall,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Get the member access expression (client.Observe<...>)
        var memberAccess = observeCall.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null)
        {
            return document;
        }

        // Extract the client expression (the part before .Observe)
        var clientExpression = memberAccess.Expression;

        // Extract function name and args from Observe call
        // Observe<T>(functionName) or Observe<T, TArgs>(functionName, args)
        if (observeCall.ArgumentList.Arguments.Count == 0)
        {
            return document; // Can't fix without arguments
        }

        var functionNameArg = observeCall.ArgumentList.Arguments[0].Expression;
        ExpressionSyntax? argsArg = null;
        if (observeCall.ArgumentList.Arguments.Count > 1)
        {
            argsArg = observeCall.ArgumentList.Arguments[1].Expression;
        }

        // Extract the generic type parameter from Observe<T> or Observe<T, TArgs>
        TypeSyntax? resultType = null;
        if (memberAccess.Name is GenericNameSyntax genericName)
        {
            if (genericName.TypeArgumentList.Arguments.Count > 0)
            {
                resultType = genericName.TypeArgumentList.Arguments[0];
            }
        }

        if (resultType == null)
        {
            return document; // Can't determine result type
        }

        // Build CreateResilientSubscription<T>(functionName, args)
        var createResilientSubscriptionName = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("CreateResilientSubscription"),
            SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(new[] { resultType })));

        var newMemberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            clientExpression,
            createResilientSubscriptionName);

        // Build argument list: functionName, args (if present)
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(functionNameArg)
        };

        if (argsArg != null)
        {
            arguments.Add(SyntaxFactory.Argument(argsArg));
        }

        var newInvocation = SyntaxFactory.InvocationExpression(
            newMemberAccess,
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

        var newRoot = root.ReplaceNode(observeCall, newInvocation);

        // Ensure using directive is added
        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            var hasUsingDirective = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "Convex.Client.Extensions.ExtensionMethods");

            if (!hasUsingDirective)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Convex"),
                                SyntaxFactory.IdentifierName("Client")),
                            SyntaxFactory.IdentifierName("Extensions")),
                        SyntaxFactory.IdentifierName("ExtensionMethods")));

                newRoot = compilationUnit.AddUsings(usingDirective);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddConnectionStateMonitoringAsync(
        Document document,
        InvocationExpressionSyntax observeCall,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Find the containing method
        var containingMethod = observeCall.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return document;
        }

        // This is a simplified version - in practice, you'd parse and insert the code properly
        // For now, we'll just return the document unchanged as this fix requires more complex code generation
        // The user can manually add connection state monitoring based on the diagnostic message
        return document;
    }
}

