using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;
using RateShield.Infrastructure.RateLimiting.Redis;

namespace RateShield.Gateway.Tests.RateLimiting;

public sealed class RedisRateLimitEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_WhenRedisFailsAndFailureBehaviorIsFailOpen_AllowsRequest()
    {
        // arrange
        var now = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

        var evaluator = CreateEvaluator(failureBehavior: "FailOpen", now: now);

        // act
        var decision = await evaluator.EvaluateAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken
        );

        // assert
        Assert.True(decision.IsAllowed);
        Assert.Equal("Default", decision.PolicyName);
        Assert.Equal(100, decision.Limit);
        Assert.Equal(100, decision.Remaining);
        Assert.Equal(now, decision.ResetAt);
        Assert.Null(decision.RetryAfter);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRedisFailsAndFailureBehaviorIsFailClosed_RejectsRequest()
    {
        // arrange
        var now = new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

        var evaluator = CreateEvaluator(failureBehavior: "FailClosed", now: now);

        // act
        var decision = await evaluator.EvaluateAsync(
            CreateRequest(),
            TestContext.Current.CancellationToken
        );

        // assert
        Assert.False(decision.IsAllowed);
        Assert.Equal("Default", decision.PolicyName);
        Assert.Equal(100, decision.Limit);
        Assert.Equal(0, decision.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(1), decision.RetryAfter);
        Assert.Equal(now.AddSeconds(1), decision.ResetAt);
    }

    private static RedisRateLimitEvaluator CreateEvaluator(
        string failureBehavior,
        DateTimeOffset now
    )
    {
        var policy = CreatePolicy();

        return new RedisRateLimitEvaluator(
            scriptExecutor: new ThrowingRedisTokenBucketScriptExecutor(),
            policyResolver: new FakeRateLimitPolicyResolver(policy),
            options: Options.Create(CreateOptions(failureBehavior)),
            clock: new FakeClock(now)
        );
    }

    private static RateLimitEvaluationRequest CreateRequest()
    {
        return new RateLimitEvaluationRequest(
            Client: new ClientIdentity("client-1", "Test"),
            RouteId: "sample-api"
        );
    }

    private static RateShieldOptions CreateOptions(string failureBehavior)
    {
        return new RateShieldOptions
        {
            Storage = new StorageOptions { Mode = "Redis", FailureBehavior = failureBehavior },
            CleanUp = new CleanUpOptions
            {
                BucketIdleTimeoutSeconds = 300,
                IntervalSeconds = 60,
                MaxBucketsPerScan = 10_000,
            },
        };
    }

    private static RateLimitPolicy CreatePolicy()
    {
        return new RateLimitPolicy(
            Name: "Default",
            Capacity: 100,
            RefillTokens: 10,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: 1
        );
    }

    private sealed class ThrowingRedisTokenBucketScriptExecutor : IRedisTokenBucketScriptExecutor
    {
        public Task<RedisTokenBucketResult> EvaluateAsync(
            string bucketKey,
            RateLimitPolicy policy,
            int bucketIdleTimeoutSeconds,
            CancellationToken cancellationToken = default
        )
        {
            throw new InvalidOperationException("Redis is unavailable.");
        }
    }

    private sealed class FakeRateLimitPolicyResolver(RateLimitPolicy policy)
        : IRateLimitPolicyResolver
    {
        public RateLimitPolicy ResolvePolicy(string routeId)
        {
            return policy;
        }
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
