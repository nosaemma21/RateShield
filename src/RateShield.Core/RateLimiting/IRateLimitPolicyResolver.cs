namespace RateShield.Core.RateLimiting;

/// <summary>
/// Implement for varieties of resolvers
/// </summary>
public interface IRateLimitPolicyResolver
{
    RateLimitPolicy ResolvePolicy(string routeId);
}
