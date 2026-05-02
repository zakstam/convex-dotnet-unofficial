using Convex.Client.Infrastructure.Telemetry;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class SensitiveDataRedactorTests
{
    [Fact]
    public void Redact_RemovesSensitiveJsonValues()
    {
        var input = "{\"token\":\"jwt-secret\",\"password\":\"pass-secret\",\"secret\":\"api-secret\",\"safe\":\"visible\"}";

        var redacted = SensitiveDataRedactor.Redact(input);

        Assert.DoesNotContain("jwt-secret", redacted);
        Assert.DoesNotContain("pass-secret", redacted);
        Assert.DoesNotContain("api-secret", redacted);
        Assert.Contains("visible", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Theory]
    [InlineData("Authorization", "Bearer abc.def.ghi")]
    [InlineData("better-auth-cookie", "__Secure-better-auth.session_token=id.signature")]
    [InlineData("Set-Better-Auth-Cookie", "__Secure-better-auth.session_token=id.signature; Path=/")]
    public void RedactHeader_RemovesSensitiveHeaderValues(string headerName, string headerValue)
    {
        var redacted = SensitiveDataRedactor.RedactHeader(headerName, headerValue);

        Assert.Equal("[REDACTED]", redacted);
    }

    [Fact]
    public void Redact_RemovesBearerTokensAndCookieValuesFromText()
    {
        var input = "Authorization=Bearer abc.def; better-auth.session_token=session.secret; safe=value";

        var redacted = SensitiveDataRedactor.Redact(input);

        Assert.DoesNotContain("abc.def", redacted);
        Assert.DoesNotContain("session.secret", redacted);
        Assert.Contains("safe=value", redacted);
    }
}
