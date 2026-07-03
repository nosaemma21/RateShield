using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting;

public sealed class ConfigurationRateLimitPolicyResolver : IRateLimitPolicyResolver
{
    private const string DefaultPolicyName = "Default";
    private readonly RateShieldOptions _options;
    private readonly ILogger<ConfigurationRateLimitPolicyResolver> _logger;

    public ConfigurationRateLimitPolicyResolver(
        IOptions<RateShieldOptions> options,
        ILogger<ConfigurationRateLimitPolicyResolver> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    public RateLimitPolicy ResolvePolicy(string routeId)
    {
        var policyName = ResolvePolicyName(routeId);

        // var policyOptions = _options.Policies[policyName];

        if (!_options.Policies.TryGetValue(policyName, out var policyOptions))
        {
            _logger.LogError(
                "Rate limit policy resolution failed. RouteId: {RouteId}, PolicyName: {PolicyName}",
                routeId,
                policyName
            );

            throw new InvalidOperationException(
                $"Rate limit policy '{policyName}' was not found for route '{routeId}'."
            );
        }

        return new RateLimitPolicy(
            Name: policyName,
            Capacity: policyOptions.Capacity,
            RefillTokens: policyOptions.RefillTokens,
            RefillPeriod: TimeSpan.FromSeconds(policyOptions.RefillPeriodSeconds),
            RequestCost: policyOptions.RequestCost
        );
    }

    private string ResolvePolicyName(string routeId)
    {
        if (_options.Routes.TryGetValue(routeId, out var routeOptions))
        {
            return routeOptions.PolicyName;
        }

        return DefaultPolicyName;
    }
}
