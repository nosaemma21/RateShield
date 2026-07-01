using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;
using RateShield.Gateway.Middleware;

namespace RateShield.Gateway.Tests.Middleware;

public sealed class RateLimitingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestIsAllowed_CallsNextMiddleware()
    {
        var nextCalled = false;

        var evaluator = new FakeRateLimitEvaluator(
            new[]
            {
                RateLimitDecision.Allowed(
                    policyName: "Default",
                    limit: 100,
                    remaining: 99,
                    resetAt: DateTimeOffset.UtcNow.AddSeconds(1)
                ),
            }
        );

        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            evaluator: evaluator
        );

        var context = CreateProxyHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("sample-api", evaluator.LastRequest?.RouteId);
        Assert.True(context.Response.Headers.ContainsKey(RateLimitHeaders.Limit));
        Assert.True(context.Response.Headers.ContainsKey(RateLimitHeaders.Remaining));
        Assert.True(context.Response.Headers.ContainsKey(RateLimitHeaders.Reset));
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsRejected_ReturnsTooManyRequests()
    {
        var nextCalled = false;

        var evaluator = new FakeRateLimitEvaluator(
            new[]
            {
                RateLimitDecision.Rejected(
                    policyName: "Default",
                    limit: 100,
                    remaining: 0,
                    resetAt: DateTimeOffset.UtcNow.AddSeconds(10),
                    retryAfter: TimeSpan.FromSeconds(10)
                ),
            }
        );

        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            evaluator: evaluator
        );

        var context = CreateProxyHttpContext();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey(RateLimitHeaders.RetryAfter));
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsNotProxyRoute_SkipsRateLimiting()
    {
        var nextCalled = false;

        var evaluator = new FakeRateLimitEvaluator(Array.Empty<RateLimitDecision>());

        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            evaluator: evaluator
        );

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Null(evaluator.LastRequest);
    }

    private static RateLimitingMiddleware CreateMiddleware(
        RequestDelegate next,
        FakeRateLimitEvaluator evaluator
    )
    {
        var options = new RateShieldOptions();

        options.Routes["sample-api"] = new RoutePolicyOptions
        {
            PolicyName = "Default",
        };

        return new RateLimitingMiddleware(
            next: next,
            identityProvider: new FakeClientIdentityProvider(),
            rateLimitEvaluator: evaluator,
            options: Options.Create(options)
        );
    }

    private static DefaultHttpContext CreateProxyHttpContext()
    {
        var context = new DefaultHttpContext();

        context.SetEndpoint(
            new Endpoint(
                requestDelegate: _ => Task.CompletedTask,
                metadata: new EndpointMetadataCollection(),
                displayName: "sample-api"
            )
        );

        return context;
    }

    private sealed class FakeClientIdentityProvider : IClientIdentityProvider<HttpContext>
    {
        public ClientIdentity ResolveClient(HttpContext context)
        {
            return new ClientIdentity("client-1", "Test");
        }
    }
}
