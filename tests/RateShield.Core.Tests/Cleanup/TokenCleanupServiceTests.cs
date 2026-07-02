using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;
using RateShield.Core.Tests.Time;
using RateShield.Infrastructure.Cleanup;
using RateShield.Infrastructure.RateLimiting;

namespace RateShield.Core.Tests.Cleanup;

public sealed class TokenBucketCleanupServiceTests
{
    [Fact]
    public void RemoveIdleBuckets_WhenBucketIsIdle_RemovesBucket()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:30:00Z");
        var clock = new FakeClock(now);
        var store = new InMemoryTokenBucketStore();

        var key = new TokenBucketKey(
            ClientId: "client-1",
            RouteId: "sample-api",
            PolicyName: "Default"
        );

        store.GetOrCreate(key, createdAt: now.AddMinutes(-30), capacity: 10);

        var service = CreateService(store, clock, bucketIdleTimeoutSeconds: 900);

        //act
        var removedCount = service.RemoveIdleBuckets();

        //assert
        Assert.Equal(1, removedCount);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void RemoveIdleBuckets_WhenBucketIsActive_DoesNotRemoveBucket()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:30:00Z");
        var clock = new FakeClock(now);
        var store = new InMemoryTokenBucketStore();

        var key = new TokenBucketKey(
            ClientId: "client-1",
            RouteId: "sample-api",
            PolicyName: "Default"
        );

        store.GetOrCreate(key, createdAt: now.AddMinutes(-1), capacity: 10);

        var service = CreateService(store, clock, bucketIdleTimeoutSeconds: 900);

        //act

        var removedCount = service.RemoveIdleBuckets();

        //assert
        Assert.Equal(0, removedCount);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void RemoveIdleBuckets_RespectsMaxBucketsPerScan()
    {
        //arrange
        var now = DateTimeOffset.Parse("2026-01-01T00:30:00Z");
        var clock = new FakeClock(now);
        var store = new InMemoryTokenBucketStore();

        for (var index = 0; index < 5; index++)
        {
            store.GetOrCreate(
                new TokenBucketKey(
                    ClientId: $"client-{index}",
                    RouteId: "sample-api",
                    PolicyName: "Default"
                ),
                createdAt: now.AddMinutes(-30),
                capacity: 10
            );
        }

        var service = CreateService(
            store,
            clock,
            bucketIdleTimeoutSeconds: 900,
            maxBucketsPerScan: 2
        );

        //act
        var removedCount = service.RemoveIdleBuckets();

        //assert
        Assert.Equal(2, removedCount);
        Assert.Equal(3, store.Count);
    }

    //helper fn
    private static TokenBucketCleanupService CreateService(
        InMemoryTokenBucketStore store,
        FakeClock clock,
        int bucketIdleTimeoutSeconds,
        int maxBucketsPerScan = 10_000
    )
    {
        var options = new RateShieldOptions
        {
            CleanUp = new CleanUpOptions
            {
                BucketIdleTimeoutSeconds = bucketIdleTimeoutSeconds,
                MaxBucketsPerScan = maxBucketsPerScan,
                IntervalSeconds = 60,
            },
        };

        return new TokenBucketCleanupService(
            bucketStore: store,
            clock: clock,
            options: Options.Create(options),
            logger: NullLogger<TokenBucketCleanupService>.Instance
        );
    }
}
