using RateShield.Core.Observability;

namespace RateShield.Gateway.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Request.Headers[CorrelationHeaders.CorrelationId] = correlationId;

        //   context.Response.OnStarting(() =>
        //   {
        //       context.Response.Headers[CorrelationHeaders.CorrelationId] = correlationId;
        //       return Task.CompletedTask;
        //   });

        context.Response.Headers[CorrelationHeaders.CorrelationId] = correlationId;

        using (
            _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
        )
        {
            await _next(context);
        }
    }

    //helper
    private static string ResolveCorrelationId(HttpContext context)
    {
        var incomingCorrelationId = context
            .Request.Headers[CorrelationHeaders.CorrelationId]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(incomingCorrelationId))
        {
            return incomingCorrelationId.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
