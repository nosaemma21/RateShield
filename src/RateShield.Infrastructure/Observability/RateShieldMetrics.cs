using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.Observability;

public sealed class RateShieldMetrics : IRateShieldMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _allowedRequests;
    private readonly Counter<long> _rejectedRequests;

    private readonly ITokenBucketStore _bucketStore;
    private readonly RateShieldOptions _options;
    private readonly Counter<long> _cleanupRuns;
    private readonly Counter<long> _cleanupRemovedBuckets;

    public RateShieldMetrics(ITokenBucketStore bucketStore, IOptions<RateShieldOptions> options)
    {
        _bucketStore = bucketStore;
        _options = options.Value;

        _meter = new Meter("RateShield", "1.0.0");

        _allowedRequests = _meter.CreateCounter<long>(
            "rateshield.requests.allowed",
            unit: "requests",
            description: "Number of requests allowed by RateShield."
        );

        _rejectedRequests = _meter.CreateCounter<long>(
            "rateshield.requests.rejected",
            unit: "requests",
            description: "Number of requests rejected by RateShield."
        );

        _meter.CreateObservableGauge(
            "rateshield.buckets.active",
            ObserveActiveBuckets,
            unit: "buckets",
            description: "Current number of active token buckets."
        );

        _cleanupRuns = _meter.CreateCounter<long>(
            "rateshield.cleanup.runs",
            unit: "runs",
            description: "Number of token bucket cleanup scans run by RateShield."
        );

        _cleanupRemovedBuckets = _meter.CreateCounter<long>(
            "rateshield.cleanup.removed_buckets",
            unit: "buckets",
            description: "Number of idle token buckets removed by cleanup."
        );
    }

    public void RecordDecision(
        string routeId,
        ClientIdentity client,
        RateLimitDecision decision,
        string storageMode
    )
    {
        // throw new NotImplementedException();
        var tags = new KeyValuePair<string, object?>[]
        {
            new("route.id", routeId),
            new("policy.name", decision.PolicyName),
            new("client.source", client.Source),
            new("storage.mode", storageMode),
        };

        if (decision.IsAllowed)
        {
            _allowedRequests.Add(1, tags);
            return;
        }

        _rejectedRequests.Add(1, tags);
    }

    //helper
    private Measurement<int> ObserveActiveBuckets()
    {
        return new Measurement<int>(
            _bucketStore.Count,
            new KeyValuePair<string, object?>("storage.mode", _options.Storage.Mode)
        );
    }

    // drop
    public void Dispose()
    {
        _meter.Dispose();
    }

    public void RecordCleanup(int removedBucketCount, int activeBucketCount, string storageMode)
    {
        // throw new NotImplementedException();
        var tags = new KeyValuePair<string, object?>[] { new("storage.mode", storageMode) };

        _cleanupRuns.Add(1, tags);

        if (removedBucketCount > 0)
        {
            _cleanupRemovedBuckets.Add(removedBucketCount, tags);
        }
    }
}
