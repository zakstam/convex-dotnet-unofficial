using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer.Analyzers;

/// <summary>
/// Analyzer for CVX004: Use type-safe function name constants.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FunctionNameAnalyzer : ConvexAnalyzerBase
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConvexDiagnosticDescriptors.CVX004_UseTypeSafeFunctionNames);

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

        // Check if this is a Convex client method that takes a function name
        if (!IsConvexFunctionMethod(methodSymbol))
        {
            return;
        }

        // Get the first argument (function name)
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;

        // Check if it's a string literal
        if (firstArg is not LiteralExpressionSyntax literalExpression ||
            !literalExpression.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return;
        }

        var functionPath = literalExpression.Token.ValueText;

        // Check if it's a Convex function path (starts with "functions/")
        if (!functionPath.StartsWith("functions/"))
        {
            return;
        }

        // Parse the function name and infer the type
        var functionName = functionPath.Substring("functions/".Length);
        var (constantClass, constantName) = InferFunctionConstant(functionName, methodSymbol.Name);

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            ConvexDiagnosticDescriptors.CVX004_UseTypeSafeFunctionNames,
            literalExpression.GetLocation(),
            constantClass,
            constantName,
            functionPath);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsConvexFunctionMethod(IMethodSymbol methodSymbol)
    {
        // Check if the method is one of the Convex client fluent API methods
        var methodName = methodSymbol.Name;
        if (methodName != "Query" &&
            methodName != "Mutate" &&
            methodName != "Action" &&
            methodName != "Observe" &&
            methodName != "WatchQuery")
        {
            return false;
        }

        // Check if the containing type is related to IConvexClient
        var containingType = methodSymbol.ContainingType;
        return ConvexAnalyzerHelpers.IsConvexClientRelated(containingType);
    }

    private static (string ConstantClass, string ConstantName) InferFunctionConstant(
        string functionName,
        string methodName)
    {
        // Infer the constant class from the method name
        var constantClass = methodName switch
        {
            "Query" or "WatchQuery" or "Observe" => "Queries",
            "Mutate" => "Mutations",
            "Action" => "Actions",
            _ => "Queries" // Default to Queries
        };

        // Convert function name to PascalCase
        var constantName = ToPascalCase(functionName);

        return (constantClass, constantName);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle camelCase: getMessages -> GetMessages
        if (char.IsLower(input[0]))
        {
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        return input;
    }
}
