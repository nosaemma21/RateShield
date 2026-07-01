using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;
using RateShield.Core.Tests.Time;
using RateShield.Infrastructure.RateLimiting;

namespace RateShield.Core.Tests.RateLimiting;

public sealed class RateLimitEvaluatorTests
{
    private static readonly ClientIdentity Client = new(Value: "client-1", Source: "Test");

    [Fact]
    public void Evaluate_WhenClientHasTokens_AllowsRequest()
    {
        // arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var policy = CreatePolicy(capacity: 2, refillTokens: 1);

        var evaluator = CreateEvaluator(now, policy);

        //act
        var decision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        //assert
        Assert.True(decision.IsAllowed);
        Assert.Equal(1, decision.Remaining);
        Assert.Equal("Default", decision.PolicyName);
    }

    [Fact]
    public void Evaluate_WhenSameClientExhaustsBucket_RejectsRequest()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var policy = CreatePolicy(capacity: 1, refillTokens: 1);

        var evaluator = CreateEvaluator(now, policy);

        // act
        var firstDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        var secondDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        //assert
        Assert.True(firstDecision.IsAllowed);
        Assert.False(secondDecision.IsAllowed);
        Assert.Equal(TimeSpan.FromSeconds(1), secondDecision.RetryAfter);
    }

    [Fact]
    public void Evaluate_WhenRefillTimePasses_AllowsRequestAgain()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var policy = CreatePolicy(capacity: 1, refillTokens: 1);
        var clock = new FakeClock(now);

        var evaluator = CreateEvaluator(clock, policy);

        //act
        var firstDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        var secondDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        clock.Advance(TimeSpan.FromSeconds(1));

        var thirdDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
        );

        //assert
        Assert.True(firstDecision.IsAllowed);
        Assert.False(secondDecision.IsAllowed);
        Assert.True(thirdDecision.IsAllowed);
    }

    [Fact]
    public void Evaluate_WhenDifferentClientUseSameRoute_UsesSeparateBuckets()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var policy = CreatePolicy(capacity: 1, refillTokens: 1);

        var evaluator = CreateEvaluator(now, policy);

        //act
        var firstClientDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(
                Client: new ClientIdentity("client-1", "Test"),
                RouteId: "sample-api"
            )
        );

        var secondClientDecision = evaluator.Evaluate(
            new RateLimitEvaluationRequest(
                Client: new ClientIdentity("client-2", "Test"),
                RouteId: "sample-api"
            )
        );

        //assert
        Assert.True(firstClientDecision.IsAllowed);
        Assert.True(secondClientDecision.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_WhenSameClientRequestsAreConcurrent_AllowsOnlyCapacity()
    {
        // arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        var policy = new RateLimitPolicy(
            Name: "Default",
            Capacity: 10,
            RefillTokens: 1,
            RefillPeriod: TimeSpan.FromMinutes(1),
            RequestCost: 1
        );

        var evaluator = CreateEvaluator(now, policy);

        // act
        var tasks = Enumerable
            .Range(0, 50)
            .Select(_ =>
                Task.Run(() =>
                    evaluator.Evaluate(
                        new RateLimitEvaluationRequest(Client: Client, RouteId: "sample-api")
                    )
                )
            )
            .ToArray();

        var decisions = await Task.WhenAll(tasks);

        var allowedCount = decisions.Count(decision => decision.IsAllowed);
        var rejectedCount = decisions.Count(decision => !decision.IsAllowed);

        // assert
        Assert.Equal(10, allowedCount);
        Assert.Equal(40, rejectedCount);
    }

    private static RateLimitEvaluator CreateEvaluator(DateTimeOffset now, RateLimitPolicy policy)
    {
        return CreateEvaluator(new FakeClock(now), policy);
    }

    private static RateLimitEvaluator CreateEvaluator(FakeClock clock, RateLimitPolicy policy)
    {
        return new RateLimitEvaluator(
            clock: clock,
            policyResolver: new FakeRateLimitResolver(policy),
            bucketStore: new InMemoryTokenBucketStore(),
            limiter: new TokenBucketLimiter(),
            lockProvider: new TokenBucketLockProvider()
        );
    }

    private static RateLimitPolicy CreatePolicy(int capacity, int refillTokens)
    {
        return new RateLimitPolicy(
            Name: "Default",
            Capacity: capacity,
            RefillTokens: refillTokens,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: 1
        );
    }
}
