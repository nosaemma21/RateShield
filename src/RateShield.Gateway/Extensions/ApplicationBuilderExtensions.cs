using System.Text.Json;
using OpenTelemetry.Trace;
using RateShield.Gateway.Middleware;

namespace RateShield.Gateway.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRateShieldRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    public static IApplicationBuilder UseRateShieldCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static IApplicationBuilder UseRateShieldExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var body = new
                {
                    Error = "gateway_error",
                    Message = "The gateway could not process the request.",
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, body);
            });
        });
    }
}
