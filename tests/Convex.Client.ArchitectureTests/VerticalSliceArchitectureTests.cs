using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Convex.Client.ArchitectureTests;

/// <summary>
/// Architecture tests to enforce vertical slice architecture rules.
/// These tests ensure:
/// 1. Slices do not depend on other slices
/// 2. Shared does not depend on slices
/// 3. Proper dependency direction (Slices → Shared → Nothing)
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
    [TestCategory("SliceIsolation")]
    public void Slices_Should_Not_Reference_Other_Slices()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var sliceTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Slices."))
            .ToList();

        if (!sliceTypes.Any())
        {
            Assert.Inconclusive("No slice types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var sliceType in sliceTypes)
        {
            var sliceName = GetSliceName(sliceType.Namespace!);
            var referencedTypes = GetReferencedTypes(sliceType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace != null &&
                    referencedType.Namespace.Contains(".Slices."))
                {
                    var referencedSliceName = GetSliceName(referencedType.Namespace);

                    // It's OK for a type to reference types in its own slice
                    if (sliceName != referencedSliceName)
                    {
                        violations.Add(
                            $"{sliceType.FullName} (in {sliceName} slice) " +
                            $"references {referencedType.FullName} (in {referencedSliceName} slice)");
                    }
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Slice-to-slice dependencies detected (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Slices must ONLY depend on Shared infrastructure, never other slices. " +
                         "Coordinate through ConvexClient facade instead.";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("SharedIsolation")]
    public void Shared_Should_Not_Reference_Slices()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var sharedTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Shared."))
            .ToList();

        if (!sharedTypes.Any())
        {
            Assert.Inconclusive("No shared types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var sharedType in sharedTypes)
        {
            var referencedTypes = GetReferencedTypes(sharedType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace != null &&
                    referencedType.Namespace.Contains(".Slices."))
                {
                    violations.Add(
                        $"{sharedType.FullName} (in Shared) " +
                        $"references {referencedType.FullName} (in Slices)");
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Shared infrastructure referencing slices detected (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Shared infrastructure must not depend on slices. " +
                         "Shared is for cross-cutting technical infrastructure only.";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("SliceStructure")]
    public void Slices_Should_Only_Depend_On_Shared_Or_SystemTypes()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var sliceTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Slices."))
            .ToList();

        if (!sliceTypes.Any())
        {
            Assert.Inconclusive("No slice types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act
        foreach (var sliceType in sliceTypes)
        {
            var sliceName = GetSliceName(sliceType.Namespace!);
            var referencedTypes = GetReferencedTypes(sliceType);

            foreach (var referencedType in referencedTypes)
            {
                if (referencedType.Namespace == null)
                {
                    continue;
                }

                // Check if referencing non-allowed Convex.Client namespaces

                if (referencedType.Namespace.StartsWith("Convex.Client") &&
                    !referencedType.Namespace.Contains(".Shared.") &&
                    !referencedType.Namespace.Contains($".Slices.{sliceName}") &&
                    referencedType.FullName != "Convex.Client.ConvexClient" && // Facade is OK
                    referencedType.FullName != "Convex.Client.IConvexClient")  // Interface is OK
                {
                    violations.Add(
                        $"{sliceType.FullName} references {referencedType.FullName} " +
                        $"(should only reference Shared infrastructure)");
                }
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Slices referencing non-Shared Convex.Client types (FORBIDDEN):\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Slices should only depend on:\n" +
                         "  1. Shared/* infrastructure\n" +
                         "  2. System types (.NET BCL)\n" +
                         "  3. Types within their own slice\n" +
                         "  4. ConvexClient facade (for registration)";
            Assert.Fail(message);
        }
    }

    [TestMethod]
    [TestCategory("Architecture")]
    [TestCategory("NamingConventions")]
    public void Slice_Entry_Points_Should_Be_Named_Correctly()
    {
        // Arrange
        var assembly = _convexClientAssembly!;
        var sliceTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                       t.Namespace.Contains(".Slices.") &&
                       t.IsClass &&
                       !t.IsNested &&
                       t.IsPublic)
            .ToList();

        if (!sliceTypes.Any())
        {
            Assert.Inconclusive("No slice types found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        // Act - Check for entry point naming convention
        var sliceNamespaces = sliceTypes
            .Select(t => t.Namespace!)
            .Distinct()
            .Where(ns => ns.Split('.').Length == 4) // Convex.Client.Slices.SliceName
            .ToList();

        foreach (var sliceNamespace in sliceNamespaces)
        {
            var sliceName = sliceNamespace.Split('.').Last();
            var expectedEntryPointName = $"{sliceName}Slice";

            var hasEntryPoint = sliceTypes.Any(t =>
                t.Namespace == sliceNamespace &&
                t.Name == expectedEntryPointName);

            if (!hasEntryPoint)
            {
                violations.Add(
                    $"Slice '{sliceName}' missing entry point class '{expectedEntryPointName}' " +
                    $"in namespace {sliceNamespace}");
            }
        }

        // Assert
        if (violations.Any())
        {
            var message = "Slice naming convention violations detected:\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Each slice must have an entry point class named [SliceName]Slice.cs\n" +
                         "Example: Slices/Queries/QueriesSlice.cs";
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
    public void Each_Slice_Should_Have_README()
    {
        // This test verifies that each slice directory has a README.md file
        // Note: This is a file system test, not a reflection test

        var slicesPath = Path.Combine(GetSolutionRoot(), "src", "Convex.Client", "Slices");

        if (!Directory.Exists(slicesPath))
        {
            Assert.Inconclusive("Slices directory does not exist yet. This is expected during initial migration.");
            return;
        }

        var sliceDirectories = Directory.GetDirectories(slicesPath);

        if (sliceDirectories.Length == 0)
        {
            Assert.Inconclusive("No slice directories found yet. This is expected during initial migration.");
            return;
        }

        var violations = new List<string>();

        foreach (var sliceDir in sliceDirectories)
        {
            var sliceName = Path.GetFileName(sliceDir);
            var readmePath = Path.Combine(sliceDir, "README.md");

            if (!File.Exists(readmePath))
            {
                violations.Add($"Slice '{sliceName}' missing README.md");
            }
        }

        if (violations.Any())
        {
            var message = "Slices missing documentation:\n\n" +
                         string.Join("\n", violations) +
                         "\n\nRule: Each slice must have a README.md documenting:\n" +
                         "  - Purpose and responsibilities\n" +
                         "  - Owner name and contact\n" +
                         "  - Public API surface\n" +
                         "  - Shared dependencies used\n" +
                         "  - Testing guidance";
            Assert.Fail(message);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Extracts the slice name from a namespace.
    /// Example: "Convex.Client.Slices.Queries.Internal" → "Queries"
    /// </summary>
    private static string GetSliceName(string namespaceName)
    {
        var parts = namespaceName.Split('.');
        var slicesIndex = Array.IndexOf(parts, "Slices");

        return slicesIndex >= 0 && slicesIndex < parts.Length - 1 ? parts[slicesIndex + 1] : namespaceName;
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
