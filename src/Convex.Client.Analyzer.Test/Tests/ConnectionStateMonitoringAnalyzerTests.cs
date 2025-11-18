using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class ConnectionStateMonitoringAnalyzerTests
{
    [TestMethod]
    public async Task ObserveWithoutConnectionMonitoring_ReportsDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.ConnectionStateMonitoringAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var subscription = client.Observe<object>(""test:query"");
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CVX002", DiagnosticSeverity.Warning)
                .WithSpan(8, 28, 8, 64)
                .WithArguments("test:query"));

        await test.RunAsync();
    }

    [TestMethod]
    public async Task ObserveWithResilientSubscription_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.ConnectionStateMonitoringAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;
using Convex.Client.Extensions;

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var subscription = client.CreateResilientSubscription<object>(""test:query"");
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        // Ignore compiler errors - the Extensions assembly might not be available in test environment
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        await test.RunAsync();
    }
}

