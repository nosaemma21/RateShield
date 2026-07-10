using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;
using RateShield.Gateway.Identity;
using RateShield.Gateway.Validation;
using RateShield.Infrastructure;

namespace RateShield.Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    // extension to include and validate the rateshield config opts
    public static IServiceCollection AddRateShieldOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<RateShieldOptions>()
            .Bind(configuration.GetSection(RateShieldOptions.SectionName))
            .ValidateOnStart(); // this will fail on start up if configs don't meet my

        services.AddSingleton<IValidateOptions<RateShieldOptions>, RateShieldOptionsValidator>();

        return services;
    }

    // extension to add rv proxy (yarp)
    public static IServiceCollection AddRateShieldReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRequestTimeouts();
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    //extension to add health checks
    public static IServiceCollection AddRateShieldHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        //basic
        // services.AddHealthChecks();
        // return services;
        var healthChecks = services.AddHealthChecks();

        var storageMode = configuration[$"{RateShieldOptions.SectionName}:Storage:Mode"];

        if (string.Equals(storageMode, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration[
                $"{RateShieldOptions.SectionName}:Redis:ConnectionString"
            ];

            healthChecks.AddRedis(
                redisConnectionString: connectionString!,
                name: "redis",
                tags: ["ready"]
            );
        }

        return services;
    }

    //added the services from the infra.
    public static IServiceCollection AddRateShieldApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRateShieldInfrastructure(configuration);
        services.AddSingleton<IClientIdentityProvider<HttpContext>, HttpClientIdentityProvider>();

        return services;
    }

    public static IServiceCollection AddRateShieldObservability(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var observabilityOptions =
            configuration
                .GetSection($"{RateShieldOptions.SectionName}:Observability")
                .Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        if (!observabilityOptions.Enabled)
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("RateShield");
                metrics.AddAspNetCoreInstrumentation();

                if (
                    string.Equals(
                        observabilityOptions.MetricsExporter,
                        "Console",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    metrics.AddConsoleExporter();
                }
                if (
                    string.Equals(
                        observabilityOptions.MetricsExporter,
                        "Prometheus",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    //🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨🚨
                    metrics.AddPrometheusExporter();
                }
            });

        return services;
    }
}
