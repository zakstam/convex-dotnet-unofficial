# Changelog

All notable changes to the Convex .NET SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.2-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.0.1-alpha...v0.0.2-alpha) (2025-11-18)


### Bug Fixes

* update CI/CD workflow to fix permissions ([0d70b53](https://github.com/zakstam/convex-dotnet-unofficial/commit/0d70b5331a9f7655a5c0cd7bc91630d3a60e2c06))

## [0.0.1-alpha] - 2025-11-18

### Added

Initial release of Convex .NET SDK with comprehensive features:

#### Core Client (`Convex.Client`)
- Real-time client with WebSocket support for live data subscriptions
- Query, Mutation, and Action execution
- Authentication support with JWT token management
- File storage operations (upload/download)
- Vector search capabilities
- Pagination support for large datasets
- Caching layer for query results
- Health monitoring and diagnostics
- Resilience patterns (retry, circuit breaker)
- Scheduling support for delayed operations
- HTTP actions support
- Vertical slice architecture for maintainability

#### Source Generators (`Convex.FunctionGenerator`)
- C# to TypeScript transpiler for Convex functions
- Automatic schema generation from C# models
- Function code generation with type-safe extension methods

#### Analyzers (`Convex.Client.Analyzer`, `Convex.Client.Analyzer.CodeFixes`)
- Roslyn analyzers for Convex client usage patterns
- Code fixes for common issues
- Performance optimization suggestions
- Security validation rules

#### Extensions (`Convex.Client.Extensions`)
- Reactive Extensions (Rx) patterns and helpers
- Testing utilities and mocks
- Fluent argument builders
- Result wrappers and error handling helpers
- Pagination helpers
- Batch operations support

#### Blazor Extensions (`Convex.Client.Extensions.Blazor`)
- Blazor WebAssembly and Server support
- StateHasChanged integration
- Form binding helpers
- Async enumerable support

#### ASP.NET Core Extensions (`Convex.Client.Extensions.AspNetCore`)
- Middleware for authentication token handling
- Health check integration
- Dependency injection support

#### Clerk Authentication Extensions
- `Convex.Client.Extensions.Clerk` - Core Clerk integration
- `Convex.Client.Extensions.Clerk.Blazor` - Blazor-specific Clerk support
- `Convex.Client.Extensions.Clerk.Godot` - Godot desktop app Clerk support
- OAuth 2.0 Authorization Code Flow with PKCE
- Token caching and management

#### Dependency Injection (`Convex.Client.Extensions.DependencyInjection`)
- Simplified client configuration
- Service registration helpers

### Platform Support
- .NET Standard 2.1
- .NET 8.0
- .NET 9.0
- Unity 2021.3+ (via .NET Standard 2.1)
- Godot 4.0+ (via .NET Standard 2.1)
- Xamarin / MAUI (via .NET Standard 2.1)
- WPF / WinForms
- ASP.NET Core 6.0+
- Blazor (WebAssembly and Server)
- Console Applications

---

[0.0.1-alpha]: https://github.com/zakstam/convex-dotnet-unofficial/releases/tag/v0.0.1-alpha
