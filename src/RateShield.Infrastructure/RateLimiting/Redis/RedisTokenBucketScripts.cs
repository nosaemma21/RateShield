namespace RateShield.Infrastructure.RateLimiting.Redis;

public static class RedisTokenBucketScripts
{
    /// <summary>
    /// Returns array which stackex package will give me back as RedisResult
    /// </summary>
    public const string EvaluateTokenBucket = """
         local bucketKey = KEYS[1]

         local capacity = tonumber(ARGV[1])
         local refillTokens = tonumber(ARGV[2])
         local refillPeriodMs = tonumber(ARGV[3])
         local requestCost = tonumber(ARGV[4])
         local idleTimeoutSeconds = tonumber(ARGV[5])

         local redisTime = redis.call("TIME")
         local nowMs = (tonumber(redisTime[1]) * 1000) + math.floor(tonumber(redisTime[2]) / 1000)

         local bucket = redis.call("HMGET", bucketKey, "tokens", "last_refilled_at_unix_ms", "last_seen_at_unix_ms")

         local tokens = tonumber(bucket[1])
         local lastRefilledAtMs = tonumber(bucket[2])

         if tokens == nil then
            tokens = capacity
            lastRefilledAtMs = nowMs
         end

         local elapsedMs = nowMs - lastRefilledAtMs

         if elapsedMs >= refillPeriodMs then
            local periodsElapsed = math.floor(elapsedMs / refillPeriodMs)
            local tokensToAdd = periodsElapsed * refillTokens

            tokens = math.min(capacity, tokens + tokensToAdd)
            lastRefilledAtMs = lastRefilledAtMs + (periodsElapsed * refillPeriodMs)
         end

         local allowed = 0
         local retryAfterMs = 0

         if tokens >= requestCost then
            tokens = tokens - requestCost
            allowed = 1
         else
            local tokensNeeded = requestCost - tokens
            local periodsNeeded = math.ceil(tokensNeeded / refillTokens)
            local nextAvailableAtMs = lastRefilledAtMs + (periodsNeeded * refillPeriodMs)

            retryAfterMs = math.max(0, nextAvailableAtMs - nowMs)
         end

         local tokensNeededForFullReset = capacity - tokens
         local resetPeriodsNeeded = math.ceil(tokensNeededForFullReset / refillTokens)
         local resetAtMs = lastRefilledAtMs + (resetPeriodsNeeded * refillPeriodMs)

         redis.call(
            "HSET",
            bucketKey,
            "tokens",
            tokens,
            "last_refilled_at_unix_ms",
            lastRefilledAtMs,
            "last_seen_at_unix_ms",
            nowMs
         )

         redis.call("EXPIRE", bucketKey, idleTimeoutSeconds)

         return {
            allowed,
            tokens,
            retryAfterMs,
            resetAtMs,
            nowMs
         }
        """;
}
