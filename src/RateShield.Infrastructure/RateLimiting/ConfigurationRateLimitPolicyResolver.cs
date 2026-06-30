using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;

namespace RateShield.Core.RateLimiting;

public sealed class ConfigurationRateLimitPolicyResolver : IRateLimitPolicyResolver
{
    private const string DefaultPolicyName = "Default";
    private readonly RateShieldOptions _options;

    public ConfigurationRateLimitPolicyResolver(IOptions<RateShieldOptions> options)
    {
        _options = options.Value;
    }

    public RateLimitPolicy ResolvePolicy(string routeId)
    {
        var policyName = ResolvePolicyName(routeId);

        var policyOptions = _options.Policies[policyName];

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
