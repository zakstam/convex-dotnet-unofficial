using Convex.Client.Core;

Console.WriteLine("Testing Convex Serialization:");
Console.WriteLine();

// Test BigInt
long bigIntValue = 42;
string bigIntResult = ConvexSerializer.SerializeToConvexJson(bigIntValue);
Console.WriteLine($"BigInt 42: {bigIntResult}");

// Test positive infinity
double posInfValue = double.PositiveInfinity;
string posInfResult = ConvexSerializer.SerializeToConvexJson(posInfValue);
Console.WriteLine($"Positive Infinity: {posInfResult}");

// Test negative infinity
double negInfValue = double.NegativeInfinity;
string negInfResult = ConvexSerializer.SerializeToConvexJson(negInfValue);
Console.WriteLine($"Negative Infinity: {negInfResult}");

// Test NaN
double nanValue = double.NaN;
string nanResult = ConvexSerializer.SerializeToConvexJson(nanValue);
Console.WriteLine($"NaN: {nanResult}");

// Test bytes
byte[] bytesValue = [0x01, 0x02, 0x03];
string bytesResult = ConvexSerializer.SerializeToConvexJson(bytesValue);
Console.WriteLine($"Bytes [1,2,3]: {bytesResult}");

// Test negative zero
double negZeroValue = -0.0;
string negZeroResult = ConvexSerializer.SerializeToConvexJson(negZeroValue);
Console.WriteLine($"Negative Zero: {negZeroResult}");
