namespace RateShield.Core.RateLimiting;

public sealed class TokenBucketLimiter : ITokenBucketLimiter
{
    public RateLimitDecision Evaluate(RateLimitRequest request, TokenBucketState bucket)
    {
        //refilling before evaluation because a bucket may have earned tokens since the previous request & decision should use cleanest state
        Refill(request, bucket);

        //used by cleanup workers to remove idle buckets
        bucket.LastSeenAt = request.RequestedAt;

        // enough tokens = allow
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

        // not enough = retry
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
        // only refills after >= 1 full refill period elapsed
        var elapsed = request.RequestedAt - bucket.LastRefilledAt;

        if (elapsed < request.Policy.RefillPeriod)
        {
            return;
        }

        // only full refill periodcounted. partials are preserved by keeping LastRefilledAt aligned to last completed refill
        var periodsElapsed = (int)(elapsed.Ticks / request.Policy.RefillPeriod.Ticks);

        if (periodsElapsed <= 0)
        {
            return;
        }

        // each completed period adds Refill tokens
        var tokensToAdd = periodsElapsed * request.Policy.RefillTokens;

        // making sure bucket cant take more than it's burst capacity
        bucket.AvailableTokens = Math.Min(
            request.Policy.Capacity,
            bucket.AvailableTokens + tokensToAdd
        );

        // timestamp +++ only by complete periods.(no fraction leftover time)
        bucket.LastRefilledAt = bucket.LastRefilledAt.AddTicks(
            periodsElapsed * request.Policy.RefillPeriod.Ticks
        );
    }

    private static TimeSpan CalculateRetryAfter(RateLimitRequest request, TokenBucketState bucket)
    {
        var tokensNeeded = request.Policy.RequestCost - bucket.AvailableTokens;

        //converting missing tokens to refill periods
        var periodsNeeded = (int)Math.Ceiling(tokensNeeded / (double)request.Policy.RefillTokens);

        var nextAvailableAt = bucket.LastRefilledAt.AddTicks(
            periodsNeeded * request.Policy.RefillPeriod.Ticks
        );

        var retryAfter = nextAvailableAt - request.RequestedAt;

        // timing lands in past, never returns negative
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
