using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;
using Yarp.ReverseProxy.Model;

namespace RateShield.Gateway.Middleware;

public sealed class RateLimitingMiddleware
{
    private const string UnknownRouteId = "unknown";

    private readonly IRateShieldMetrics _metrics;
    private readonly RequestDelegate _next;
    private readonly IClientIdentityProvider<HttpContext> _identityProvider;
    private readonly IRateLimitEvaluator _rateLimitEvaluator;
    private RateShieldOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        IRateShieldMetrics metrics,
        RequestDelegate next,
        IClientIdentityProvider<HttpContext> identityProvider,
        IRateLimitEvaluator rateLimitEvaluator,
        IOptions<RateShieldOptions> options,
        ILogger<RateLimitingMiddleware> logger
    )
    {
        _next = next;
        _identityProvider = identityProvider;
        _rateLimitEvaluator = rateLimitEvaluator;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var routeId = ResolveRouteId(context);
        // only yarp routed traffic is rate limited

        if (routeId is null)
        {
            await _next(context);
            return;
        }

        var client = _identityProvider.ResolveClient(context);

        var decision = _rateLimitEvaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: client, RouteId: routeId)
        );

        LogRateLimitDecison(routeId, client, decision);
        _metrics.RecordDecision(
            routeId: routeId,
            client: client,
            decision: decision,
            storageMode: _options.Storage.Mode
        );

        AddRateLimitHeaders(context, decision);

        if (decision.IsAllowed)
        {
            await _next(context);
            return;
        }
        await WriteRejectionResponseAsync(context, decision);
    }

    // helper
    // private static string? ResolveRouteId(HttpContext context)
    // {
    //     //retrieve api endpoint
    //     var endpoint = context.GetEndpoint();

    //     var routeModel = endpoint?.Metadata.GetMetadata<RouteModel>();

    //     return routeModel?.Config.RouteId;
    // }

    private string? ResolveRouteId(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        var routeModel = endpoint?.Metadata.GetMetadata<RouteModel>();

        if (!string.IsNullOrWhiteSpace(routeModel?.Config.RouteId))
        {
            return routeModel.Config.RouteId;
        }

        var displayName = endpoint?.DisplayName;

        return !string.IsNullOrWhiteSpace(displayName) && _options.Routes.ContainsKey(displayName)
            ? displayName
            : null;
    }

    //helper
    private static void AddRateLimitHeaders(HttpContext context, RateLimitDecision decision)
    {
        context.Response.Headers[RateLimitHeaders.Limit] = decision.Limit.ToString();
        context.Response.Headers[RateLimitHeaders.Remaining] = decision.Remaining.ToString();
        context.Response.Headers[RateLimitHeaders.Reset] = decision
            .ResetAt.ToUnixTimeSeconds()
            .ToString();

        if (decision.RetryAfter is not null)
            context.Response.Headers[RateLimitHeaders.RetryAfter] = Math.Ceiling(
                    decision.RetryAfter.Value.TotalSeconds
                )
                .ToString();
    }

    //helper
    private async Task WriteRejectionResponseAsync(HttpContext context, RateLimitDecision decision)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = _options.RejectionResponse.ContentType;

        var body = new
        {
            Error = _options.RejectionResponse.ErrorCode,
            Message = _options.RejectionResponse.Message,
            Policy = decision.PolicyName,
            RetryAfterSeconds = decision.RetryAfter is null
                ? 0
                : (int)Math.Ceiling(decision.RetryAfter.Value.TotalSeconds),
        };

        await context.Response.WriteAsJsonAsync(body);
    }

    //helper
    private void LogRateLimitDecison(
        string routeId,
        ClientIdentity client,
        RateLimitDecision decision
    )
    {
        if (decision.IsAllowed)
        {
            _logger.LogDebug(
                "Rate limit decision allowed. RouteId: {RouteId}, PolicyName: {PolicyName}, ClientSource: {ClientSource}, StorageMode: {StorageMode}, Remaining: {Remaining}, Limit: {Limit}",
                routeId,
                decision.PolicyName,
                client.Source,
                _options.Storage.Mode,
                decision.Remaining,
                decision.Limit
            );

            return;
        }

        _logger.LogWarning(
            "Rate limit decision rejected. RouteId: {RouteId}, PolicyName: {PolicyName}, ClientSource: {ClientSource}, StorageMode: {StorageMode}, Remaining: {Remaining}, Limit: {Limit}, RetryAfterSeconds: {RetryAfterSeconds}, RejectionReason: {RejectionReason}",
            routeId,
            decision.PolicyName,
            client.Source,
            _options.Storage.Mode,
            decision.Remaining,
            decision.Limit,
            decision.RetryAfter is null ? 0 : Math.Ceiling(decision.RetryAfter.Value.TotalSeconds),
            "InsufficientTokens"
        );
    }
}
