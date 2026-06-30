using RateShield.Core.Identity;

namespace RateShield.Core.RateLimiting;

//request going into limiter
public sealed record RateLimitRequest(
    ClientIdentity Client,
    string RouteId,
    RateLimitPolicy Policy,
    DateTimeOffset RequestedAt
);
