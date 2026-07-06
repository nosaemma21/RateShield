using RateShield.Core.RateLimiting;
using RateShield.Infrastructure.RateLimiting.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace RateShield.Gateway.Tests.RateLimiting;

public sealed class RedisTokenBucketScriptExecutorTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    private IConnectionMultiplexer? _connectionMultiplexer;

    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();

        if (_connectionMultiplexer is not null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString()
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionMultiplexer is not null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task EvaluateAsync_WhenBucketHasTokens_AllowsAndConsumesToken()
    {
        //arrange
        var executor = new RedisTokenBucketScriptExecutor(_connectionMultiplexer!);

        var policy = CreatePolicy(capacity: 2, refillTokens: 1, requestCost: 1);

        //act
        var first = await executor.EvaluateAsync(
            bucketKey: "rateshield:test:integration:allow",
            policy: policy,
            bucketIdleTimeoutSeconds: 60,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var second = await executor.EvaluateAsync(
            bucketKey: "rateshield:test:integration:allow",
            policy: policy,
            bucketIdleTimeoutSeconds: 60,
            cancellationToken: TestContext.Current.CancellationToken
        );

        //assert
        Assert.True(first.IsAllowed);
        Assert.Equal(1, first.RemainingTokens);

        Assert.True(second.IsAllowed);
        Assert.Equal(0, second.RemainingTokens);
    }

    [Fact]
    public async Task EvaluateAsync_WhenBucketHasInsufficientTokens_RejectsRequest()
    {
        //arrange
        var executor = new RedisTokenBucketScriptExecutor(_connectionMultiplexer!);

        var policy = CreatePolicy(capacity: 1, refillTokens: 1, requestCost: 1);

        var bucketKey = "rateshield:test:integration:reject";

        await executor.EvaluateAsync(
            bucketKey: bucketKey,
            policy: policy,
            bucketIdleTimeoutSeconds: 60,
            cancellationToken: TestContext.Current.CancellationToken
        );

        //act
        var decision = await executor.EvaluateAsync(
            bucketKey: bucketKey,
            policy: policy,
            bucketIdleTimeoutSeconds: 60,
            cancellationToken: TestContext.Current.CancellationToken
        );

        //assert
        Assert.False(decision.IsAllowed);
        Assert.Equal(0, decision.RemainingTokens);
        Assert.True(decision.RetryAfter > TimeSpan.Zero);
    }

    //helper
    private static RateLimitPolicy CreatePolicy(int capacity, int refillTokens, int requestCost)
    {
        return new RateLimitPolicy(
            Name: "Default",
            Capacity: capacity,
            RefillTokens: refillTokens,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: requestCost
        );
    }
}
