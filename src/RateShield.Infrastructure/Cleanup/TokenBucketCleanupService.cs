using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;

namespace RateShield.Infrastructure.Cleanup;

public sealed class TokenBucketCleanupService : ITokenBucketCleanupService
{
    private readonly ITokenBucketStore _bucketStore;
    private readonly IClock _clock;
    private readonly RateShieldOptions _options;
    private readonly ILogger<TokenBucketCleanupService> _logger;
    private readonly IRateShieldMetrics _metrics;

    public TokenBucketCleanupService(
        ITokenBucketStore bucketStore,
        IClock clock,
        IOptions<RateShieldOptions> options,
        ILogger<TokenBucketCleanupService> logger,
        IRateShieldMetrics metrics
    )
    {
        _bucketStore = bucketStore;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public int RemoveIdleBuckets()
    {
        var idleBefore = _clock.UtcNow.AddSeconds(-_options.CleanUp.BucketIdleTimeoutSeconds);

        var idleKeys = _bucketStore.GetIdleKeys(
            idleBefore: idleBefore,
            maxKeys: _options.CleanUp.MaxBucketsPerScan
        );

        var removedCount = 0;

        foreach (var key in idleKeys)
        {
            if (_bucketStore.TryRemove(key))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation(
                "Removed {RemovedCount} idle token buckets. ActiveBucketCount: {ActiveBucketCount}",
                removedCount,
                _bucketStore.Count
            );
        }

        _metrics.RecordCleanup(
            removedBucketCount: removedCount,
            activeBucketCount: _bucketStore.Count,
            storageMode: _options.Storage.Mode
        );
        return removedCount;
    }
}
