using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public interface IRedisRateLimitEvaluator
{
    Task<RateLimitDecision> EvaluateAsync(
        RateLimitEvaluationRequest request,
        CancellationToken cancellationToken = default
    );
}
