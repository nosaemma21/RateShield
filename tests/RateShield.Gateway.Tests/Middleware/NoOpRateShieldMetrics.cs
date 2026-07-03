using RateShield.Core.Identity;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;

namespace RateShield.Gateway.Tests.Middleware;

internal sealed class NoOpRateShieldMetrics : IRateShieldMetrics
{
    public void RecordDecision(
        string routeId,
        ClientIdentity client,
        RateLimitDecision decision,
        string storageMode
    ) { }
}
