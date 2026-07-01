namespace RateShield.Core.RateLimiting;

/// <summary>
/// Identifies one token bucket for a specific client, route, and policy.
/// </summary>
/// <param name="ClientId">The normalized client identity value.</param>
/// <param name="RouteId">The YARP route ID matched by the request.</param>
/// <param name="PolicyName">The rate-limit policy applied to the route.</param>
public sealed record TokenBucketKey(string ClientId, string RouteId, string PolicyName);
