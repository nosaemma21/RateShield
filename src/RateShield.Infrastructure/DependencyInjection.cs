using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateShield.Core.Configuration;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;
using RateShield.Infrastructure.Cleanup;
using RateShield.Infrastructure.Observability;
using RateShield.Infrastructure.RateLimiting;
using RateShield.Infrastructure.Time;
using StackExchange.Redis;

namespace RateShield.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRateShieldInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        //////////🚦🚦🚦//////////
        services.AddSingleton<IRateLimitPolicyResolver, ConfigurationRateLimitPolicyResolver>();
        services.AddSingleton<IClock, SystemClock>(); //🧪🧪
        services.AddSingleton<ITokenBucketLimiter, TokenBucketLimiter>();
        services.AddSingleton<ITokenBucketStore, InMemoryTokenBucketStore>(); //redis later
        services.AddSingleton<TokenBucketLockProvider>();
        services.AddSingleton<IRateLimitEvaluator, RateLimitEvaluator>();
        services.AddSingleton<ITokenBucketCleanupService, TokenBucketCleanupService>();
        services.AddHostedService<TokenBucketCleanupWorker>();
        services.AddSingleton<IRateShieldMetrics, RateShieldMetrics>();

        var storageMode = configuration[$"{RateShieldOptions.SectionName}:Storage:Mode"];

        if (string.Equals(storageMode, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration[$"{RateShieldOptions.SectionName}:Redis:ConnectionString"];

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(connectionString!)
            );
        }
        return services;
    }
}

