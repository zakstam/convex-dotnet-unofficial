using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Convex.Client.ArchitectureTests;

/// <summary>
/// Architecture tests to enforce vertical slice architecture rules with module-based organization.
/// These tests ensure:
/// 1. Features do not depend on other features (strict isolation, even within same module)
/// 2. Infrastructure does not depend on features
/// 3. Proper dependency direction (Features → Infrastructure → .NET BCL)
/// 4. Module-based structure is maintained (Features/[Module]/[Feature]/)
/// </summary>
[TestClass]
public class VerticalSliceArchitectureTests
{
    private static Assembly? _convexClientAssembly;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _convexClientAssembly = typeof(ConvexClient).Assembly;
    }


    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("FeatureIsolation")]
    public void Features_Should_Not_Reference_Other_Features()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var featureTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Features."))
            .ToList();

        if (!featureTypes.Any())
        {
            Assert.Inconclusive("No feature types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var featureType in featureTypes)
        {
            var featureName = GetFeatureName(featureType.Namespace!);
            var referencedTypes = GetReferencedTypes(featureType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace != null &&
                    referencedType.Namespace.Contains(".Features."))
                {
                    var referencedFeatureName = GetFeatureName(referencedType.Namespace);

                    // STRICT ISOLATION: Features cannot reference ANY other feature,
                    // even within the same module
                    if (featureName != referencedFeatureName)
                    {
                        violations.Add(
                            $"{featureType.FullName} (in {featureName} feature) " +
                            $"references {referencedType.FullName} (in {referencedFeatureName} feature)");
                    }
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Feature-to-feature dependencies detected (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Features must ONLY depend on Infrastructure, never other features. " +
                         "Coordinate through ConvexClient facade instead.";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("InfrastructureIsolation")]
    public void Infrastructure_Should_Not_Reference_Features()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var infrastructureTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Infrastructure."))
            .ToList();

        if (!infrastructureTypes.Any())
        {
            Assert.Inconclusive("No infrastructure types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var infrastructureType in infrastructureTypes)
        {
            var referencedTypes = GetReferencedTypes(infrastructureType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace != null &&
                    referencedType.Namespace.Contains(".Features."))
                {
                    violations.Add(
                        $"{infrastructureType.FullName} (in Infrastructure) " +
                        $"references {referencedType.FullName} (in Features)");
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Infrastructure referencing features detected (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Infrastructure must not depend on features. " +
                         "Infrastructure is for cross-cutting technical infrastructure only.";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("FeatureStructure")]
    public void Features_Should_Only_Depend_On_Infrastructure_Or_SystemTypes()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var featureTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Features."))
            .ToList();

        if (!featureTypes.Any())
        {
            Assert.Inconclusive("No feature types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var featureType in featureTypes)
        {
            var featureName = GetFeatureName(featureType.Namespace!);
            var referencedTypes = GetReferencedTypes(featureType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace == null)
                {
                    continue;
                }

                // Check if referencing non-allowed Convex.Client namespaces

                if (referencedType.Namespace.StartsWith("Convex.Client") &&
                    !referencedType.Namespace.Contains(".Infrastructure.") &&
                    !referencedType.Namespace.Contains($".Features.{GetModuleName(featureType.Namespace!)}.{featureName}") &&
                    referencedType.FullName != "Convex.Client.ConvexClient" && // Facade is OK
                    referencedType.FullName != "Convex.Client.IConvexClient")  // Interface is OK
                {
                    violations.Add(
                        $"{featureType.FullName} references {referencedType.FullName} " +
                        $"(should only reference Infrastructure)");
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Features referencing non-Infrastructure Convex.Client types (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Features should only depend on:\n" +
                         "  1. Infrastructure/* infrastructure\n" +
                         "  2. System types (.NET BCL)\n" +
                         "  3. Types within their own feature\n" +
                         "  4. ConvexClient facade (for registration)";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("NamingConventions")]
    public void Feature_Entry_Points_Should_Be_Named_Correctly()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var featureTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                       t.Namespace.Contains(".Features.") &&
                       t.IsClass &&
                       !t.IsNested &&
                       t.IsPublic)
            .ToList();

        if (!featureTypes.Any())
        {
            Assert.Inconclusive("No feature types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act - Check for entry point naming convention
        // Each feature namespace should have at least one public class ending in "Slice"
        var featureNamespaces = featureTypes
            .Select(t => t.Namespace!)
            .Distinct()
            .Where(ns => ns.Split('.').Length == 5) // Convex.Client.Features.Module.Feature
            .ToList();

        foreach (var featureNamespace in featureNamespaces)
        {
            var featureName = featureNamespace.Split('.').Last();

            // Check for any public class ending in "Slice"
            var hasSliceEntryPoint = featureTypes.Any(t =>
                t.Namespace == featureNamespace &&
                t.Name.EndsWith("Slice"));

            if (!hasSliceEntryPoint)
            {
                violations.Add(
                    $"Feature '{featureName}' (namespace {featureNamespace}) " +
                    $"missing entry point class ending in 'Slice'");
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Feature naming convention violations detected:\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Each feature must have a public entry point class ending in 'Slice'\n" +
                         "Example: Features/DataAccess/Queries/QueriesSlice.cs or FileStorageSlice.cs";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("LegacyCode")]
    public void CoreOperations_Should_Eventually_Be_Removed()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var coreOperationsTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".CoreOperations."))
            .ToList();

        // Act & Assert
        if (coreOperationsTypes.Any())
        {
            var typeCount = coreOperationsTypes.Count;
            var namespaces = coreOperationsTypes
                .Select(t => t.Namespace!)
                .Distinct()
                .OrderBy(ns => ns)
                .ToList();

            Assert.Inconclusive(
                $"CoreOperations still exists ({typeCount} types in {namespaces.Count} namespaces).\n\n" +
                $"This is expected during migration. CoreOperations will be removed when all slices are migrated.\n\n" +
                $"Namespaces found:\n" +
                string.Join("\n", namespaces.Select(ns => $"  - {ns}")));
        }
        else
        {
            // SUCCESS! CoreOperations has been fully removed
            Assert.IsTrue(true, "CoreOperations successfully removed! Vertical slice migration complete.");
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("Documentation")]
    public void Each_Feature_Should_Have_README()
    {
        // This test verifies that each feature directory has a README.md file
        // Note: This is a file system test, not a reflection test

        var featuresPath = Path.Combine(GetSolutionRoot(), "src", "Convex.Client", "Features");

        if (!Directory.Exists(featuresPath))
        {
            Assert.Inconclusive("Features directory does not exist yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Check each module directory (DataAccess, RealTime, etc.)
        var moduleDirectories = Directory.GetDirectories(featuresPath);

        if (moduleDirectories.Length == 0)
        {
            Assert.Inconclusive("No module directories found yet. This is expected during initial migration.");
            return;
        }

        foreach (var moduleDir in moduleDirectories)
        {
            var moduleName = Path.GetFileName(moduleDir);

            // Get feature directories within this module
            var featureDirectories = Directory.GetDirectories(moduleDir);

            foreach (var featureDir in featureDirectories)
            {
                var featureName = Path.GetFileName(featureDir);
                var readmePath = Path.Combine(featureDir, "README.md");

                if (!File.Exists(readmePath))
                {
                    violations.Add($"Feature '{moduleName}/{featureName}' missing README.md");
                }
            }
        }

        if (violations.Any())
        {
            var message = "Features missing documentation:\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Each feature must have a README.md documenting:\n" +
                         "  - Purpose and responsibilities\n" +
                         "  - Module classification\n" +
                         "  - Public API surface\n" +
                         "  - Infrastructure dependencies used\n" +
                         "  - Testing guidance";
            Assert.Fail(message);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Extracts the feature name from a namespace.
    /// Example: "Convex.Client.Features.DataAccess.Queries.Internal" → "Queries"
    /// </summary>
    private static string GetFeatureName(string namespaceName)
    {
        var parts = namespaceName.Split('.');
        var featuresIndex = Array.IndexOf(parts, "Features");

        // Namespace structure: Convex.Client.Features.Module.Feature[.SubNamespace]
        // featuresIndex + 2 gives us the feature name
        return featuresIndex >= 0 && featuresIndex < parts.Length - 2 ? parts[featuresIndex + 2] : namespaceName;
    }

    /// <summary>
    /// Extracts the module name from a namespace.
    /// Example: "Convex.Client.Features.DataAccess.Queries" → "DataAccess"
    /// </summary>
    private static string GetModuleName(string namespaceName)
    {
        var parts = namespaceName.Split('.');
        var featuresIndex = Array.IndexOf(parts, "Features");

        // Namespace structure: Convex.Client.Features.Module.Feature
        // featuresIndex + 1 gives us the module name
        return featuresIndex >= 0 && featuresIndex < parts.Length - 1 ? parts[featuresIndex + 1] : namespaceName;
    }

    /// <summary>
    /// Gets all types referenced by a given type (from fields, properties, methods, etc.)
    /// </summary>
    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        var referencedTypes = new HashSet<Type>();

        try
        {
            // Get types from fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                AddTypeAndGenericArguments(field.FieldType, referencedTypes);
            }

            // Get types from properties
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                AddTypeAndGenericArguments(property.PropertyType, referencedTypes);
            }

            // Get types from methods (parameters and return types)
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                AddTypeAndGenericArguments(method.ReturnType, referencedTypes);

                foreach (var parameter in method.GetParameters())
                {
                    AddTypeAndGenericArguments(parameter.ParameterType, referencedTypes);
                }
            }

            // Get types from constructors
            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    AddTypeAndGenericArguments(parameter.ParameterType, referencedTypes);
                }
            }

            // Get base type and interfaces
            if (type.BaseType != null)
            {
                AddTypeAndGenericArguments(type.BaseType, referencedTypes);
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                AddTypeAndGenericArguments(interfaceType, referencedTypes);
            }
        }
        catch
        {
            // Some types may throw exceptions when reflecting over them
            // This is OK for our purposes - we'll just skip them
        }

        return referencedTypes;
    }

    /// <summary>
    /// Adds a type and its generic arguments to the set
    /// </summary>
    private static void AddTypeAndGenericArguments(Type type, HashSet<Type> types)
    {
        if (type == null)
        {
            return;
        }

        // Add the type itself (unwrap if it's an array, pointer, or by-ref)

        var actualType = type;
        if (type.HasElementType)
        {
            actualType = type.GetElementType()!;
        }

        _ = types.Add(actualType);

        // Add generic arguments recursively
        if (type.IsGenericType)
        {
            foreach (var genericArg in type.GetGenericArguments())
            {
                AddTypeAndGenericArguments(genericArg, types);
            }
        }
    }

    /// <summary>
    /// Gets the solution root directory
    /// </summary>
    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();

        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "convex-dotnet-client.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        return currentDir ?? throw new InvalidOperationException("Could not find solution root directory");
    }

    #endregion
}
