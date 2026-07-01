using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Gateway.Identity;

namespace RateShield.Gateway.Tests.Identity;

public sealed class HttpClientIdentityProviderTests
{
    [Fact]
    public void ResolveClient_WhenApiKeyHeaderExists_UsesApiKey()
    {
        //arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Api-Key"] = "api-key-123";

        var provider = CreateProvider();

        //act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("api-key-123", identity.Value);
        Assert.Equal("ApiKeyHeader", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenClientIdHeaderExists_UsesClientId()
    {
        //arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Client-Id"] = "client-42";

        var provider = CreateProvider();

        //act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("client-42", identity.Value);
        Assert.Equal("ClientIdHeader", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenApiKeyAndClientIdExists_UsesApiKeyFirst()
    {
        //arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Api-Key"] = "api-key-123";
        context.Request.Headers["X-Client-Id"] = "client-42";

        var provider = CreateProvider();

        //  act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("api-key-123", identity.Value);
        Assert.Equal("ApiKeyHeader", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenNoHeadersExist_UsesRemoteIp()
    {
        //arrange
        var context = CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        var provider = CreateProvider();

        // act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("203.0.113.10", identity.Value);
        Assert.Equal("RemoteIp", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenHeaderValueHasWhiteSpace_TrimsValue()
    {
        //arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Api-Key"] = " api-key-123 ";

        var provider = CreateProvider();

        //act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("api-key-123", identity.Value);
        Assert.Equal("ApiKeyHeader", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenApiKeyHeaderIsWhitespace_FallsBackToClientId()
    {
        // arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Api-Key"] = "   ";
        context.Request.Headers["X-Client-Id"] = "client-42";

        var provider = CreateProvider();

        //act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("client-42", identity.Value);
        Assert.Equal("ClientIdHeader", identity.Source);
    }

    [Fact]
    public void ResolveClient_WhenCustomHeaderNamesConfigured_UsesConfiguredNames()
    {
        //arrange
        var context = CreateHttpContext();
        context.Request.Headers["X-Custom-Key"] = "custom-key";

        var provider = CreateProvider(
            new IdentityOptions
            {
                ApiKeyHeaderName = "X-Custom-Key",
                ClientIdHeaderName = "X-Custom-Client",
            }
        );

        // act
        var identity = provider.ResolveClient(context);

        //assert
        Assert.Equal("custom-key", identity.Value);
        Assert.Equal("ApiKeyHeader", identity.Source);
    }

    //helpers------------------**-----------------//
    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("127.0.0.1") },
        };
    }

    private static HttpClientIdentityProvider CreateProvider(
        IdentityOptions? identityOptions = null
    )
    {
        var options = new RateShieldOptions { Identity = identityOptions ?? new IdentityOptions() };

        return new HttpClientIdentityProvider(Options.Create(options));
    }
}
