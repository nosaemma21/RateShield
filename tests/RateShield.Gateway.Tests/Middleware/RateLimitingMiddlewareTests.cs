using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
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
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(
            context.Response.Body,
            new JsonDocumentOptions(),
            TestContext.Current.CancellationToken
        );
        var body = document.RootElement;

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey(RateLimitHeaders.RetryAfter));

        Assert.Equal("rate_limit_exceeded", body.GetProperty("error").GetString());
        Assert.Equal("Too many requests.", body.GetProperty("message").GetString());
        Assert.Equal(10, body.GetProperty("retryAfterSeconds").GetInt32());
        Assert.StartsWith("application/json", context.Response.ContentType);
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

        options.Routes["sample-api"] = new RoutePolicyOptions { PolicyName = "Default" };

        var metrics = new NoOpRateShieldMetrics();

        return new RateLimitingMiddleware(
            next: next,
            identityProvider: new FakeClientIdentityProvider(),
            rateLimitEvaluator: evaluator,
            logger: NullLogger<RateLimitingMiddleware>.Instance,
            options: Options.Create(options),
            metrics: new NoOpRateShieldMetrics()
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
