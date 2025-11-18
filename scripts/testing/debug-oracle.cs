using System.Net.Http.Json;
using System.Text.Json;
using Convex.Client.Core;

Console.WriteLine("Testing ConvexSerializer against TypeScript oracle...");

using var httpClient = new HttpClient();

// Test special floats
var testCases = new[]
{
    (value: double.NaN, type: "float", description: "NaN"),
    (value: double.PositiveInfinity, type: "float", description: "PositiveInfinity"),
    (value: double.NegativeInfinity, type: "float", description: "NegativeInfinity"),
    (value: -0.0, type: "negative-zero", description: "NegativeZero")
};

foreach (var (value, type, description) in testCases)
{
    Console.WriteLine($"\n=== Testing {description} ===");

    // Get C# result
    string csharpResult = ConvexSerializer.SerializeToConvexJson(value);
    Console.WriteLine($"C#: {csharpResult}");

    // Get TypeScript oracle result
    var oracleRequest = new { value = value.ToString(), type };
    try
    {
        var oracleResponse = await httpClient.PostAsJsonAsync("http://localhost:3001/api/convex-to-json", oracleRequest);
        var oracleResult = await oracleResponse.Content.ReadFromJsonAsync<OracleResponse>();
        Console.WriteLine($"TS: {oracleResult?.Result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Oracle error: {ex.Message}");
    }
}

// Test bytes
Console.WriteLine("\n=== Testing Bytes ===");
var bytesTest = new byte[] { 0x01, 0x02, 0x03 };
string csharpBytesResult = ConvexSerializer.SerializeToConvexJson(bytesTest);
Console.WriteLine($"C# bytes: {csharpBytesResult}");

var bytesOracleRequest = new { value = bytesTest, type = "bytes" };
try
{
    var oracleResponse = await httpClient.PostAsJsonAsync("http://localhost:3001/api/convex-to-json", bytesOracleRequest);
    var oracleResult = await oracleResponse.Content.ReadFromJsonAsync<OracleResponse>();
    Console.WriteLine($"TS bytes: {oracleResult?.Result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Oracle bytes error: {ex.Message}");
}

record OracleResponse(string Result);
