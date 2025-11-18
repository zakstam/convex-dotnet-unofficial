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
/// Code fix provider for CVX006: Subscription disposal.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SubscriptionDisposalCodeFixProvider)), Shared]
public class SubscriptionDisposalCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("CVX006");

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

        var fieldOrProperty = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf()
            .OfType<MemberDeclarationSyntax>()
            .FirstOrDefault();

        if (fieldOrProperty == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Implement IDisposable and add Dispose method",
                createChangedDocument: c => ImplementDisposeAsync(
                    context.Document,
                    fieldOrProperty,
                    c),
                equivalenceKey: "ImplementDispose"),
            diagnostic);
    }

    private static async Task<Document> ImplementDisposeAsync(
        Document document,
        MemberDeclarationSyntax member,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var containingClass = member.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
        {
            return document;
        }

        // Get field/property name
        string? fieldName = null;
        if (member is FieldDeclarationSyntax field)
        {
            fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText;
        }
        else if (member is PropertyDeclarationSyntax property)
        {
            fieldName = property.Identifier.ValueText;
        }

        if (string.IsNullOrEmpty(fieldName))
        {
            return document;
        }

        // Check if class already implements IDisposable
        var implementsDisposable = containingClass.BaseList?.Types
            .Any(t => t.Type.ToString() == "IDisposable") == true;

        var newClass = containingClass;

        // Add IDisposable to base list if not present
        if (!implementsDisposable)
        {
            var baseList = containingClass.BaseList ?? SyntaxFactory.BaseList();
            var disposableType = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("IDisposable"));
            var newBaseList = baseList.AddTypes(disposableType);
            newClass = containingClass.WithBaseList(newBaseList);
        }

        // Check if Dispose method exists
        var hasDispose = newClass.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.Identifier.ValueText == "Dispose");

        if (!hasDispose)
        {
            // Create Dispose method
            var disposeMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "Dispose")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(fieldName!),
                                SyntaxFactory.IdentifierName("Dispose"))))));

            newClass = newClass.AddMembers(disposeMethod);
        }
        else
        {
            // Add disposal to existing Dispose method
            var disposeMethod = newClass.Members.OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.ValueText == "Dispose");

            if (disposeMethod.Body != null)
            {
                var disposalStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(fieldName!),
                            SyntaxFactory.IdentifierName("Dispose"))));

                var newBody = disposeMethod.Body.AddStatements(disposalStatement);
                var newDisposeMethod = disposeMethod.WithBody(newBody);
                newClass = newClass.ReplaceNode(disposeMethod, newDisposeMethod);
            }
        }

        var newRoot = root.ReplaceNode(containingClass, newClass);
        return document.WithSyntaxRoot(newRoot);
    }
}

