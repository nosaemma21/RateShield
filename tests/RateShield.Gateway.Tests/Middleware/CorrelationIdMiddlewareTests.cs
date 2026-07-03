using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RateShield.Core.Observability;
using RateShield.Gateway.Middleware;

namespace RateShield.Gateway.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAaync_WhenCorrelationIdExists_PreservesIt()
    {
        //arrange
        var nextCalled = false;

        var middleware = new CorrelationIdMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            logger: NullLogger<CorrelationIdMiddleware>.Instance
        );

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeaders.CorrelationId] = "test-correlation-id";

        //arrange
        await middleware.InvokeAsync(context);

        // assert
        Assert.True(nextCalled);
        Assert.Equal(
            "test-correlation-id",
            context.Request.Headers[CorrelationHeaders.CorrelationId]
        );
        Assert.Equal(
            "test-correlation-id",
            context.Response.Headers[CorrelationHeaders.CorrelationId]
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdIsMissing_GeneratesOne()
    {
        //arrange
        var middleware = new CorrelationIdMiddleware(
            next: _ => Task.CompletedTask,
            logger: NullLogger<CorrelationIdMiddleware>.Instance
        );

        var context = new DefaultHttpContext();

        //act
        await middleware.InvokeAsync(context);

        //assert
        Assert.False(
            string.IsNullOrWhiteSpace(context.Request.Headers[CorrelationHeaders.CorrelationId])
        );
        Assert.Equal(
            context.Request.Headers[CorrelationHeaders.CorrelationId],
            context.Response.Headers[CorrelationHeaders.CorrelationId]
        );
    }
}
