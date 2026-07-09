using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RateShield.Gateway.Tests.Integration;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task ReadinessEndpoint_WhenInMemoryStorageIsHealthy_ReturnsOk()
    {
        //arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        //act
        using var response = await client.GetAsync("/health/ready", cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        //assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
