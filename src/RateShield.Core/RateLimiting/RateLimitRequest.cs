using RateShield.Core.Identity;

namespace RateShield.Core.RateLimiting;

//request going into limiter

/// <summary>
/// Represents one request that needs to be evaluated by the rate limiter.
/// </summary>
/// <param name="Client">The resolved client identity for the incoming request.</param>
/// <param name="RouteId">The YARP route ID matched by the incoming request.</param>
/// <param name="Policy">The rate-limit policy that applies to this request.</param>
/// <param name="RequestedAt">The UTC timestamp when the request is evaluated.</param>
public sealed record RateLimitRequest(
    ClientIdentity Client,
    string RouteId,
    RateLimitPolicy Policy,
    DateTimeOffset RequestedAt
);
