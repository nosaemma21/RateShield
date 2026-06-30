namespace RateShield.Core.RateLimiting;

//types map to config policy
public sealed record RateLimitPolicy(
    string Name,
    int Capacity,
    int RefillTokens,
    TimeSpan RefillPeriod,
    int RequestCost
);
