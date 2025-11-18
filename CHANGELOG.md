# Changelog

All notable changes to the Convex .NET SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.2-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.0.1-alpha...v0.0.2-alpha) (2025-11-18)


### Bug Fixes

* update CI/CD workflow to fix permissions ([0d70b53](https://github.com/zakstam/convex-dotnet-unofficial/commit/0d70b5331a9f7655a5c0cd7bc91630d3a60e2c06))

## [0.0.7-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.6-alpha...v0.0.7-alpha) (2025-11-18)


### Bug Fixes

* update CI/CD workflow and release notes for clarity ([6a9c68a](https://github.com/zakstam/convex-dotnet/commit/6a9c68a43a05859831b20b59023a8fa0a7bc2bee))

## [0.0.6-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.5-alpha...v0.0.6-alpha) (2025-11-18)


### Bug Fixes

* use PAT for release-please to trigger workflows ([6393940](https://github.com/zakstam/convex-dotnet/commit/63939406dfec510ff3a08ace5e2310230390ff28))

## [0.0.5-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.4-alpha...v0.0.5-alpha) (2025-11-18)


### Bug Fixes

* add more release event types to CI/CD workflow trigger ([7c9ba07](https://github.com/zakstam/convex-dotnet/commit/7c9ba07210fd82ecfea1aa0ac9cb4b5ccf96044f))

## [0.0.4-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.3-alpha...v0.0.4-alpha) (2025-11-18)


### Bug Fixes

* trigger release-please workflow ([467b8d1](https://github.com/zakstam/convex-dotnet/commit/467b8d1bbf5863fd9b5213919bf8813ae6cdc92d))

## [0.0.3-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.2-alpha...v0.0.3-alpha) (2025-11-18)


### Bug Fixes

* trigger release-please workflow ([b461090](https://github.com/zakstam/convex-dotnet/commit/b461090155629963287f4957d1c32136c9c1cc37))

## [0.0.2-alpha](https://github.com/zakstam/convex-dotnet/compare/v0.0.1-alpha...v0.0.2-alpha) (2025-11-18)


### Bug Fixes

* trigger release-please workflow ([1d53baf](https://github.com/zakstam/convex-dotnet/commit/1d53baf8b1e115c6043e82461d162c13589e543b))
* trigger release-please workflow ([a619fff](https://github.com/zakstam/convex-dotnet/commit/a619fffbc3f9df03453747ffa13623d5a9ab394e))

## [0.3.5](https://github.com/zakstam/convex-dotnet/compare/v0.3.4...v0.3.5) (2025-11-17)


### 🐛 Bug Fixes

* exclude examples from package build and fix PR title template ([706049e](https://github.com/zakstam/convex-dotnet/commit/706049ea14655325856fd9874726e3e8d90d8469))
* template for PR title version ([11ea088](https://github.com/zakstam/convex-dotnet/commit/11ea0882826eaa51da14bd831771ec7343d2f2f6))

## [0.3.4](https://github.com/zakstam/convex-dotnet/compare/v0.3.3...v0.3.4) (2025-11-17)


### 🐛 Bug Fixes

* handle template package NU5017 error in build workflow ([d2c1d7a](https://github.com/zakstam/convex-dotnet/commit/d2c1d7ab042f2f07a2752fcf7b9ed60d4adac677))

## [0.3.3](https://github.com/zakstam/convex-dotnet/compare/v0.3.2...v0.3.3) (2025-11-17)


### 🐛 Bug Fixes

* remove nuget.config from git ([ca1fd92](https://github.com/zakstam/convex-dotnet/commit/ca1fd92737a5788ac06f59bfac064647e90b7000))

## [0.3.2](https://github.com/zakstam/convex-dotnet/compare/v0.3.1...v0.3.2) (2025-11-17)


### ♻️ Code Refactoring

* mark bundled analyzers as non-packable ([979105b](https://github.com/zakstam/convex-dotnet/commit/979105b99a1f8832e18f0b5b96364378fd020a1e))

## [0.3.1](https://github.com/zakstam/convex-dotnet/compare/v0.3.0...v0.3.1) (2025-11-17)


### 🐛 Bug Fixes

* resolve package build errors in CI workflow ([2f5cebf](https://github.com/zakstam/convex-dotnet/commit/2f5cebff3e7507090cd11ccf516f95892aa03c26))

## [0.3.0](https://github.com/zakstam/convex-dotnet/compare/v0.2.0...v0.3.0) (2025-11-17)


### ✨ Features

* implement automated release system ([591198c](https://github.com/zakstam/convex-dotnet/commit/591198c28e68af0dabca3347c6d1646e0d1dee39))
* new release flow ([6d147e1](https://github.com/zakstam/convex-dotnet/commit/6d147e12c400d4ad8debf637cf4d4db1a02c6ab4))

## [0.0.1] - 2025-01-XX

### Added

#### Core Client (`Convex.Client`)
- Initial release of Convex .NET SDK
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

### Known Limitations

#### Transpiler Limitations
- LINQ methods not supported - use explicit loops instead
- Lambda expressions not supported - use named methods
- Async/await in function bodies not supported - signatures only
- Null-conditional operators (`?.`) not supported - use explicit checks

#### Pagination Slice
- Live subscriptions with pagination implemented via `PaginatedSubscription`
- Split cursor support infrastructure exists but not yet utilized
- Automatic page size adjustment based on PageStatus not yet implemented

#### Mutations Slice
- Architecture refactoring completed - uses Shared infrastructure
- All dependencies migrated to Shared interfaces

#### Clerk Godot Extension
- Tokens not persisted to disk (user must re-authenticate on app restart)
- Clerk domain must be manually configured
- No token refresh flow (tokens expire after cache duration)
- Uses system browser only (no in-app browser)

#### Other Limitations
- Caching: No TTL expiration, no size limits/LRU eviction, no persistence
- Resilience: No distributed circuit breaker, no metrics/events
- Health: No metric persistence, fixed sample windows
- Diagnostics: No metric persistence, fixed disconnection history size
- Authentication: No automatic token refresh, no token expiration detection

### Documentation
- Comprehensive README with getting started guide
- API documentation via XML comments
- Example projects (RealTimeChat, RealTimeChatClerk)
- Slice-specific README files documenting architecture

### Examples
- RealTimeChat - Full-featured real-time chat application
  - Blazor WebAssembly frontend
  - WPF desktop application
  - Godot game engine integration
- RealTimeChatClerk - Same chat application with Clerk authentication

---

[0.0.1]: https://github.com/zakstam/convex-dotnet/releases/tag/v0.0.1
