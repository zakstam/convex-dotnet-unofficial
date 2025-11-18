# Convex.Client.Analyzer.CodeFixes

This package provides automatic code fixes for issues detected by the Convex.Client.Analyzer Roslyn analyzers.

## Overview

The code fixes in this package automatically resolve common issues identified by the Convex analyzers, improving code quality and following best practices for Convex .NET Client usage.

## Supported Fixes

- **CVX002**: Replace `Observe()` with `CreateResilientSubscription()` or add connection state monitoring
- **CVX003**: Replace generic `Exception` with specific Convex exception types (`ConvexException`, `ConvexFunctionException`, etc.)
- **CVX004**: Replace string literals with type-safe `ConvexFunctions` constants
- **CVX006**: Implement `IDisposable` pattern and add `Dispose()` method for subscriptions
- **CVX007**: Add missing `ExecuteAsync()` calls to builder chains
- **CVX010**: Add `DefineQueryDependency()` calls for cache invalidation

## Installation

This package is automatically included when you install the Convex.Client.Analyzer package.

## Usage

When the analyzer detects an issue, you'll see a lightbulb icon (💡) in your IDE. Click it or press `Ctrl+.` (or `Cmd+.` on Mac) to see available code fixes.

**Example**:
```csharp
// Before (with CVX003 warning)
catch (Exception ex) { }

// After applying code fix
catch (ConvexException ex) { }
```

## Requirements

- .NET Standard 2.0 or higher
- Visual Studio 2019+ or compatible IDE with Roslyn analyzer support
