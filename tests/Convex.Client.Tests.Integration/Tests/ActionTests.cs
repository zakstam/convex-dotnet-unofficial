using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for action operations.
/// Actions are server-side functions that can perform side effects.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class ActionTests
{
    private readonly ConvexFixture _fixture;

    public ActionTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Echo_ShouldReturnMessage()
    {
        var result = await _fixture.Client
            .Action<EchoResult>("testActions:echo")
            .WithArgs(new { message = "Hello, Convex!" })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal("Hello, Convex!", result.echo);
        Assert.True(result.timestamp > 0);
    }

    [Fact]
    public async Task Compute_Add_ShouldReturnSum()
    {
        var result = await _fixture.Client
            .Action<ComputeResult>("testActions:compute")
            .WithArgs(new { a = 10.0, b = 5.0, operation = "add" })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(15.0, result.result);
        Assert.Equal("add", result.operation);
    }

    [Fact]
    public async Task Compute_Subtract_ShouldReturnDifference()
    {
        var result = await _fixture.Client
            .Action<ComputeResult>("testActions:compute")
            .WithArgs(new { a = 10.0, b = 3.0, operation = "subtract" })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(7.0, result.result);
        Assert.Equal("subtract", result.operation);
    }

    [Fact]
    public async Task Compute_Multiply_ShouldReturnProduct()
    {
        var result = await _fixture.Client
            .Action<ComputeResult>("testActions:compute")
            .WithArgs(new { a = 6.0, b = 7.0, operation = "multiply" })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(42.0, result.result);
        Assert.Equal("multiply", result.operation);
    }

    [Fact]
    public async Task Compute_Divide_ShouldReturnQuotient()
    {
        var result = await _fixture.Client
            .Action<ComputeResult>("testActions:compute")
            .WithArgs(new { a = 20.0, b = 4.0, operation = "divide" })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(5.0, result.result);
        Assert.Equal("divide", result.operation);
    }

    [Fact]
    public async Task Compute_DivideByZero_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Action<ComputeResult>("testActions:compute")
                .WithArgs(new { a = 10.0, b = 0.0, operation = "divide" })
                .ExecuteAsync();
        });

        Assert.Contains("Division by zero", exception.Message);
    }

    [Fact]
    public async Task Compute_UnknownOperation_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Action<ComputeResult>("testActions:compute")
                .WithArgs(new { a = 10.0, b = 5.0, operation = "modulo" })
                .ExecuteAsync();
        });

        Assert.Contains("Unknown operation", exception.Message);
    }

    [Fact]
    public async Task Delay_ShouldWaitApproximately()
    {
        var delayMs = 100;
        var result = await _fixture.Client
            .Action<DelayResult>("testActions:delay")
            .WithArgs(new { milliseconds = delayMs })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(delayMs, (int)result.requestedDelay);
        // Allow some tolerance for network latency
        Assert.True(result.actualDelay >= delayMs - 10, $"Actual delay {result.actualDelay} was less than expected {delayMs}");
    }

    [Fact]
    public async Task ThrowError_ShouldThrowWithMessage()
    {
        var errorMessage = "Test error from action";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Action<object>("testActions:throwError")
                .WithArgs(new { errorMessage })
                .ExecuteAsync();
        });

        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task Action_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _fixture.Client
                .Action<EchoResult>("testActions:echo")
                .WithArgs(new { message = "This should be cancelled" })
                .ExecuteAsync(cts.Token);
        });
    }
}

/// <summary>
/// Result type for the echo action.
/// </summary>
public class EchoResult
{
    public string echo { get; set; } = string.Empty;
    public double timestamp { get; set; }
}

/// <summary>
/// Result type for the compute action.
/// </summary>
public class ComputeResult
{
    public double result { get; set; }
    public string operation { get; set; } = string.Empty;
}

/// <summary>
/// Result type for the delay action.
/// </summary>
public class DelayResult
{
    public double requestedDelay { get; set; }
    public double actualDelay { get; set; }
}
