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
/// Code fix provider for CVX007: Builder pattern issues.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BuilderPatternCodeFixProvider)), Shared]
public class BuilderPatternCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX007");

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

        var message = diagnostic.GetMessage();
        if (message.Contains("ExecuteAsync"))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add ExecuteAsync() call",
                    createChangedDocument: c => AddExecuteAsyncAsync(
                        context.Document,
                        invocation,
                        c),
                    equivalenceKey: "AddExecuteAsync"),
                diagnostic);
        }
    }

    private static async Task<Document> AddExecuteAsyncAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Find the builder chain and add ExecuteAsync()
        var executeAsyncCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocation,
                SyntaxFactory.IdentifierName("ExecuteAsync")));

        var newRoot = root.ReplaceNode(invocation, executeAsyncCall);
        return document.WithSyntaxRoot(newRoot);
    }
}

