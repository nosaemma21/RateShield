using RateShield.Core.RateLimiting;
using StackExchange.Redis;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public sealed class RedisTokenBucketScriptExecutor : IRedisTokenBucketScriptExecutor
{
    private readonly IDatabase _database;

    public RedisTokenBucketScriptExecutor(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    //script exec
    public async Task<RedisTokenBucketResult> EvaluateAsync(
        string bucketKey,
        RateLimitPolicy policy,
        int bucketIdleTimeoutSeconds,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _database.ScriptEvaluateAsync(
            RedisTokenBucketScripts.EvaluateTokenBucket,
            keys: [bucketKey],
            values:
            [
                policy.Capacity,
                policy.RefillTokens,
                (long)policy.RefillPeriod.TotalMilliseconds,
                policy.RequestCost,
                bucketIdleTimeoutSeconds,
            ]
        );

        return RedisTokenBucketResultMapper.Map(result);
    }
}
