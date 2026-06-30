//extensions for endpoint builder to helps me prevent clutter in Program.cs

namespace RateShield.Gateway.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRateShieldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "RateShield gateway is running");

        // if process is alive
        endpoints.MapHealthChecks("/health/live");

        //if process is ready to serve traffic
        endpoints.MapHealthChecks("/health/ready");

        // for yarp
        endpoints.MapReverseProxy();

        return endpoints;
    }
}
