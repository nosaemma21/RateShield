//extensions for endpoint builder to helps me prevent clutter in Program.cs

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RateShield.Core.Identity;

namespace RateShield.Gateway.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRateShieldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "RateShield gateway is running");

        // if process is alive
        endpoints.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions { Predicate = _ => false }
        );

        //if process is ready to serve traffic
        endpoints.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = healthCheck => healthCheck.Tags.Contains("ready") }
        );

        //FOR PROMETHEUS (BETA)🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨
        endpoints.MapPrometheusScrapingEndpoint();

        // for yarp
        endpoints.MapReverseProxy();

        // 🧪🧪
        // endpoints.MapGet(
        //     "/debug/policy/{routeId}",
        //     (string routeId, IRateLimitPolicyResolver resolver) =>
        //     {
        //         return resolver.ResolvePolicy(routeId);
        //     }
        // );

        //🧪🧪

        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (environment.IsDevelopment())
        {
            endpoints.MapGet(
                "/debug/client",
                (HttpContext context, IClientIdentityProvider<HttpContext> identityProvider) =>
                {
                    var identity = identityProvider.ResolveClient(context);

                    return new
                    {
                        identity.Source,
                        Value = identity.Source is "ApiKeyHeader" ? "[REDACTED]" : identity.Value,
                    };
                }
            );
        }
        ;

        return endpoints;
    }
}
