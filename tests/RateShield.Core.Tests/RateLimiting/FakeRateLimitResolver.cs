using RateShield.Core.RateLimiting;

namespace RateShield.Core.Tests.RateLimiting;

public sealed class FakeRateLimitResolver : IRateLimitPolicyResolver
{
    private readonly RateLimitPolicy _policy;

    public FakeRateLimitResolver(RateLimitPolicy policy)
    {
        _policy = policy;
    }

    public RateLimitPolicy ResolvePolicy(string routeId)
    {
        //   throw new NotImplementedException();
        return _policy;
    }
}
