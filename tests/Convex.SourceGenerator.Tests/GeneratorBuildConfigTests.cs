using System;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Xunit;

namespace Convex.SourceGenerator.Tests;

/// <summary>
/// Tests build-time configuration defaults and safeguards for Convex source generator packaging.
/// </summary>
public class GeneratorBuildConfigTests : IDisposable
{
    private static readonly object s_msBuildLock = new();
    private static bool s_msBuildRegistered;

    private readonly string _testRoot;
    private readonly string _propsFilePath;
    private readonly string _targetsFilePath;

    public GeneratorBuildConfigTests()
    {
        lock (s_msBuildLock)
        {
            if (!s_msBuildRegistered)
            {
                MSBuildLocator.RegisterDefaults();
                s_msBuildRegistered = true;
            }
        }

        _testRoot = Path.Combine(Path.GetTempPath(), "ConvexBuildConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);

        var testAssemblyDir = Path.GetDirectoryName(typeof(GeneratorBuildConfigTests).Assembly.Location)!;
        var buildDir = Path.GetFullPath(Path.Combine(
            testAssemblyDir, "..", "..", "..", "..", "..", "src", "Convex.Client", "build"));

        _propsFilePath = Path.Combine(buildDir, "Convex.Client.props");
        _targetsFilePath = Path.Combine(buildDir, "Convex.Client.targets");

        if (!File.Exists(_propsFilePath))
        {
            throw new InvalidOperationException($"Could not find Convex.Client.props at: {_propsFilePath}");
        }

        if (!File.Exists(_targetsFilePath))
        {
            throw new InvalidOperationException($"Could not find Convex.Client.targets at: {_targetsFilePath}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }

        ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Defaults_DiagnosticProperties_AreSet()
    {
        var projectDir = Path.Combine(_testRoot, "defaults");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateProject(projectDir, "DefaultsApp", importTargets: false);

        using var projectCollection = new ProjectCollection();
        var project = projectCollection.LoadProject(csprojPath);

        Assert.Equal("warning", project.GetPropertyValue("ConvexDiagnosticMode"));
        Assert.Equal("false", project.GetPropertyValue("ConvexSuppressGeneratorWarnings"));
        Assert.Equal("false", project.GetPropertyValue("ConvexFailOnGeneratorMisconfig"));
    }

    [Fact]
    public void Targets_DoesNotForceDefaultBackendPath_WhenDiscoveryFails()
    {
        var projectDir = Path.Combine(_testRoot, "no-discovery");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateProject(projectDir, "NoDiscoveryApp", importTargets: true);

        using var projectCollection = new ProjectCollection();
        var project = projectCollection.LoadProject(csprojPath);
        var pathValue = project.GetPropertyValue("ConvexBackendPath");

        Assert.True(string.IsNullOrEmpty(pathValue));
    }

    [Fact]
    public void Targets_SetsDefaultBackendPath_WhenBackendGenerationEnabled()
    {
        var projectDir = Path.Combine(_testRoot, "backend-generation");
        Directory.CreateDirectory(projectDir);
        var csprojPath = CreateProject(projectDir, "BackendGenerationApp", importTargets: true, enableBackendGeneration: true);

        using var projectCollection = new ProjectCollection();
        var project = projectCollection.LoadProject(csprojPath);
        var actual = NormalizePath(project.GetPropertyValue("ConvexBackendPath"));
        var expected = NormalizePath(Path.Combine(projectDir, "convex"));

        Assert.Equal(expected, actual);
    }

    private string CreateProject(string directory, string projectName, bool importTargets, bool enableBackendGeneration = false)
    {
        var backendGenerationLine = enableBackendGeneration
            ? "<ConvexEnableBackendGeneration>true</ConvexEnableBackendGeneration>"
            : "";

        var targetsImport = importTargets
            ? $@"  <Import Project=""{_targetsFilePath}"" />"
            : "";

        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    {backendGenerationLine}
  </PropertyGroup>
  <Import Project=""{_propsFilePath}"" />
{targetsImport}
</Project>";

        var csprojPath = Path.Combine(directory, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, csprojContent);
        return csprojPath;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
