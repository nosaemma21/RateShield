using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;

public sealed class TokenBucketCleanupWorker : BackgroundService
{
    private readonly ITokenBucketStore _bucketStore;
    private readonly IClock _clock;
    private readonly RateShieldOptions _options;
    private readonly ILogger<TokenBucketCleanupWorker> _logger;

    public TokenBucketCleanupWorker(
        ITokenBucketStore bucketStore,
        IClock clock,
        RateShieldOptions options,
        ILogger<TokenBucketCleanupWorker> logger
    )
    {
        _bucketStore = bucketStore;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    //bg worker
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //  will run in bg while gateway is alive
        var interval = TimeSpan.FromSeconds(_options.CleanUp.IntervalSeconds);

        // the interval execution timer
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            RemoveIdleBuckets();
        }
    }

    //helper
    private void RemoveIdleBuckets()
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
    }
}
