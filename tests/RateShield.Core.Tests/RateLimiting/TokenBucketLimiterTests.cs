using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;

namespace RateShield.Core.Tests.RateLimiting;

public sealed class TokenBucketLimiterTests
{
    private static readonly ClientIdentity Client = new(Value: "client-1", Source: "Test");

    private static readonly RateLimitPolicy Policy = new(
        Name: "Default",
        Capacity: 10,
        RefillTokens: 2,
        RefillPeriod: TimeSpan.FromSeconds(1),
        RequestCost: 1
    );

    [Fact]
    public void Evaluate_WhenEnoughTokensAvailable_AllowsRequestAndConsumesTokens()
    {
        // Arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        var bucket = new TokenBucketState(
            availableTokens: 10,
            lastRefilledAt: now,
            lastSeenAt: now
        );

        var request = CreateRequest(now);
        var limiter = new TokenBucketLimiter();

        // Act
        var decision = limiter.Evaluate(request, bucket);

        // Assert
        Assert.True(decision.IsAllowed);
        Assert.Equal(9, bucket.AvailableTokens);
        Assert.Equal(9, decision.Remaining);
        Assert.Null(decision.RetryAfter);
        Assert.Equal(now, bucket.LastSeenAt);
    }

    [Fact]
    public void Evaluate_WhenNotEnoughTokensAvailable_RejectRequestWitRetryAfter()
    {
        //Arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        var bucket = new TokenBucketState(availableTokens: 0, lastRefilledAt: now, lastSeenAt: now);

        var request = CreateRequest(now);
        var limiter = new TokenBucketLimiter();

        // Act
        var decision = limiter.Evaluate(request, bucket);

        //Assert
        Assert.False(decision.IsAllowed);
        Assert.Equal(0, bucket.AvailableTokens);
        Assert.Equal(0, decision.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(1), decision.RetryAfter);
        Assert.Equal(now.AddSeconds(1), decision.ResetAt);
        Assert.Equal(now, bucket.LastSeenAt);
    }

    [Fact]
    public void Evaluate_WhenRefillPeriodElapsed_AddsEarnedTokensBeforeDecision()
    {
        // Arrange
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var requestedAt = start.AddSeconds(2);

        var bucket = new TokenBucketState(
            availableTokens: 0,
            lastRefilledAt: start,
            lastSeenAt: start
        );

        var request = CreateRequest(requestedAt);
        var limiter = new TokenBucketLimiter();

        //   Act
        var decision = limiter.Evaluate(request, bucket);

        // Assert
        Assert.True(decision.IsAllowed);

        // periods -2, tokens +4, -1 token consumed
        Assert.Equal(3, bucket.AvailableTokens);
        Assert.Equal(3, decision.Remaining);
        Assert.Equal(start.AddSeconds(2), bucket.LastRefilledAt);
        Assert.Equal(requestedAt, bucket.LastSeenAt);
    }

    [Fact]
    public void Evaluate_WhenRefiilWouldExceedCapacity_CapsTokensAtCapacity()
    {
        //Arrange
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var requestedAt = start.AddSeconds(10);

        var bucket = new TokenBucketState(
            availableTokens: 9,
            lastRefilledAt: start,
            lastSeenAt: start
        );

        var request = CreateRequest(requestedAt);
        var limiter = new TokenBucketLimiter();

        //Act
        var decision = limiter.Evaluate(request, bucket);

        //   Assert
        Assert.True(decision.IsAllowed);

        //   refill caps at 10, request -1
        Assert.Equal(9, bucket.AvailableTokens);
        Assert.Equal(9, decision.Remaining);
    }

    [Fact]
    public void Evaluate_WhenRequestCostRequiresMultipleRefillPeriods_ReturnsRoundedUpRetryAfter()
    {
        // Arrange

        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        var policy = new RateLimitPolicy(
            Name: "Expensive",
            Capacity: 10,
            RefillTokens: 2,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: 5
        );

        var bucket = new TokenBucketState(availableTokens: 1, lastRefilledAt: now, lastSeenAt: now);

        var request = new RateLimitRequest(
            Client: Client,
            RouteId: "sample-api",
            Policy: policy,
            RequestedAt: now
        );

        var limiter = new TokenBucketLimiter();

        //   Act
        var decision = limiter.Evaluate(request, bucket);

        // Assert
        Assert.False(decision.IsAllowed);

        // missing 4 tokens, refill +2 per second = wait 2 secs.
        Assert.Equal(TimeSpan.FromSeconds(2), decision.RetryAfter);
        Assert.Equal(now.AddSeconds(2), decision.ResetAt);
    }

    [Fact]
    public void Evaluate_WhenRequestCostIsGreaterThanOne_ConsumesRequestCostTokens()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var policy = new RateLimitPolicy(
            Name: "Expensive",
            Capacity: 10,
            RefillTokens: 2,
            RefillPeriod: TimeSpan.FromSeconds(1),
            RequestCost: 4
        );

        var bucket = new TokenBucketState(
            availableTokens: 10,
            lastRefilledAt: now,
            lastSeenAt: now
        );

        var request = new RateLimitRequest(
            Client: Client,
            RouteId: "sample-api",
            Policy: policy,
            RequestedAt: now
        );

        var limiter = new TokenBucketLimiter();

        //act
        var decision = limiter.Evaluate(request, bucket);

        //assert
        Assert.True(decision.IsAllowed);
        Assert.Equal(6, bucket.AvailableTokens);
        Assert.Equal(6, decision.Remaining);
        Assert.Null(decision.RetryAfter);
    }

    // Request dummy🧪🧪🧪
    private static RateLimitRequest CreateRequest(DateTimeOffset requestedAt)
    {
        return new RateLimitRequest(
            Client: Client,
            RouteId: "sample-api",
            Policy: Policy,
            RequestedAt: requestedAt
        );
    }
}
