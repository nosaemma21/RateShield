using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;
using RateShield.Infrastructure.RateLimiting.Redis;

namespace RateShield.Gateway.Tests.RateLimiting;

public sealed class RedisTokenBucketKeyBuilderTests
{
    [Fact]
    public void Build_ReturnsExpectedKeyPrefix()
    {
        // Arrange
        var client = new ClientIdentity("client-123", "ApiKeyHeader");
        var policy = CreatePolicy();
        // act

        var key = RedisTokenBucketKeyBuilder.Build(client, "sample-api", policy);

        // assert
        Assert.StartsWith("rateshield:v1:bucket:sample-api:Default:", key);
    }

    [Fact]
    public void Build_DoesNotExposeRawClientIdentity()
    {
        // arrange
        var client = new ClientIdentity("secret-api-key", "ApiKeyHeader");
        var policy = CreatePolicy();

        // act
        var key = RedisTokenBucketKeyBuilder.Build(client, "sample-api", policy);

        // assert
        Assert.DoesNotContain("secret-api-key", key);
        Assert.DoesNotContain("ApiKeyHeader", key);
    }

    [Fact]
    public void TestName()
    {
        // arrange
        var client = new ClientIdentity("cient-123", "ApiKeyHeader");
        var policy = CreatePolicy();

        // act
        var first = RedisTokenBucketKeyBuilder.Build(client, "sample-api", policy);
        var second = RedisTokenBucketKeyBuilder.Build(client, "sample-api", policy);

        // assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Build_UsesDifferentHashForDifferentIdentitySources()
    {
        // arrange
        var policy = CreatePolicy();

        var apiKeyClient = new ClientIdentity("same-value", "ApiKeyHeader");
        var remoteIpClient = new ClientIdentity("same-value", "RemoteIp");

        // act
        var first = RedisTokenBucketKeyBuilder.Build(apiKeyClient, "sample-api", policy);
        var second = RedisTokenBucketKeyBuilder.Build(remoteIpClient, "sample-api", policy);

        // asssert
        Assert.NotEqual(first, second);
    }

    //helperfn
    private static RateLimitPolicy CreatePolicy()
    {
        return new RateLimitPolicy(
            Name: "Default",
            Capacity: 100,
            RefillTokens: 10,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: 1
        );
    }
}
