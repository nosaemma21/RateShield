namespace RateShield.Infrastructure.RateLimiting.Redis;

/// <summary>
/// Model to map to the lua script for redis
/// </summary>
/// <param name="IsAllowed">Whether request should pass</param>
/// <param name="RemainingTokens">Tokens left after request evaluation</param>
/// <param name="RetryAfter">How long client waits before retrying. allowed will be Timespan.Zero</param>
/// <param name="ResetAt">When bucket is expected to be full again</param>
/// <param name="EvaluatedAt">Redis server time used for eval</param>
public sealed record RedisTokenBucketResult(
    bool IsAllowed,
    int RemainingTokens,
    TimeSpan RetryAfter,
    DateTimeOffset ResetAt,
    DateTimeOffset EvaluatedAt
);
