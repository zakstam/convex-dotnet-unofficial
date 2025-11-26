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
/// Code fix provider for CVX003: Avoid generic Exception types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenericExceptionCodeFixProvider)), Shared]
public class GenericExceptionCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX003");

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

        var catchClause = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();

        if (catchClause?.Declaration == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with ConvexException",
                createChangedDocument: c => ReplaceWithConvexExceptionAsync(
                    context.Document,
                    catchClause,
                    c),
                equivalenceKey: "ReplaceWithConvexException"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with specific Convex exceptions",
                createChangedDocument: c => ReplaceWithSpecificExceptionsAsync(
                    context.Document,
                    catchClause,
                    c),
                equivalenceKey: "ReplaceWithSpecificExceptions"),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithConvexExceptionAsync(
        Document document,
        CatchClauseSyntax catchClause,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var newType = SyntaxFactory.IdentifierName("ConvexException");
        var newDeclaration = SyntaxFactory.CatchDeclaration(newType);
        var newCatchClause = catchClause.WithDeclaration(newDeclaration);

        var newRoot = root.ReplaceNode(catchClause, newCatchClause);

        // Ensure using directive is added
        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            var hasUsingDirective = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "Convex.Client.Infrastructure.ErrorHandling");

            if (!hasUsingDirective)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Convex"),
                                SyntaxFactory.IdentifierName("Client")),
                            SyntaxFactory.IdentifierName("Shared")),
                        SyntaxFactory.IdentifierName("ErrorHandling")));

                newRoot = compilationUnit.AddUsings(usingDirective);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ReplaceWithSpecificExceptionsAsync(
        Document document,
        CatchClauseSyntax catchClause,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Replace with multiple catch blocks for specific exceptions
        var catchBlocks = new[]
        {
            SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("ConvexFunctionException"), SyntaxFactory.Identifier("ex")),
                null,
                catchClause.Block),
            SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("ConvexNetworkException"), SyntaxFactory.Identifier("ex")),
                null,
                catchClause.Block),
            SyntaxFactory.CatchClause(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("ConvexException"), SyntaxFactory.Identifier("ex")),
                null,
                catchClause.Block)
        };

        // Find the try statement
        var tryStatement = catchClause.Parent as TryStatementSyntax;
        if (tryStatement != null)
        {
            var newTryStatement = tryStatement.WithCatches(SyntaxFactory.List(catchBlocks));
            var newRoot = root.ReplaceNode(tryStatement, newTryStatement);

            // Ensure using directive is added
            if (newRoot is CompilationUnitSyntax compilationUnit)
            {
                var hasUsingDirective = compilationUnit.Usings.Any(u =>
                    u.Name?.ToString() == "Convex.Client.Infrastructure.ErrorHandling");

                if (!hasUsingDirective)
                {
                    var usingDirective = SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.QualifiedName(
                                    SyntaxFactory.IdentifierName("Convex"),
                                    SyntaxFactory.IdentifierName("Client")),
                                SyntaxFactory.IdentifierName("Shared")),
                            SyntaxFactory.IdentifierName("ErrorHandling")));

                    newRoot = compilationUnit.AddUsings(usingDirective);
                }
            }

            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }
}

