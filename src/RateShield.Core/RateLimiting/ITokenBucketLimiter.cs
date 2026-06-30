namespace RateShield.Core.RateLimiting;

public interface ITokenBucketLimiter
{
    /// <summary>
    /// allows or rejects the request given a bucket state
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="bucket">The token bucket state</param>
    /// <returns></returns>
    RateLimitDecision Evaluate(RateLimitRequest request, TokenBucketState bucket);
}
