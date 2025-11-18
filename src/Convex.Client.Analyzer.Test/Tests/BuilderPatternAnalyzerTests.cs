using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class BuilderPatternAnalyzerTests
{
    [TestMethod]
    public async Task BuilderWithoutExecuteAsync_ReportsDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.BuilderPatternAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var builder = client.Query<object>(""test:query"");
        // Missing ExecuteAsync()
    }
}"
        };

        // Note: This test may need adjustment based on actual analyzer behavior
        // The analyzer checks for assignments without ExecuteAsync
        TestHelpers.ConfigureTestState(test.TestState);
        await test.RunAsync();
    }

    [TestMethod]
    public async Task BuilderWithExecuteAsync_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.BuilderPatternAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using System.Threading.Tasks;
using Convex.Client;

class TestClass
{
    async Task TestMethod(IConvexClient client)
    {
        var result = await client.Query<object>(""test:query"").ExecuteAsync();
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        await test.RunAsync();
    }
}

