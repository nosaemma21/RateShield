using RateShield.Core.RateLimiting;
using RateShield.Core.Time;

namespace RateShield.Infrastructure.RateLimiting;

public sealed class RateLimitEvaluator : IRateLimitEvaluator
{
    private readonly IClock _clock;
    private readonly IRateLimitPolicyResolver _policyResolver;
    private readonly ITokenBucketStore _bucketStore;
    private readonly ITokenBucketLimiter _limiter;
    private readonly TokenBucketLockProvider _lockProvider;

    public RateLimitEvaluator(
        IClock clock,
        IRateLimitPolicyResolver policyResolver,
        ITokenBucketStore bucketStore,
        ITokenBucketLimiter limiter,
        TokenBucketLockProvider lockProvider
    )
    {
        _clock = clock;
        _policyResolver = policyResolver;
        _bucketStore = bucketStore;
        _limiter = limiter;
        _lockProvider = lockProvider;
    }

    public RateLimitDecision Evaluate(RateLimitEvaluationRequest request)
    {
        //   throw new NotImplementedException();
        var requestedAt = _clock.UtcNow; //get time
        var policy = _policyResolver.ResolvePolicy(request.RouteId); //find policy for matched route

        // build bucket identity
        var key = new TokenBucketKey(
            ClientId: request.Client.Value,
            RouteId: request.RouteId,
            PolicyName: policy.Name
        );

        //get lock for the particular bucket in question
        var bucketLock = _lockProvider.GetLock(key);

        // protects the particular sequence
        lock (bucketLock)
        {
            var bucket = _bucketStore.GetOrCreate(
                key,
                createdAt: requestedAt,
                capacity: policy.Capacity
            );

            var limiterRequest = new RateLimitRequest(
                Client: request.Client,
                RouteId: request.RouteId,
                Policy: policy,
                RequestedAt: requestedAt
            );

            return _limiter.Evaluate(limiterRequest, bucket);
        }
    }
}
