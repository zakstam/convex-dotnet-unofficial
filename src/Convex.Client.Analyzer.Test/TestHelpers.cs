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

    /// <summary>
    /// Gets convex client reference.
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
        
        // Convex extension APIs now ship from `Convex.Client.dll`, so no extra assembly reference is needed.
    }
}
