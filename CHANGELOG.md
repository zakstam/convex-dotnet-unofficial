# Changelog

All notable changes to the Convex .NET SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.1.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v4.0.1-alpha...v4.1.0-alpha) (2025-12-03)


### Features

* add JSON converters for DateTimeOffset and nullable DateTimeOffset, and update schema generation for timestamp fields ([de746f1](https://github.com/zakstam/convex-dotnet-unofficial/commit/de746f17f6791d618a14112b1141179e688e2b09))


### Bug Fixes

* enhance optimistic updates with subscription cache support ([289808a](https://github.com/zakstam/convex-dotnet-unofficial/commit/289808ab421f7e7414623c3ea3c51f5b219c882c))
* update dependency injection namespace and clean up build targets ([78b221e](https://github.com/zakstam/convex-dotnet-unofficial/commit/78b221ef90bdeac9ebc8bc47b55ed2b7a6ee6876))

## [4.0.1-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v4.0.0-alpha...v4.0.1-alpha) (2025-12-02)


### Bug Fixes

* update documentation to clarify TypeScript file usage and source generator setup ([b94b197](https://github.com/zakstam/convex-dotnet-unofficial/commit/b94b197ed85555dbf0414892b1a16322f0c71fcd))

## [4.0.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v3.0.0-alpha...v4.0.0-alpha) (2025-12-02)


### ⚠ BREAKING CHANGES

* Package structure has been completely reorganized.
* unified function generator and source generator into one project
* Remove old *Slice properties from ConvexClient. All features are now accessible only through IConvexClient interface.

### Features

* add argument parsing and class generation for Convex functions ([eff1aac](https://github.com/zakstam/convex-dotnet-unofficial/commit/eff1aac1c54682bfd28106626aa03a67036a3876))
* add comprehensive unit tests for Convex client components ([d2452b5](https://github.com/zakstam/convex-dotnet-unofficial/commit/d2452b5cf28ea89833bf7ab98f8ac08ef56b1306))
* consolidate packages from 8 to 3 ([4780a59](https://github.com/zakstam/convex-dotnet-unofficial/commit/4780a59cd9bf385902be5d6d6ef6edd0cce85021))
* enhance Convex.SourceGenerator with new features and diagnostics ([8ca1036](https://github.com/zakstam/convex-dotnet-unofficial/commit/8ca10363ae55358287564f0309ffa810efb4c722))
* enhance cursor and drawing game functionality with new gaming presets ([9944be4](https://github.com/zakstam/convex-dotnet-unofficial/commit/9944be4574230b282ba3cb399d4a0a5279a8aa72))
* expose all feature services through IConvexClient interface ([ebec805](https://github.com/zakstam/convex-dotnet-unofficial/commit/ebec8050883004dabfbaf4314e29e01db1ef74c4))
* unified function generator and source generator into one project ([159d6c2](https://github.com/zakstam/convex-dotnet-unofficial/commit/159d6c24aae287c9103947d0b286b0f02446df81))


### Bug Fixes

* change async methods to synchronous in CacheTests ([c82d19b](https://github.com/zakstam/convex-dotnet-unofficial/commit/c82d19b642751e54b03dd85f95c51f68ce504071))
* update TimestampManager to set request content type correctly ([41dd74c](https://github.com/zakstam/convex-dotnet-unofficial/commit/41dd74cf89d7f034f5bbe94bd2822dd739fdba33))

## [3.0.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v2.0.0-alpha...v3.0.0-alpha) (2025-11-29)


### ⚠ BREAKING CHANGES

* simplified the paginator API

### Features

* simplified the paginator API ([8f9e011](https://github.com/zakstam/convex-dotnet-unofficial/commit/8f9e01163238f5f30a93c77ff09dc4a980d72f2b))


### Bug Fixes

* deadlock CreateResilientSubscription was waiting for Connected state before calling Observe(), but Observe() is what triggers the connection. Now it calls Observe() directly, which handles connection internally. ([3e74d9a](https://github.com/zakstam/convex-dotnet-unofficial/commit/3e74d9a51a3af06b7d2ea77cf096cf537cc47618))
* update QueryBuilder_ExecuteAsync test to return null for null deserialized result instead of throwing an exception ([9faa4c1](https://github.com/zakstam/convex-dotnet-unofficial/commit/9faa4c180e6f3475f331b8bee20e6c1382d76811))

## [2.0.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v1.0.0-alpha...v2.0.0-alpha) (2025-11-28)


### ⚠ BREAKING CHANGES

* resolve race condition in WebSocket subscription reconnection

### Features

* add Convex.SchemaGenerator project to solution ([94d6a8d](https://github.com/zakstam/convex-dotnet-unofficial/commit/94d6a8d9ea42a1b0e68a890ab90b6b9eefcdb023))
* add SchemaGenerator for type-safe C# models from schema.ts ([4c1a2ec](https://github.com/zakstam/convex-dotnet-unofficial/commit/4c1a2ecf5db0b4f1a73270b8e89a5eea3421f9b5))
* enhance API documentation for ConvexClient and related features ([bd1703b](https://github.com/zakstam/convex-dotnet-unofficial/commit/bd1703bf32ab2c71893fa17d6b14046ae9c5b02d))
* enhance Cursor Playground UI and functionality ([cc2f757](https://github.com/zakstam/convex-dotnet-unofficial/commit/cc2f75716554fa152d340b15f20d6021e1127449))
* enhance logging capabilities across various components with optional debug logging ([a6b8d02](https://github.com/zakstam/convex-dotnet-unofficial/commit/a6b8d02e8e1fc89a9a67c3f6f6e7f1a1875815c6))
* introduce BatchValidationException for improved error handling in event batching ([064627e](https://github.com/zakstam/convex-dotnet-unofficial/commit/064627e2b262e114bb91643469a685f625c5a20f))
* refactor ConvexFunctionGenerator to support TypeScript files and improve constant generation ([f2e16e7](https://github.com/zakstam/convex-dotnet-unofficial/commit/f2e16e731ecf99a3969221cd41a1c0e6123c8db4))
* refactor TicTacToe service and configuration for improved clarity and functionality ([aa18191](https://github.com/zakstam/convex-dotnet-unofficial/commit/aa18191ee7e4e95b3a23210edec7357eb333887b))
* simplify ReplyService initialization by removing redundant sendReplyFunctionName parameter ([8cc294e](https://github.com/zakstam/convex-dotnet-unofficial/commit/8cc294effab117c4b3e58227e3818ba3a1e2b53f))
* update CursorService to use generated types and improve method signatures ([2154e42](https://github.com/zakstam/convex-dotnet-unofficial/commit/2154e4254e89503d6f472735e01c1119fa86e1c6))
* update project files to include TypeScript function files for type-safe constant generation ([1cff415](https://github.com/zakstam/convex-dotnet-unofficial/commit/1cff415c4163c69ec66242482334f3dd6f80415e))


### Bug Fixes

* remove obsolete Convex authentication configuration file ([d72ebe1](https://github.com/zakstam/convex-dotnet-unofficial/commit/d72ebe160281ff9c9ebf456885646faa4901206c))
* resolve race condition in WebSocket subscription reconnection ([2298c35](https://github.com/zakstam/convex-dotnet-unofficial/commit/2298c35dd605fddbddd210ba8b4e0f24e278404d))

## [1.0.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.4.0-alpha...v1.0.0-alpha) (2025-11-26)


### ⚠ BREAKING CHANGES

* SchemaValidationException now inherits from ConvexException instead of Exception. Code that catches ConvexException will now also catch SchemaValidationException.

### fix\

* change SchemaValidationException to inherit from ConvexException ([9df52ca](https://github.com/zakstam/convex-dotnet-unofficial/commit/9df52cac1ed861ada168b405c143cef18e8161b7))


### Bug Fixes

* add locking mechanism around observable subscription to ensure thread safety in ObservableConvexList ([e6a59d4](https://github.com/zakstam/convex-dotnet-unofficial/commit/e6a59d4ed50867221ec7c1037cd08497e9340f20))
* add validation for timeout property in ConvexClient to ensure it is greater than zero and does not exceed 24 hours ([0b4770b](https://github.com/zakstam/convex-dotnet-unofficial/commit/0b4770bfe9a44f44e43d4fd3b57753f151881270))
* implement lock timeout in AuthenticationManager to prevent indefinite waiting during authentication ([0254a64](https://github.com/zakstam/convex-dotnet-unofficial/commit/0254a64394d7fcb385ae522827c70e48555ec604))
* remove diagnostic logging from AuthenticationManager, AuthenticationSlice, and DefaultHttpClientProvider ([0435a2a](https://github.com/zakstam/convex-dotnet-unofficial/commit/0435a2af3d73c0a2e7a02c8039c611bab1173510))
* replace ArgumentNullException.ThrowIfNull with explicit null check in AuthenticationManager and AuthenticationSlice (for .net 2) ([4b592cc](https://github.com/zakstam/convex-dotnet-unofficial/commit/4b592cce60d2a12fcf984b5878d303e3810cf907))
* replace Dictionary with ConcurrentDictionary for cached values in ConvexClient to improve thread safety ([4e8a188](https://github.com/zakstam/convex-dotnet-unofficial/commit/4e8a1885287fb8a84ce68cc9e282fb7b91b6784b))

## [0.4.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.3.0-alpha...v0.4.0-alpha) (2025-11-26)


### Features

* add Cursor Playground example with real-time cursor tracking, user presence, and interactive features using Blazor and Convex backend ([a02940b](https://github.com/zakstam/convex-dotnet-unofficial/commit/a02940b29ff0f36efcd100028fb5593fe5f0edd9))
* add new projects for TicTacToe and Cursor Playground examples in the solution file, including shared and Blazor components ([30e0976](https://github.com/zakstam/convex-dotnet-unofficial/commit/30e097662e32354f1968f91f127dce4945af1c22))
* add time-based batching extension for high-frequency event streams with sampling, batching, and replay capabilities ([251c31f](https://github.com/zakstam/convex-dotnet-unofficial/commit/251c31f4bc6efbca15dbdbf8474aeccc5077db5e))
* implement real-time multiplayer drawing game with Blazor and Convex backend, including game mechanics, stroke batching, and player interactions ([54cb3ef](https://github.com/zakstam/convex-dotnet-unofficial/commit/54cb3ef47b704049ed69e1232a5dee8d3b08b8b0))
* introduce new features and enhancements across Convex.Client, including data access actions, caching, observability diagnostics, and improved error handling in middleware ([544253f](https://github.com/zakstam/convex-dotnet-unofficial/commit/544253fef61edc11a973a6e5c0f662cacc957030))

## [0.3.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.2.0-alpha...v0.3.0-alpha) (2025-11-18)


### Features

* enhance WebSocket client to manage subscription metadata and re-establish subscriptions after reconnection ([0f679e6](https://github.com/zakstam/convex-dotnet-unofficial/commit/0f679e61d951d5f36f694de83711c6d8db0e3dd1))


### Bug Fixes

* improve error handling and channel management in WebSocket client during subscription operations ([d5c9e74](https://github.com/zakstam/convex-dotnet-unofficial/commit/d5c9e7488cfc855df744907455baa985d4cff54e))

## [0.2.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.1.0-alpha...v0.2.0-alpha) (2025-11-18)


### Features

* add demo video and update README to include link ([a5932e8](https://github.com/zakstam/convex-dotnet-unofficial/commit/a5932e8f69a5b3c6db7c397fa175d6c419015d5d))


### Bug Fixes

* streamline serialization call in BatchQueryBuilder and simplify nullable type check ([e846495](https://github.com/zakstam/convex-dotnet-unofficial/commit/e846495229f01dec623538cb48e4f8fc924faf3a))

## [0.1.0-alpha](https://github.com/zakstam/convex-dotnet-unofficial/compare/v0.0.2-alpha...v0.1.0-alpha) (2025-11-18)


### Features

* add maximum safe retry count in QueryBuilder to prevent infinite loops ([6df7fdf](https://github.com/zakstam/convex-dotnet-unofficial/commit/6df7fdfa76f6d64858fbb1f182296bbae9e00eed))


### Bug Fixes

* add runtime null check in QueryBuilder to ensure arguments are not null ([699ce26](https://github.com/zakstam/convex-dotnet-unofficial/commit/699ce26c5ce470f880bac8567eefb261615d6ef2))
* enhance deserialization error handling in BatchQueryBuilder to support nullable types and improve error messages ([d0669b8](https://github.com/zakstam/convex-dotnet-unofficial/commit/d0669b8681aec8bfb2478844e772cb470c09fc69))
* enhance thread safety and immutability in BatchQueryBuilder to prevent modifications after execution starts ([644b3ed](https://github.com/zakstam/convex-dotnet-unofficial/commit/644b3ed594126cd246725fd30bd1e3daede64eef))
* enhance timeout exception handling in QueryBuilder to provide clearer context and logging ([cff181b](https://github.com/zakstam/convex-dotnet-unofficial/commit/cff181bec25f7637328686a5556251a46efe249b))
* ensure unique function name keys in BatchQueryBuilder by appending index for duplicates ([e5231a5](https://github.com/zakstam/convex-dotnet-unofficial/commit/e5231a5101981b9b177f31cd8356bfe4261e3798))
* improve cancellation handling in QueryBuilder to prevent retries on cancellation ([46586aa](https://github.com/zakstam/convex-dotnet-unofficial/commit/46586aa5ac28cfb3c4be126be11e0404021f34d7))
* improve error handling in BatchQueryBuilder by identifying problematic queries during serialization failures ([44cd684](https://github.com/zakstam/convex-dotnet-unofficial/commit/44cd6843d0e4bdc77db7f6102dcdd5b101e03a10))

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
