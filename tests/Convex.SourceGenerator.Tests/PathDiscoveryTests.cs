using System;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Xunit;

namespace Convex.SourceGenerator.Tests;

/// <summary>
/// Integration tests for ConvexBackendPath auto-discovery in MSBuild props files.
/// Tests that the props files correctly discover the convex backend folder
/// in various project directory structures.
/// </summary>
public class PathDiscoveryTests : IDisposable
{
    private static readonly object s_msBuildLock = new();
    private static bool s_msBuildRegistered;

    private readonly string _testRoot;
    private readonly string _propsFilePath;

    public PathDiscoveryTests()
    {
        // Register MSBuild once (thread-safe)
        lock (s_msBuildLock)
        {
            if (!s_msBuildRegistered)
            {
                TryRegisterMSBuild();
                s_msBuildRegistered = true;
            }
        }

        // Create a unique test root directory
        _testRoot = Path.Combine(Path.GetTempPath(), "ConvexTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);

        // Get the path to the actual Convex.Client.props file
        var testAssemblyDir = Path.GetDirectoryName(typeof(PathDiscoveryTests).Assembly.Location)!;
        // Navigate from tests/Convex.SourceGenerator.Tests/bin/Debug/net9.0 to src/Convex.Client/build
        _propsFilePath = Path.GetFullPath(Path.Combine(
            testAssemblyDir, "..", "..", "..", "..", "..", "src", "Convex.Client", "build", "Convex.Client.props"));

        if (!File.Exists(_propsFilePath))
        {
            throw new InvalidOperationException($"Could not find Convex.Client.props at: {_propsFilePath}");
        }
    }

    public void Dispose()
    {
        // Clean up test directories
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Unload all projects to prevent memory leaks
        ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tests the Godot project structure where backend/convex is a sibling to the .csproj file.
    /// This is the exact scenario reported in the GitHub issue.
    ///
    /// Structure:
    /// godot-project-root/
    /// ├── backend/
    /// │   └── convex/
    /// │       └── schema.ts
    /// └── GodotApp.csproj
    /// </summary>
    [Fact]
    public void AutoDiscovers_BackendConvex_AsSiblingToCsproj()
    {
        // Arrange: Create the Godot-like project structure
        var projectDir = Path.Combine(_testRoot, "godot-project");
        var backendConvexDir = Path.Combine(projectDir, "backend", "convex");
        Directory.CreateDirectory(backendConvexDir);

        // Create a dummy schema.ts to make it a valid convex folder
        File.WriteAllText(Path.Combine(backendConvexDir, "schema.ts"), "// test schema");

        var csprojPath = CreateTestCsproj(projectDir, "GodotApp");

        // Act: Evaluate the project
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert: Should discover backend/convex
        Assert.NotNull(convexBackendPath);
        Assert.NotEmpty(convexBackendPath);

        var normalizedExpected = NormalizePath(backendConvexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests the standard structure where convex is directly in the project directory.
    ///
    /// Structure:
    /// project/
    /// ├── convex/
    /// │   └── schema.ts
    /// └── MyApp.csproj
    /// </summary>
    [Fact]
    public void AutoDiscovers_Convex_InSameDirectory()
    {
        // Arrange
        var projectDir = Path.Combine(_testRoot, "standard-project");
        var convexDir = Path.Combine(projectDir, "convex");
        Directory.CreateDirectory(convexDir);
        File.WriteAllText(Path.Combine(convexDir, "schema.ts"), "// test schema");

        var csprojPath = CreateTestCsproj(projectDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(convexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests the monorepo structure where convex is in the parent directory.
    ///
    /// Structure:
    /// monorepo/
    /// ├── convex/
    /// │   └── schema.ts
    /// └── apps/
    ///     └── MyApp.csproj
    /// </summary>
    [Fact]
    public void AutoDiscovers_Convex_InParentDirectory()
    {
        // Arrange
        var monorepoDir = Path.Combine(_testRoot, "monorepo");
        var convexDir = Path.Combine(monorepoDir, "convex");
        var appsDir = Path.Combine(monorepoDir, "apps");
        Directory.CreateDirectory(convexDir);
        Directory.CreateDirectory(appsDir);
        File.WriteAllText(Path.Combine(convexDir, "schema.ts"), "// test schema");

        var csprojPath = CreateTestCsproj(appsDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(convexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests the monorepo structure where backend/convex is in the parent directory.
    ///
    /// Structure:
    /// monorepo/
    /// ├── backend/
    /// │   └── convex/
    /// │       └── schema.ts
    /// └── apps/
    ///     └── MyApp.csproj
    /// </summary>
    [Fact]
    public void AutoDiscovers_BackendConvex_InParentDirectory()
    {
        // Arrange
        var monorepoDir = Path.Combine(_testRoot, "monorepo-backend");
        var backendConvexDir = Path.Combine(monorepoDir, "backend", "convex");
        var appsDir = Path.Combine(monorepoDir, "apps");
        Directory.CreateDirectory(backendConvexDir);
        Directory.CreateDirectory(appsDir);
        File.WriteAllText(Path.Combine(backendConvexDir, "schema.ts"), "// test schema");

        var csprojPath = CreateTestCsproj(appsDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(backendConvexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests the deep monorepo structure where backend/convex is two levels up.
    ///
    /// Structure:
    /// monorepo/
    /// ├── backend/
    /// │   └── convex/
    /// │       └── schema.ts
    /// └── apps/
    ///     └── dotnet/
    ///         └── MyApp.csproj
    /// </summary>
    [Fact]
    public void AutoDiscovers_BackendConvex_TwoLevelsUp()
    {
        // Arrange
        var monorepoDir = Path.Combine(_testRoot, "deep-monorepo");
        var backendConvexDir = Path.Combine(monorepoDir, "backend", "convex");
        var dotnetDir = Path.Combine(monorepoDir, "apps", "dotnet");
        Directory.CreateDirectory(backendConvexDir);
        Directory.CreateDirectory(dotnetDir);
        File.WriteAllText(Path.Combine(backendConvexDir, "schema.ts"), "// test schema");

        var csprojPath = CreateTestCsproj(dotnetDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(backendConvexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests that explicit ConvexBackendPath takes precedence over auto-discovery.
    /// </summary>
    [Fact]
    public void ExplicitPath_TakesPrecedence_OverAutoDiscovery()
    {
        // Arrange
        var projectDir = Path.Combine(_testRoot, "explicit-path-project");
        var autoDiscoverDir = Path.Combine(projectDir, "convex");
        var explicitDir = Path.Combine(projectDir, "custom", "backend");
        Directory.CreateDirectory(autoDiscoverDir);
        Directory.CreateDirectory(explicitDir);
        File.WriteAllText(Path.Combine(autoDiscoverDir, "schema.ts"), "// auto schema");
        File.WriteAllText(Path.Combine(explicitDir, "schema.ts"), "// explicit schema");

        var csprojPath = CreateTestCsproj(projectDir, "MyApp", explicitConvexBackendPath: explicitDir);

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(explicitDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Tests that no path is set when no convex folder exists.
    /// </summary>
    [Fact]
    public void ReturnsEmpty_WhenNoConvexFolderExists()
    {
        // Arrange
        var projectDir = Path.Combine(_testRoot, "no-convex-project");
        Directory.CreateDirectory(projectDir);

        var csprojPath = CreateTestCsproj(projectDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert
        Assert.True(string.IsNullOrEmpty(convexBackendPath));
    }

    /// <summary>
    /// Tests priority: same-directory convex/ takes precedence over parent directory.
    /// </summary>
    [Fact]
    public void SameDirectory_TakesPrecedence_OverParentDirectory()
    {
        // Arrange: Both project/convex and parent/convex exist
        var parentDir = Path.Combine(_testRoot, "priority-test");
        var projectDir = Path.Combine(parentDir, "app");
        var parentConvexDir = Path.Combine(parentDir, "convex");
        var projectConvexDir = Path.Combine(projectDir, "convex");

        Directory.CreateDirectory(parentConvexDir);
        Directory.CreateDirectory(projectConvexDir);
        File.WriteAllText(Path.Combine(parentConvexDir, "schema.ts"), "// parent schema");
        File.WriteAllText(Path.Combine(projectConvexDir, "schema.ts"), "// project schema");

        var csprojPath = CreateTestCsproj(projectDir, "MyApp");

        // Act
        var convexBackendPath = EvaluateConvexBackendPath(csprojPath);

        // Assert: Should prefer same-directory convex
        Assert.NotNull(convexBackendPath);
        var normalizedExpected = NormalizePath(projectConvexDir);
        var normalizedActual = NormalizePath(convexBackendPath);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    private string CreateTestCsproj(string directory, string projectName, string? explicitConvexBackendPath = null)
    {
        Directory.CreateDirectory(directory);

        var explicitPathProperty = explicitConvexBackendPath != null
            ? $"<ConvexBackendPath>{explicitConvexBackendPath}</ConvexBackendPath>"
            : "";

        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    {explicitPathProperty}
  </PropertyGroup>
  <Import Project=""{_propsFilePath}"" />
</Project>";

        var csprojPath = Path.Combine(directory, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, csprojContent);
        return csprojPath;
    }

    private static string? EvaluateConvexBackendPath(string csprojPath)
    {
        using var projectCollection = new ProjectCollection();
        var project = projectCollection.LoadProject(csprojPath);
        return project.GetPropertyValue("ConvexBackendPath");
    }

    private static string NormalizePath(string path)
    {
        // Normalize path separators and resolve any .. or . segments
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void TryRegisterMSBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        try
        {
            MSBuildLocator.RegisterDefaults();
        }
        catch (InvalidOperationException)
        {
            // In some test hosts, Microsoft.Build assemblies may already be loaded
            // before tests run. In that case registration is not possible/needed.
        }
    }
}
