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
using Microsoft.Extensions.Hosting;

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
            async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync(cancellationToken);

                return Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                        ContentType = context.Request.ContentType,
                        Body = requestBody,
                    }
                );
            }
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

    [Fact]
    public async Task Gateway_WhenPostRequestIsAllowed_ForwardsBodyToBackend()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapPost(
            "/api/{**catchAll}",
            async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync(cancellationToken);

                return Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                        ContentType = context.Request.ContentType,
                        Body = requestBody,
                    }
                );
            }
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

        var request = new { Name = "RateShield", Mode = "PostForwardingTest" };

        // act
        using var response = await gatewayClient.PostAsJsonAsync(
            "/api/orders",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedRequest>(cancellationToken);

        // assert
        Assert.NotNull(body);
        Assert.Equal("POST", body.Method);
        Assert.Equal("/api/orders", body.Path);
        Assert.Equal("application/json; charset=utf-8", body.ContentType);
        Assert.Contains("RateShield", body.Body);
        Assert.Contains("PostForwardingTest", body.Body);
    }

    [Fact]
    public async Task Gateway_WhenPutRequestIsAllowed_ForwardsBodyToBackend()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapPut(
            "/api/{**catchAll}",
            async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync(cancellationToken);

                return Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                        ContentType = context.Request.ContentType,
                        Body = requestBody,
                    }
                );
            }
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

        var request = new { Name = "RateShield", Mode = "PutForwardingTest" };

        // act
        using var response = await gatewayClient.PutAsJsonAsync(
            "/api/orders/42",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedRequest>(cancellationToken);

        // assert
        Assert.NotNull(body);
        Assert.Equal("PUT", body.Method);
        Assert.Equal("/api/orders/42", body.Path);
        Assert.Contains("RateShield", body.Body);
        Assert.Contains("PutForwardingTest", body.Body);
    }

    [Fact]
    public async Task Gateway_WhenPatchRequestIsAllowed_ForwardsBodyToBackend()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapPatch(
            "/api/{**catchAll}",
            async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync(cancellationToken);

                return Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                        ContentType = context.Request.ContentType,
                        Body = requestBody,
                    }
                );
            }
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

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/orders/42")
        {
            Content = JsonContent.Create(new { Name = "RateShield", Mode = "PatchForwardingTest" }),
        };

        // act
        using var response = await gatewayClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedRequest>(cancellationToken);

        // assert
        Assert.NotNull(body);
        Assert.Equal("PATCH", body.Method);
        Assert.Equal("/api/orders/42", body.Path);
        Assert.Equal("application/json; charset=utf-8", body.ContentType);
        Assert.Contains("RateShield", body.Body);
        Assert.Contains("PatchForwardingTest", body.Body);
    }

    [Fact]
    public async Task Gateway_WhenDeleteRequestIsAllowed_ForwardsRequestToBackend()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapDelete(
            "/api/{**catchAll}",
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
        using var response = await gatewayClient.DeleteAsync(
            "/api/orders/42?hardDelete=true",
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedRequest>(cancellationToken);

        // assert
        Assert.NotNull(body);
        Assert.Equal("DELETE", body.Method);
        Assert.Equal("/api/orders/42", body.Path);
        Assert.Equal("?hardDelete=true", body.QueryString);
    }

    [Fact]
    public async Task Gateway_WhenRequestHasImportantHeaders_ForwardsHeadersToBackend()
    {
        //arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapGet(
            "/api/{**catchAll}",
            (HttpContext context) =>
                Results.Ok(
                    new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        CorrelationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                        ClientId = context.Request.Headers["X-Client-Id"].ToString(),
                        ApiKey = context.Request.Headers["X-Api-Key"].ToString(),
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
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/42");

        request.Headers.Add("X-Correlation-ID", "test-correlation-id");
        request.Headers.Add("X-Client-Id", "client-123");
        request.Headers.Add("X-Api-Key", "api-key-123");

        // act
        using var response = await gatewayClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ForwardedHeadersRequest>(
            cancellationToken
        );

        // assert
        Assert.NotNull(body);
        Assert.Equal("GET", body.Method);
        Assert.Equal("/api/orders/42", body.Path);
        Assert.Equal("test-correlation-id", body.CorrelationId);
        Assert.Equal("client-123", body.ClientId);
        Assert.Equal("api-key-123", body.ApiKey);
    }

    [Fact]
    public async Task Gateway_WhenBackendReturnsHeaders_ForwardsHeadersToClient()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var backendBuilder = WebApplication.CreateBuilder();

        backendBuilder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        await using var backend = backendBuilder.Build();

        backend.MapGet(
            "/api/{**catchAll}",
            (HttpContext context) =>
            {
                context.Response.Headers["X-Backend-Version"] = "sample-v1";
                context.Response.Headers["X-Backend-Region"] = "local-test";

                return Results.Ok(
                    new { Method = context.Request.Method, Path = context.Request.Path.Value }
                );
            }
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
        using var response = await gatewayClient.GetAsync("/api/orders/42", cancellationToken);

        response.EnsureSuccessStatusCode();

        // assert
        Assert.True(response.Headers.TryGetValues("X-Backend-Version", out var versionValues));
        Assert.Contains("sample-v1", versionValues);

        Assert.True(response.Headers.TryGetValues("X-Backend-Region", out var regionValues));
        Assert.Contains("local-test", regionValues);
    }

    private sealed record ForwardedRequest(
        string Method,
        string Path,
        string QueryString,
        string? ContentType,
        string Body
    );

    //helper
    private sealed record ForwardedHeadersRequest(
        string Method,
        string Path,
        string CorrelationId,
        string ClientId,
        string ApiKey
    );
}
