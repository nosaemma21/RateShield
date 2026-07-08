using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Infrastructure.RateLimiting;

namespace RateShield.Core.Tests.RateLimiting;

public sealed class ConfigurationRateLimitPolicyResolverTests
{
    [Fact]
    public void ResolvePolicy_WhenRouteHasPolicyMapping_ReturnsMappedPolicy()
    {
        //arrange
        var resolver = CreateResolver();

        //act
        var policy = resolver.ResolvePolicy("premium-api");

        //assert
        Assert.Equal("Premium", policy.Name);
        Assert.Equal(200, policy.Capacity);
        Assert.Equal(20, policy.RefillTokens);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.RefillPeriod);
        Assert.Equal(2, policy.RequestCost);
    }

    [Fact]
    public void ResolvePolicy_WhenRouteHasNoPolicyMapping_ReturnsDefaultPolicy()
    {
        // arrange
        var resolver = CreateResolver();

        // act
        var policy = resolver.ResolvePolicy("unknown-route");

        // assert
        Assert.Equal("Default", policy.Name);
        Assert.Equal(100, policy.Capacity);
        Assert.Equal(10, policy.RefillTokens);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.RefillPeriod);
        Assert.Equal(1, policy.RequestCost);
    }

    [Fact]
    public void ResolvePolicy_WhenMappedPolicyDoesNotExist_ThrowsInvalidOperationException()
    {
        // arrange
        var options = CreateValidOptions();
        options.Routes["broken-api"] = new RoutePolicyOptions { PolicyName = "Missing" };

        var resolver = CreateResolver(options);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.ResolvePolicy("broken-api")
        );

        // assert
        Assert.Contains("Rate limit policy 'Missing' was not found", exception.Message);
    }

    //helper
    private static ConfigurationRateLimitPolicyResolver CreateResolver(
        RateShieldOptions? options = null
    )
    {
        return new ConfigurationRateLimitPolicyResolver(
            Options.Create(options ?? CreateValidOptions()),
            NullLogger<ConfigurationRateLimitPolicyResolver>.Instance
        );
    }

    //helper
    private static RateShieldOptions CreateValidOptions()
    {
        return new RateShieldOptions
        {
            Policies = new Dictionary<string, RateLimitPolicyOptins>
            {
                ["Default"] = new RateLimitPolicyOptins
                {
                    Capacity = 100,
                    RefillTokens = 10,
                    RefillPeriodSeconds = 1,
                    RequestCost = 1,
                },

                ["Premium"] = new RateLimitPolicyOptins
                {
                    Capacity = 200,
                    RefillTokens = 20,
                    RefillPeriodSeconds = 2,
                    RequestCost = 2,
                },
            },
            Routes = new Dictionary<string, RoutePolicyOptions>
            {
                ["premium-api"] = new RoutePolicyOptions { PolicyName = "Premium" },
            },
        };
    }
}
