using System.Net;
using RateShield.Infrastructure.RateLimiting.Redis;

namespace RateShield.Gateway.Tests.RateLimiting;

public sealed class RedisConnectionConfigurationTests
{
    [Fact]
    public void Create_WithRenderInternalUri_ExtractsEndpointAndAppliesTimeouts()
    {
        var configuration = RedisConnectionConfiguration.Create(
            "redis://red-example:6379",
            connectTimeoutMilliseconds: 5000,
            commandTimeoutMilliseconds: 1000
        );

        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(configuration.EndPoints));

        Assert.Equal("red-example", endpoint.Host);
        Assert.Equal(6379, endpoint.Port);
        Assert.False(configuration.Ssl);
        Assert.False(configuration.AbortOnConnectFail);
        Assert.Equal(5000, configuration.ConnectTimeout);
        Assert.Equal(1000, configuration.AsyncTimeout);
        Assert.Equal(1000, configuration.SyncTimeout);
    }

    [Fact]
    public void Create_WithTlsUri_ExtractsCredentialsAndDatabase()
    {
        var configuration = RedisConnectionConfiguration.Create(
            "rediss://default:p%40ss@redis.example.com:6380/2",
            connectTimeoutMilliseconds: 5000,
            commandTimeoutMilliseconds: 1000
        );

        Assert.True(configuration.Ssl);
        Assert.Equal("redis.example.com", configuration.SslHost);
        Assert.Equal("default", configuration.User);
        Assert.Equal("p@ss", configuration.Password);
        Assert.Equal(2, configuration.DefaultDatabase);
    }

    [Fact]
    public void Create_WithStackExchangeConfigurationString_PreservesSupportedFormat()
    {
        var configuration = RedisConnectionConfiguration.Create(
            "localhost:6379,defaultDatabase=3",
            connectTimeoutMilliseconds: 5000,
            commandTimeoutMilliseconds: 1000
        );

        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(configuration.EndPoints));

        Assert.Equal("localhost", endpoint.Host);
        Assert.Equal(6379, endpoint.Port);
        Assert.Equal(3, configuration.DefaultDatabase);
    }
}
