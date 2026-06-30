using RateShield.Core.Configuration;

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
            .Validate(
                options => options.Policies.Count > 0,
                "At least one rate limit policy must be configured."
            )
            .Validate(
                options => options.Policies.ContainsKey("Default"),
                "A Default rate limit policy must be configured"
            //will remove default policy from json config
            )
            .Validate(
                options => options.CleanUp.IntervalSeconds > 0,
                "Cleanup options must be greater than zero"
            )
            .Validate(
                options => options.CleanUp.BucketIdleTimeoutSeconds > 0,
                "Bucket idle timeout must be greater than zero"
            )
            .Validate(
                options => options.RejectionResponse.ContentType.Length > 0,
                "Rejection response content type is required."
            )
            .Validate(
                options => options.RejectionResponse.ErrorCode.Length > 0,
                "Rejection response error code is required"
            )
            .Validate(
                options => options.RejectionResponse.Message.Length > 0,
                "Rejection response message is required"
            )
            .ValidateOnStart(); // this will fail on start up if configs don't meet my

        return services;
    }

    // extension to add rv proxy (yarp)
    public static IServiceCollection AddRateShieldReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    //extension to add health checks
    public static IServiceCollection AddRateShieldHealthChecks(this IServiceCollection services)
    {
        //basic
        services.AddHealthChecks();
        return services;
    }
}
