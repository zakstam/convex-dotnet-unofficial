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
/// Code fix provider for CVX004: Use type-safe function name constants.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FunctionNameCodeFixProvider)), Shared]
public class FunctionNameCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX004");

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

        var literalExpression = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();

        if (literalExpression == null)
        {
            return;
        }

        // Extract the constant information from the diagnostic message
        // Message format: "Use 'ConvexFunctions.{0}.{1}' instead of string literal '{2}'"
        var message = diagnostic.GetMessage();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // Parse the message to get the constant class and name
        var (constantClass, constantName) = ParseDiagnosticMessage(message);
        if (string.IsNullOrEmpty(constantClass) || string.IsNullOrEmpty(constantName))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use ConvexFunctions.{constantClass}.{constantName}",
                createChangedDocument: c => ReplaceWithConstantAsync(
                    context.Document,
                    literalExpression,
                    constantClass,
                    constantName,
                    c),
                equivalenceKey: "UseTypeSafeConstant"),
            diagnostic);
    }

    private static (string ConstantClass, string ConstantName) ParseDiagnosticMessage(string message)
    {
        // Message format: "Use 'ConvexFunctions.Queries.GetMessages' instead of string literal 'functions/getMessages'"
        var startIndex = message.IndexOf("ConvexFunctions.");
        if (startIndex < 0)
        {
            return (string.Empty, string.Empty);
        }

        startIndex += "ConvexFunctions.".Length;
        var endIndex = message.IndexOf("'", startIndex);
        if (endIndex < 0)
        {
            return (string.Empty, string.Empty);
        }

        var constantPath = message.Substring(startIndex, endIndex - startIndex);
        var parts = constantPath.Split('.');
        if (parts.Length != 2)
        {
            return (string.Empty, string.Empty);
        }

        return (parts[0], parts[1]);
    }

    private static async Task<Document> ReplaceWithConstantAsync(
        Document document,
        LiteralExpressionSyntax literalExpression,
        string constantClass,
        string constantName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Create the member access expression: ConvexFunctions.Queries.GetMessages
        var constantAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("ConvexFunctions"),
                SyntaxFactory.IdentifierName(constantClass)),
            SyntaxFactory.IdentifierName(constantName));

        // Replace the literal with the constant access
        var newRoot = root.ReplaceNode(literalExpression, constantAccess);

        // Ensure the using directive is added if needed
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel != null && newRoot is CompilationUnitSyntax compilationUnit)
        {
            // Check if we need to add "using Convex.Generated;"
            var hasUsingDirective = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "Convex.Generated");

            if (!hasUsingDirective)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("Convex"),
                        SyntaxFactory.IdentifierName("Generated")));

                newRoot = compilationUnit.AddUsings(usingDirective);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
