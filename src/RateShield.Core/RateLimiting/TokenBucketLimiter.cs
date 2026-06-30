namespace RateShield.Core.RateLimiting;

public sealed class TokenBucketLimiter : ITokenBucketLimiter
{
    public RateLimitDecision Evaluate(RateLimitRequest request, TokenBucketState bucket)
    {
        //refilling earned tokens
        Refill(request, bucket);

        bucket.LastSeenAt = request.RequestedAt;

        // allow if enough tokens exist
        if (bucket.AvailableTokens >= request.Policy.RequestCost)
        {
            bucket.AvailableTokens -= request.Policy.RequestCost;

            return RateLimitDecision.Allowed(
                policyName: request.Policy.Name,
                limit: request.Policy.Capacity,
                remaining: bucket.AvailableTokens,
                resetAt: CalculateResetAt(request, bucket)
            );
        }

        var retryAfter = CalculateRetryAfter(request, bucket);

        return RateLimitDecision.Rejected(
            policyName: request.Policy.Name,
            limit: request.Policy.Capacity,
            remaining: bucket.AvailableTokens,
            resetAt: request.RequestedAt.Add(retryAfter),
            retryAfter: retryAfter
        );
    }

    // to refil the tokens
    public static void Refill(RateLimitRequest request, TokenBucketState bucket)
    {
        var elapsed = request.RequestedAt - bucket.LastRefilledAt;

        if (elapsed < request.Policy.RefillPeriod)
        {
            return;
        }

        var periodsElapsed = (int)(elapsed.Ticks / request.Policy.RefillPeriod.Ticks);

        if (periodsElapsed <= 0)
        {
            return;
        }

        var tokensToAdd = periodsElapsed * request.Policy.RefillTokens;

        bucket.AvailableTokens = Math.Min(
            request.Policy.Capacity,
            bucket.AvailableTokens + tokensToAdd
        );

        bucket.LastRefilledAt = bucket.LastRefilledAt.AddTicks(
            periodsElapsed * request.Policy.RefillPeriod.Ticks
        );
    }

    private static TimeSpan CalculateRetryAfter(RateLimitRequest request, TokenBucketState bucket)
    {
        var tokensNeeded = request.Policy.RequestCost - bucket.AvailableTokens;

        var periodsNeeded = (int)
            Math.Ceiling(tokensNeeded / (double)request.Policy.RefillPeriod.Ticks);

        var nextAvailableAt = bucket.LastRefilledAt.AddTicks(
            periodsNeeded * request.Policy.RefillPeriod.Ticks
        );

        var retryAfter = nextAvailableAt - request.RequestedAt;

        return retryAfter <= TimeSpan.Zero ? TimeSpan.Zero : retryAfter;
    }

    private static DateTimeOffset CalculateResetAt(
        RateLimitRequest request,
        TokenBucketState bucket
    )
    {
        if (bucket.AvailableTokens >= request.Policy.Capacity)
        {
            return request.RequestedAt;
        }

        var tokensNeeded = request.Policy.Capacity - bucket.AvailableTokens;

        var periodsNeeded = (int)Math.Ceiling(tokensNeeded / (double)request.Policy.RefillTokens);

        return bucket.LastRefilledAt.AddTicks(periodsNeeded * request.Policy.RefillPeriod.Ticks);
    }
}
