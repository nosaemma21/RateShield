using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public interface IRedisTokenBucketScriptExecutor
{
    Task<RedisTokenBucketResult> EvaluateAsync(
        string bucketKey,
        RateLimitPolicy policy,
        int bucketIdleTimeoutSeconds,
        CancellationToken cancellationToken = default
    );
}
