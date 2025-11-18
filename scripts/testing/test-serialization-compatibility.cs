using System.Net.Http.Json;
using System.Text.Json;
using Convex.Client.Core;

Console.WriteLine("=== Convex Serialization Compatibility Test ===\n");

// Test BigInt serialization
long testValue = 42L;
string csharpResult = ConvexSerializer.SerializeToConvexJson(testValue);
Console.WriteLine($"C# BigInt(42) serialization: {csharpResult}");

// Compare with TypeScript oracle
using var httpClient = new HttpClient();
var oracleRequest = new { value = "42", type = "bigint" };
var oracleResponse = await httpClient.PostAsJsonAsync("http://localhost:3001/api/convex-to-json", oracleRequest);
var oracleResult = await oracleResponse.Content.ReadFromJsonAsync<dynamic>();
var tsResult = oracleResult?.result?.ToString();
Console.WriteLine($"TypeScript BigInt(42) serialization: {tsResult}");

Console.WriteLine($"\nMatch: {csharpResult == tsResult}");

// Test special float values
var testCases = new[]
{
    (double.NaN, "NaN"),
    (double.PositiveInfinity, "Infinity"),
    (double.NegativeInfinity, "-Infinity"),
    (-0.0, "negative-zero")
};

Console.WriteLine("\n=== Special Float Values ===");
foreach (var (value, tsValueName) in testCases)
{
    string csharpFloat = ConvexSerializer.SerializeToConvexJson(value);
    Console.WriteLine($"\nC# {tsValueName} serialization: {csharpFloat}");

    var floatRequest = new { value = tsValueName, type = tsValueName == "negative-zero" ? "negative-zero" : "float" };
    var floatResponse = await httpClient.PostAsJsonAsync("http://localhost:3001/api/convex-to-json", floatRequest);
    var floatResult = await floatResponse.Content.ReadFromJsonAsync<dynamic>();
    var tsFloatResult = floatResult?.result?.ToString();
    Console.WriteLine($"TypeScript {tsValueName} serialization: {tsFloatResult}");
    Console.WriteLine($"Match: {csharpFloat == tsFloatResult}");
}

// Test byte arrays
Console.WriteLine("\n=== Byte Array Values ===");
var testBytes = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
string csharpBytes = ConvexSerializer.SerializeToConvexJson(testBytes);
Console.WriteLine($"C# bytes serialization: {csharpBytes}");

var bytesRequest = new { value = testBytes, type = "bytes" };
var bytesResponse = await httpClient.PostAsJsonAsync("http://localhost:3001/api/convex-to-json", bytesRequest);
var bytesResult = await bytesResponse.Content.ReadFromJsonAsync<dynamic>();
var tsBytesResult = bytesResult?.result?.ToString();
Console.WriteLine($"TypeScript bytes serialization: {tsBytesResult}");
Console.WriteLine($"Match: {csharpBytes == tsBytesResult}");

Console.WriteLine("\n=== Test Complete ===");
