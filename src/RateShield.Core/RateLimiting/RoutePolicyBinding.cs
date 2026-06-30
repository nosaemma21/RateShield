namespace RateShield.Core.RateLimiting;

/// <summary>
/// Helps bind route id (from config) to a policy (from config)
/// </summary>
/// <param name="RouteId">Id of the route</param>
/// <param name="PolicyName">Policy name to bind with</param>
public sealed record RoutePolicyBinding(string RouteId, string PolicyName);
