using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;

namespace RateShield.Core.Observability;

public interface IRateShieldMetrics
{
    void RecordDecision(
        string routeId,
        ClientIdentity client,
        RateLimitDecision decision,
        string storageMode
    );
}
