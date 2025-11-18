using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class GenericExceptionAnalyzerTests
{
    [TestMethod]
    public async Task GenericExceptionInCatchBlock_ReportsDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.GenericExceptionAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using System;
using Convex.Client;

class TestClass
{
    async void TestMethod(IConvexClient client)
    {
        try
        {
            await client.Query<object>(""test:query"").ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("CVX003", DiagnosticSeverity.Warning)
                .WithSpan(13, 15, 13, 29)
                .WithMessage("Catching generic Exception hides Convex-specific error information. Catch specific Convex exceptions (ConvexException, ConvexFunctionException, ConvexNetworkException, etc.) instead."));

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SpecificExceptionInCatchBlock_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.GenericExceptionAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using System;
using Convex.Client;
using Convex.Client.Shared.ErrorHandling;

class TestClass
{
    async void TestMethod(IConvexClient client)
    {
        try
        {
            await client.Query<object>(""test:query"").ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}"
        };

        TestHelpers.ConfigureTestState(test.TestState);
        await test.RunAsync();
    }
}

