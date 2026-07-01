using RateShield.Gateway.Middleware;

namespace RateShield.Gateway.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRateShieldRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
