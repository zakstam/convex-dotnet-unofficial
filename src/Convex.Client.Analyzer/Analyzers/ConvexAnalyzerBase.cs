using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Convex.Client.Analyzer;

/// <summary>
/// Base class for all Convex client analyzers.
/// </summary>
public abstract class ConvexAnalyzerBase : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the diagnostic descriptors supported by this analyzer.
    /// </summary>
    public abstract override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    /// Called once at session start to register actions in the analysis context.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public abstract override void Initialize(AnalysisContext context);
}

/// <summary>
/// Contains diagnostic descriptors for Convex client analyzers.
/// </summary>
public static class ConvexDiagnosticDescriptors
{
    /// <summary>
    /// CVX001: Avoid direct IConvexClient method calls
    /// </summary>
    public static readonly DiagnosticDescriptor CVX001_AvoidDirectClientCalls = new(
        id: "CVX001",
        title: "Avoid direct IConvexClient method calls",
        messageFormat: "Direct calls to IConvexClient methods bypass error handling, retry logic, and performance optimizations. Consider using extension methods or builder patterns.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Direct calls to IConvexClient methods bypass error handling, retry logic, and performance optimizations provided by extension methods.");

    /// <summary>
    /// CVX002: Ensure connection state monitoring for real-time features
    /// </summary>
    public static readonly DiagnosticDescriptor CVX002_EnsureConnectionStateMonitoring = new(
        id: "CVX002",
        title: "Ensure connection state monitoring for real-time features",
        messageFormat: "Real-time subscription '{0}' should monitor connection state to handle disconnections gracefully. Consider using CreateResilientSubscription or subscribing to ConnectionStateChanges.",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Real-time subscriptions can fail silently when the connection is lost. Monitor connection state to handle disconnections gracefully.");

    /// <summary>
    /// CVX003: Avoid generic Exception types in Convex operations
    /// </summary>
    public static readonly DiagnosticDescriptor CVX003_AvoidGenericExceptionTypes = new(
        id: "CVX003",
        title: "Avoid generic Exception types in Convex operations",
        messageFormat: "Catching generic Exception hides Convex-specific error information. Catch specific Convex exceptions (ConvexException, ConvexFunctionException, ConvexNetworkException, etc.) instead.",
        category: "ErrorHandling",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Generic exception handling makes error diagnosis difficult and can hide Convex-specific issues. Use specific Convex exception types for better error handling.");

    /// <summary>
    /// CVX004: Use type-safe function name constants
    /// </summary>
    public static readonly DiagnosticDescriptor CVX004_UseTypeSafeFunctionNames = new(
        id: "CVX004",
        title: "Use type-safe function name constants instead of string literals",
        messageFormat: "Use 'ConvexFunctions.{0}.{1}' instead of string literal '{2}'",
        category: "TypeSafety",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "String literals for function names are error-prone and prevent compile-time validation. Use the generated ConvexFunctions constants for type safety.");

    /// <summary>
    /// CVX005: Missing error handling in async operations
    /// </summary>
    public static readonly DiagnosticDescriptor CVX005_MissingErrorHandling = new(
        id: "CVX005",
        title: "Missing error handling in async Convex operations",
        messageFormat: "Async Convex operation '{0}' has no error handling. Consider adding try-catch or using OnError() handler.",
        category: "ErrorHandling",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Unhandled async Convex operations can hide errors. Add error handling with try-catch or OnError() handlers.");

    /// <summary>
    /// CVX006: Subscription disposal
    /// </summary>
    public static readonly DiagnosticDescriptor CVX006_SubscriptionDisposal = new(
        id: "CVX006",
        title: "Subscription should be properly disposed",
        messageFormat: "Subscription stored in field '{0}' should be disposed in Dispose() method to prevent memory leaks",
        category: "Memory",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Subscriptions that are not properly disposed can cause memory leaks. Implement IDisposable pattern for classes with subscriptions.");

    /// <summary>
    /// CVX007: Builder pattern best practices
    /// </summary>
    public static readonly DiagnosticDescriptor CVX007_BuilderPatternIssue = new(
        id: "CVX007",
        title: "Builder pattern issue detected",
        messageFormat: "{0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Builder pattern issues: missing ExecuteAsync(), invalid chaining, or missing required methods.");

    /// <summary>
    /// CVX008: Type safety for function arguments
    /// </summary>
    public static readonly DiagnosticDescriptor CVX008_ArgumentTypeSafety = new(
        id: "CVX008",
        title: "Use typed arguments instead of anonymous objects",
        messageFormat: "Consider using typed argument classes instead of anonymous objects for better type safety and IntelliSense support",
        category: "TypeSafety",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Anonymous objects for arguments reduce type safety. Use typed argument classes when available.");

    /// <summary>
    /// CVX009: Optimistic update best practices
    /// </summary>
    public static readonly DiagnosticDescriptor CVX009_OptimisticUpdateIssue = new(
        id: "CVX009",
        title: "Optimistic update best practice issue",
        messageFormat: "{0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Optimistic update issues: missing rollback handlers, optimistic updates on queries, or missing optimistic updates on mutations.");

    /// <summary>
    /// CVX010: Cache invalidation patterns
    /// </summary>
    public static readonly DiagnosticDescriptor CVX010_CacheInvalidation = new(
        id: "CVX010",
        title: "Consider defining cache invalidation dependencies",
        messageFormat: "Mutation '{0}' may need to invalidate related queries. Consider calling DefineQueryDependency() to set up automatic cache invalidation.",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Mutations that modify data should invalidate related queries to ensure cache consistency.");
}

/// <summary>
/// Shared utilities for Convex analyzers.
/// </summary>
public static class ConvexAnalyzerHelpers
{
    /// <summary>
    /// Checks if a type symbol is related to IConvexClient.
    /// </summary>
    public static bool IsConvexClientRelated(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();

        // Check if it's in the Convex.Client namespace
        if (namespaceName?.StartsWith("Convex.Client") == true)
        {
            return true;
        }

        // Check if it implements IConvexClient
        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            if (interfaceType.Name == "IConvexClient" &&
                interfaceType.ContainingNamespace?.ToDisplayString() == "Convex.Client")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a method symbol is a Convex operation method.
    /// </summary>
    public static bool IsConvexOperationMethod(IMethodSymbol methodSymbol)
    {
        var methodName = methodSymbol.Name;
        return methodName is "Query" or "Mutate" or "Action" or "Observe" or "WatchQuery" or "ExecuteAsync" or "Subscribe"
            && IsConvexClientRelated(methodSymbol.ContainingType);
    }

    /// <summary>
    /// Checks if a type symbol is a Convex exception type.
    /// </summary>
    public static bool IsConvexExceptionType(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (namespaceName != "Convex.Client.Shared.ErrorHandling")
        {
            return false;
        }

        return typeSymbol.Name is "ConvexException" or "ConvexFunctionException" or "ConvexNetworkException" 
            or "ConvexAuthenticationException" or "ConvexRateLimitException" or "ConvexCircuitBreakerException"
            or "ConvexArgumentException";
    }

    /// <summary>
    /// Checks if a type symbol is a builder type (IQueryBuilder, IMutationBuilder, IActionBuilder).
    /// </summary>
    public static bool IsBuilderType(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        
        // Check if it's a builder interface in Shared.Builders namespace
        if (namespaceName == "Convex.Client.Shared.Builders" &&
            typeSymbol.Name is "IQueryBuilder" or "IMutationBuilder" or "IActionBuilder")
        {
            return true;
        }

        // Check if it's a concrete builder implementation in Slices namespace
        if (namespaceName != "Convex.Client.Slices.Queries" &&
            namespaceName != "Convex.Client.Slices.Mutations" &&
            namespaceName != "Convex.Client.Slices.Actions")
        {
            // Not in Slices namespace, but might implement builder interfaces
            // Continue to check interfaces below
        }
        else
        {
            // In Slices namespace - check if it's a builder type
            if (typeSymbol.Name is "IQueryBuilder" or "IMutationBuilder" or "IActionBuilder")
            {
                return true;
            }

            // Also check if the type name itself suggests it's a builder
            if (typeSymbol.Name.Contains("Builder"))
            {
                return true;
            }
        }

        // Check if the type implements builder interfaces from either Slices or Shared.Builders
        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            // Check for both non-generic and generic interfaces (IQueryBuilder, IQueryBuilder<T>)
            var interfaceName = interfaceType.Name;
            if (interfaceName == "IQueryBuilder" || interfaceName == "IMutationBuilder" || interfaceName == "IActionBuilder")
            {
                var interfaceNamespace = interfaceType.ContainingNamespace?.ToDisplayString();
                if (interfaceNamespace?.StartsWith("Convex.Client.Slices") == true ||
                    interfaceNamespace?.StartsWith("Convex.Client.Shared.Builders") == true)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
