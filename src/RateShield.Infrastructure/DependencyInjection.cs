using Microsoft.Extensions.DependencyInjection;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRateShieldInfrastructure(this IServiceCollection services)
    {
        //////////🚦🚦🚦//////////
        services.AddSingleton<IRateLimitPolicyResolver, ConfigurationRateLimitPolicyResolver>();

        return services;
    }
}
