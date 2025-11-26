using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Convex.Client.Analyzer.Test.Tests;

[TestClass]
public class MissingErrorHandlingAnalyzerTests
{
    [TestMethod]
    public async Task ExecuteAsyncWithTryCatch_NoDiagnostic()
    {
        var test = new CSharpAnalyzerTest<Convex.Client.Analyzer.Analyzers.MissingErrorHandlingAnalyzer, DefaultVerifier>
        {
            TestCode = @"
using System;
using System.Threading.Tasks;
using Convex.Client;
using Convex.Client.Infrastructure.ErrorHandling;

class TestClass
{
    async Task TestMethod(IConvexClient client)
    {
        try
        {
            var result = await client.Query<object>(""test:query"").ExecuteAsync();
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

