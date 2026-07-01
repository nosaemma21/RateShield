using Microsoft.Extensions.DependencyInjection;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;
using RateShield.Infrastructure.RateLimiting;
using RateShield.Infrastructure.Time;

namespace RateShield.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRateShieldInfrastructure(this IServiceCollection services)
    {
        //////////🚦🚦🚦//////////
        services.AddSingleton<IRateLimitPolicyResolver, ConfigurationRateLimitPolicyResolver>();
        services.AddSingleton<IClock, SystemClock>(); //🧪🧪
        services.AddSingleton<ITokenBucketLimiter, TokenBucketLimiter>();
        services.AddSingleton<ITokenBucketStore, InMemoryTokenBucketStore>(); //redis later
        services.AddSingleton<TokenBucketLockProvider>();
        services.AddSingleton<IRateLimitEvaluator, RateLimitEvaluator>();

        return services;
    }
}
