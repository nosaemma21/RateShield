using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RateShield.Gateway.Tests.Integration;

public sealed class GatewayForwardingTests
{
    [Fact]
    public async Task Gateway_ForwardsExpectedRequestToBackend()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapMethods(
            "/api/{**catchAll}",
            ["GET", "POST", "PUT", "PATCH", "DELETE"],
            (HttpContext context) =>
                Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                    }
                )
        );

        await backend.StartAsync(cancellationToken);

        var server = backend.Services.GetRequiredService<IServer>();
        var backendAddress = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        await using var gatewayFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureAppConfiguration(
                    (_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                [
                                    "ReverseProxy:Clusters:sample-backend:"
                                        + "Destinations:sample-backend-primary:Address"
                                ] = backendAddress,
                            }
                        );
                    }
                );
            }
        );

        using var gatewayClient = gatewayFactory.CreateClient();

        // act
        using var response = await gatewayClient.GetAsync(
            "/api/orders/42?include=items",
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedRequest>(cancellationToken);

        // assert
        Assert.NotNull(body);
        Assert.Equal("GET", body.Method);
        Assert.Equal("/api/orders/42", body.Path);
        Assert.Equal("?include=items", body.QueryString);
    }

    private sealed record ForwardedRequest(string Method, string Path, string QueryString);
}
