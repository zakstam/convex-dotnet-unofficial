using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class FunctionNameAnalyzerTests
{
    [TestMethod]
    public async Task StringLiteralFunctionName_ReportsDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.FunctionNameAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

class TestClass
{
    void TestMethod(IConvexClient client)
    {
        var result = client.Query<object>(""functions/getMessages"");
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CVX004", DiagnosticSeverity.Warning)
                .WithSpan(8, 43, 8, 66)
                .WithArguments("Queries", "GetMessages", "functions/getMessages"));

        await test.RunAsync();
    }

    [TestMethod]
    public async Task ConstantFunctionName_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.FunctionNameAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using Convex.Client;

namespace Convex.Generated
{
    public static class ConvexFunctions
    {
        public static class Queries
        {
            public const string GetMessages = ""functions/getMessages"";
        }
    }
}

class TestClass
{
    void TestMethod(Convex.Client.IConvexClient client)
    {
        var result = client.Query<object>(Convex.Generated.ConvexFunctions.Queries.GetMessages);
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        await test.RunAsync();
    }
}

