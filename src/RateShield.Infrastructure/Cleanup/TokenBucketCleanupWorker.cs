using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;

public sealed class TokenBucketCleanupWorker : BackgroundService
{
    private readonly RateShieldOptions _options;
    private readonly ITokenBucketCleanupService _cleanupService;

    public TokenBucketCleanupWorker(
        IOptions<RateShieldOptions> options,
        ITokenBucketCleanupService cleanupService
    )
    {
        _options = options.Value;
        _cleanupService = cleanupService;
    }

    //bg worker
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //  will run in bg while gateway is alive
        var interval = TimeSpan.FromSeconds(_options.CleanUp.IntervalSeconds);

        // the interval execution timer
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _cleanupService.RemoveIdleBuckets();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful host shutdown cancels the timer wait.
        }
    }

    //helper
    // private void RemoveIdleBuckets()
    // {
    //     var idleBefore = _clock.UtcNow.AddSeconds(-_options.CleanUp.BucketIdleTimeoutSeconds);

    //     var idleKeys = _bucketStore.GetIdleKeys(
    //         idleBefore: idleBefore,
    //         maxKeys: _options.CleanUp.MaxBucketsPerScan
    //     );

    //     var removedCount = 0;

    //     foreach (var key in idleKeys)
    //     {
    //         if (_bucketStore.TryRemove(key))
    //         {
    //             removedCount++;
    //         }
    //     }

    //     if (removedCount > 0)
    //     {
    //         _logger.LogInformation(
    //             "Removed {RemovedCount} idle token buckets. ActiveBucketCount: {ActiveBucketCount}",
    //             removedCount,
    //             _bucketStore.Count
    //         );
    //     }
    // }
}
