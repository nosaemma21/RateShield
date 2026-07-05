using StackExchange.Redis;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public static class RedisTokenBucketResultMapper
{
    private const int ExpectedResultLength = 5;

    public static RedisTokenBucketResult Map(RedisResult result)
    {
        var values = (RedisResult[])result!;

        if (values.Length != ExpectedResultLength)
        {
            throw new InvalidOperationException(
                $"Redis token bucket script returned {values.Length} values, but {ExpectedResultLength} were expected."
            );
        }

        var isAllowed = (int)values[0] == 1;
        var remainingTokens = (int)values[1];
        var retryAfterMs = (long)values[2];
        var resetAtUnixMs = (long)values[3];
        var evaluatedAtUnixMs = (long)values[4];

        return new RedisTokenBucketResult(
            IsAllowed: isAllowed,
            RemainingTokens: remainingTokens,
            RetryAfter: TimeSpan.FromMilliseconds(retryAfterMs),
            ResetAt: DateTimeOffset.FromUnixTimeMilliseconds(resetAtUnixMs),
            EvaluatedAt: DateTimeOffset.FromUnixTimeMilliseconds(evaluatedAtUnixMs)
        );
    }
}
