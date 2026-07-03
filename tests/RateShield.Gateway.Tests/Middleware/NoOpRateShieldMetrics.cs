using RateShield.Core.Identity;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;

namespace RateShield.Gateway.Tests.Middleware;

public sealed class NoOpRateShieldMetrics : IRateShieldMetrics
{
    public void RecordDecision(
        string routeId,
        ClientIdentity client,
        RateLimitDecision decision,
        string storageMode
    ) { }

    public void RecordCleanup(int removedBucketCount, int activeBucketCount, string storageMode) { }

    public void RecordError(string errorType, string routeId, string storageMode) { }
}
