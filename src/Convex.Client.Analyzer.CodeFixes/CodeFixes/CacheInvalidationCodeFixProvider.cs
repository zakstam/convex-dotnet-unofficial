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
/// Code fix provider for CVX010: Cache invalidation.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CacheInvalidationCodeFixProvider)), Shared]
public class CacheInvalidationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX010");

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

        // Extract function name from diagnostic message
        var message = diagnostic.GetMessage();
        var functionName = ExtractFunctionName(message);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add DefineQueryDependency call",
                createChangedDocument: c => AddDefineQueryDependencyAsync(
                    context.Document,
                    invocation,
                    functionName,
                    c),
                equivalenceKey: "AddDefineQueryDependency"),
            diagnostic);
    }

    private static string ExtractFunctionName(string message)
    {
        // Message format: "Mutation '{0}' may need to invalidate..."
        var startIndex = message.IndexOf("'");
        if (startIndex >= 0)
        {
            var endIndex = message.IndexOf("'", startIndex + 1);
            if (endIndex > startIndex)
            {
                return message.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
        }
        return "mutation";
    }

    private static async Task<Document> AddDefineQueryDependencyAsync(
        Document document,
        InvocationExpressionSyntax mutationCall,
        string functionName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Find the containing method
        var containingMethod = mutationCall.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod?.Body == null)
        {
            return document;
        }

        // Get the client variable name
        var clientName = GetClientVariableName(mutationCall);

        // Create DefineQueryDependency call
        var defineDependencyCall = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(clientName),
                    SyntaxFactory.IdentifierName("DefineQueryDependency")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(functionName))),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("related:query")))
                            }))));

        // Add the call after the mutation
        var statements = containingMethod.Body.Statements.ToList();
        var mutationIndex = statements.FindIndex(s => s.DescendantNodes().Contains(mutationCall));
        if (mutationIndex >= 0)
        {
            statements.Insert(mutationIndex + 1, defineDependencyCall);
            var newBody = containingMethod.Body.WithStatements(SyntaxFactory.List(statements));
            var newMethod = containingMethod.WithBody(newBody);
            var newRoot = root.ReplaceNode(containingMethod, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }

    private static string GetClientVariableName(InvocationExpressionSyntax invocation)
    {
        // Try to find the client variable from the invocation
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText;
            }
        }
        return "client";
    }
}

