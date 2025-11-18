using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class ArgumentTypeAnalyzerTests
{
    [TestMethod]
    public async Task AnonymousObjectArguments_ReportsDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.ArgumentTypeAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var result = client.Query<object>(""test:query"").WithArgs(new { id = 123 });
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CVX008", Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                .WithSpan(8, 66, 8, 82));

        await test.RunAsync();
    }

    [TestMethod]
    public async Task TypedArguments_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.ArgumentTypeAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

class TestArgs
{
    public int Id { get; set; }
}

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var args = new TestArgs { Id = 123 };
        var result = client.Query<object>(""test:query"").WithArgs(args);
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        await test.RunAsync();
    }
}

