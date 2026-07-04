using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RateShield.Gateway.Extensions;

namespace RateShield.Gateway.Tests.Extensions;

public sealed class ApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseRateShieldExceptionHandling_WhenPipelineThrows_ReturnsGenericGatewayError()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();

                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                });

                webBuilder.Configure(app =>
                {
                    app.UseRateShieldExceptionHandling();

                    app.Run(_ =>
                    {
                        throw new InvalidOperationException(
                            "Sensitive backend detail: http://private-backend.internal"
                        );
                    });
                });
            })
            .StartAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        using var response = await client.GetAsync(
            "/anything",
            TestContext.Current.CancellationToken
        );
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("gateway_error", body);
        Assert.Contains("The gateway could not process the request.", body);
        Assert.DoesNotContain("private-backend", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }
}
