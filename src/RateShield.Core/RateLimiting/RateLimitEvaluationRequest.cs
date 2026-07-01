using RateShield.Core.Identity;

namespace RateShield.Core.RateLimiting;

/// <summary>
/// Represents the minimal information needed to evaluate an incoming request.
/// This model stays independent from ASP.NET Core so the rate limiter can be tested without HTTP.
/// </summary>
/// <param name="Client">The resolved client identity.</param>
/// <param name="RouteId">The YARP route ID matched by the request.</param>
public sealed record RateLimitEvaluationRequest(ClientIdentity Client, string RouteId);
