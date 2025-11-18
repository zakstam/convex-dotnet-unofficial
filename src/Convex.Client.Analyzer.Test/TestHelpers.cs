using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Convex.Client.Analyzer.Test;

/// <summary>
/// Helper class for analyzer tests that provides common test configuration
/// including metadata references to Convex.Client assemblies.
/// </summary>
public static class TestHelpers
{
    private static MetadataReference? _convexClientReference;
    private static MetadataReference? _convexClientExtensionsReference;

    /// <summary>
    /// Gets the metadata reference for the Convex.Client assembly.
    /// </summary>
    public static MetadataReference GetConvexClientReference()
    {
        if (_convexClientReference != null)
            return _convexClientReference;

        // Get the path to the Convex.Client assembly
        var assemblyPath = typeof(Convex.Client.IConvexClient).Assembly.Location;
        _convexClientReference = MetadataReference.CreateFromFile(assemblyPath);
        return _convexClientReference;
    }

    /// <summary>
    /// Gets the metadata reference for the Convex.Client.Extensions assembly.
    /// </summary>
    public static MetadataReference? GetConvexClientExtensionsReference()
    {
        if (_convexClientExtensionsReference != null)
            return _convexClientExtensionsReference;

        // Try to get the extensions assembly from the same directory as Convex.Client
        var convexClientAssembly = typeof(Convex.Client.IConvexClient).Assembly;
        var convexClientLocation = convexClientAssembly.Location;
        var convexClientDir = Path.GetDirectoryName(convexClientLocation);
        
        // Look for Convex.Client.Extensions.dll in the same directory
        var extensionsPath = Path.Combine(convexClientDir!, "Convex.Client.Extensions.dll");
        
        if (File.Exists(extensionsPath))
        {
            _convexClientExtensionsReference = MetadataReference.CreateFromFile(extensionsPath);
            return _convexClientExtensionsReference;
        }

        // Try alternative locations
        var testAssemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testDir = Path.GetDirectoryName(testAssemblyLocation);
        var alternativePaths = new[]
        {
            Path.Combine(testDir!, "Convex.Client.Extensions.dll"),
            Path.Combine(testDir!, "..", "..", "..", "..", "src", "Convex.Client.Extensions", "bin", "Debug", "net8.0", "Convex.Client.Extensions.dll"),
            Path.Combine(testDir!, "..", "..", "..", "..", "src", "Convex.Client.Extensions", "bin", "Debug", "net9.0", "Convex.Client.Extensions.dll"),
        };

        foreach (var path in alternativePaths)
        {
            if (File.Exists(path))
            {
                _convexClientExtensionsReference = MetadataReference.CreateFromFile(path);
                return _convexClientExtensionsReference;
            }
        }

        // Return null if not found - some tests might not need it
        return null;
    }

    /// <summary>
    /// Configures a test state with all necessary Convex.Client references.
    /// </summary>
    public static void ConfigureTestState(SolutionState testState)
    {
        // Detect the framework version from the Convex.Client assembly location
        var convexClientAssembly = typeof(Convex.Client.IConvexClient).Assembly;
        var assemblyLocation = convexClientAssembly.Location;
        
        // Determine framework version from the path (bin/Debug/net8.0 or bin/Debug/net9.0)
        // Also check the runtime framework version as a fallback
        ReferenceAssemblies referenceAssemblies;
        var runtimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        
        if (assemblyLocation.Contains("net9.0") || runtimeVersion.Contains("9.0"))
        {
            referenceAssemblies = ReferenceAssemblies.Net.Net90;
        }
        else if (assemblyLocation.Contains("net8.0") || runtimeVersion.Contains("8.0"))
        {
            referenceAssemblies = ReferenceAssemblies.Net.Net80;
        }
        else
        {
            // Default to net8.0
            referenceAssemblies = ReferenceAssemblies.Net.Net80;
        }
        
        testState.ReferenceAssemblies = referenceAssemblies;
        testState.AdditionalReferences.Add(GetConvexClientReference());
        
        // Attributes are now included in Convex.Client.dll, so no separate reference needed

        var extensionsRef = GetConvexClientExtensionsReference();
        if (extensionsRef != null)
        {
            testState.AdditionalReferences.Add(extensionsRef);
        }
    }
}
