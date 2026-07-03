using RateShield.Gateway.Security;

namespace RateShield.Gateway.Tests.Security;

public sealed class HeaderRedactorTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("X-Api-Key")]
    [InlineData("X-Client-Secret")]
    [InlineData("X-Auth-Token")]
    public void Redact_WhenHeaderIsSensitive_ReturnsRedactedValue(string headerName)
    {
        //act
        var result = HeaderRedactor.Redact(headerName, "secret-value");

        //assert
        Assert.Equal("[REDACTED]", result);
    }

    [Fact]
    public void Redact_WhenHeaderIsNotSensitive_ReturnsOriginalValue()
    {
        // act
        var result = HeaderRedactor.Redact("X-Correlation-ID", "correlation-123");

        Assert.Equal("correlation-123", result);
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("authorization")]
    [InlineData("X-Api-Key")]
    public void IsSensitive_WhenHeaderIsSensitive_ReturnsTrue(string headerName)
    {
        //act
        var result = HeaderRedactor.IsSensitive(headerName);
        //assert
        Assert.True(result);
    }

    [Fact]
    public void IsSensitive_WhenHeaderIsNotSensitive_ReturnsFalse()
    {
        //act
        var result = HeaderRedactor.IsSensitive("X-Correlation-ID");
        //assert
        Assert.False(result);
    }
}
