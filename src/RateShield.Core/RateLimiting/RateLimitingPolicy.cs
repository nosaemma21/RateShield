namespace RateShield.Core.RateLimiting;

//types map to config policy

/// <summary>
/// Defines the token bucket settings used to rate-limit requests for a route or client group.
/// </summary>
/// <param name="Name">The policy name from configuration, such as Default or Strict.</param>
/// <param name="Capacity">The maximum number of tokens the bucket can hold. This controls burst size.</param>
/// <param name="RefillTokens">The number of tokens added after each completed refill period.</param>
/// <param name="RefillPeriod">How often tokens are refilled.</param>
/// <param name="RequestCost">The number of tokens consumed by a single request using this policy.</param>
public sealed record RateLimitPolicy(
    string Name,
    int Capacity,
    int RefillTokens,
    TimeSpan RefillPeriod,
    int RequestCost
);
