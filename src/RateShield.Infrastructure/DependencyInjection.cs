using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Observability;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;
using RateShield.Infrastructure.Cleanup;
using RateShield.Infrastructure.Observability;
using RateShield.Infrastructure.RateLimiting;
using RateShield.Infrastructure.RateLimiting.Redis;
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

        var options =
            configuration.GetSection(RateShieldOptions.SectionName).Get<RateShieldOptions>()
            ?? new RateShieldOptions();

        if (string.Equals(storageMode, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration[
                $"{RateShieldOptions.SectionName}:Redis:ConnectionString"
            ];

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisConfiguration = ConfigurationOptions.Parse(connectionString!);

                redisConfiguration.ConnectTimeout = options.Redis.ConnectTimeoutMilliseconds;

                redisConfiguration.AsyncTimeout = options.Redis.CommandTimeoutMilliseconds;

                redisConfiguration.SyncTimeout = options.Redis.CommandTimeoutMilliseconds;

                redisConfiguration.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(redisConfiguration);
            });
            services.AddSingleton<
                IRedisTokenBucketScriptExecutor,
                RedisTokenBucketScriptExecutor
            >();
            services.AddSingleton<IRedisRateLimitEvaluator, RedisRateLimitEvaluator>();
        }
        return services;
    }
}
