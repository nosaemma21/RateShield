using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Gateway.Validation;

namespace RateShield.Gateway.Tests.Validation;

public sealed class RateShieldOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenIdentityStrategyIsMissing_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            new IdentityOptions
            {
                Strategy = "",
                ApiKeyHeaderName = "X-Api-Key",
                ClientIdHeaderName = "X-Client-Id",
                TrustForwardedHeaders = false,
                ForwardedForHeaderName = "X-Forwarded-For",
                TrustedProxyIpAddresses = [],
            }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("RateShield:Identity:Strategy is required.")
        );
    }

    [Fact]
    public void Validate_WhenApiKeyHeaderNameIsMissing_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            new IdentityOptions
            {
                Strategy = "HeaderThenIp",
                ApiKeyHeaderName = "",
                ClientIdHeaderName = "X-Client-Id",
                TrustForwardedHeaders = false,
                ForwardedForHeaderName = "X-Forwarded-For",
                TrustedProxyIpAddresses = [],
            }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("RateShield:Identity:ApiKeyHeaderName is required.")
        );
    }

    [Fact]
    public void Validate_WhenClientIdHeaderNameIsMissing_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            new IdentityOptions
            {
                Strategy = "HeaderThenIp",
                ApiKeyHeaderName = "X-Api-Key",
                ClientIdHeaderName = "",
                TrustForwardedHeaders = false,
                ForwardedForHeaderName = "X-Forwarded-For",
                TrustedProxyIpAddresses = [],
            }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("RateShield:Identity:ClientIdHeaderName is required.")
        );
    }

    [Fact]
    public void Validate_WhenForwardedHeadersAreTrustedWithoutHeaderName_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            new IdentityOptions
            {
                Strategy = "HeaderThenIp",
                ApiKeyHeaderName = "X-Api-Key",
                ClientIdHeaderName = "X-Client-Id",
                TrustForwardedHeaders = true,
                ForwardedForHeaderName = "",
                TrustedProxyIpAddresses = ["127.0.0.1"],
            }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "RateShield:Identity:ForwardedForHeaderName is required when forwarded headers are trusted."
                )
        );
    }

    [Fact]
    public void Validate_WhenForwardedHeadersAreTrustedWithoutTrustedProxyIps_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            new IdentityOptions
            {
                Strategy = "HeaderThenIp",
                ApiKeyHeaderName = "X-Api-Key",
                ClientIdHeaderName = "X-Client-Id",
                TrustForwardedHeaders = true,
                ForwardedForHeaderName = "X-Forwarded-For",
                TrustedProxyIpAddresses = [],
            }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "RateShield:Identity:TrustedProxyIpAddresses must contain at least one IP address when forwarded headers are trusted."
                )
        );
    }

    [Fact]
    public void Validate_WhenStorageModeIsRedisWithoutConnectionString_ReturnsFailure()
    {
        //arrange
        var options = CreateValidOptions(
            storage: new StorageOptions { Mode = "Redis", FailureBehavior = "FailClosed" },
            redis: new RedisOptions { ConnectionString = "" }
        );

        //act
        var result = Validate(options);

        //assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "RateShield:Redis:ConnectionString is required when RateShield:Storage:Mode is Redis."
                )
        );
    }

    [Fact]
    public void Validate_WhenRedisConnectTimeoutIsInvalid_ReturnsFailure()
    {
        // arrange
        var options = CreateValidOptions(
            storage: new StorageOptions { Mode = "Redis", FailureBehavior = "FailClosed" },
            redis: new RedisOptions
            {
                ConnectionString = "localhost:6379",
                ConnectTimeoutMilliseconds = 0,
                CommandTimeoutMilliseconds = 1000,
            }
        );

        // act
        var result = Validate(options);

        // assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "RateShield:Redis:ConnectTimeoutMilliseconds must be greater than 0."
                )
        );
    }

    [Fact]
    public void Validate_WhenRedisCommandTimeoutIsInvalid_ReturnsFailure()
    {
        // arrange
        var options = CreateValidOptions(
            storage: new StorageOptions { Mode = "Redis", FailureBehavior = "FailClosed" },
            redis: new RedisOptions
            {
                ConnectionString = "localhost:6379",
                ConnectTimeoutMilliseconds = 5000,
                CommandTimeoutMilliseconds = 0,
            }
        );

        // act
        var result = Validate(options);

        // assert
        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure =>
                failure.Contains(
                    "RateShield:Redis:CommandTimeoutMilliseconds must be greater than 0."
                )
        );
    }

    //helper fn
    private static ValidateOptionsResult Validate(RateShieldOptions options)
    {
        var validator = new RateShieldOptionsValidator();

        return validator.Validate(null, options);
    }

    //helper fn
    private static RateShieldOptions CreateValidOptions(
        IdentityOptions? identity = null,
        StorageOptions? storage = null,
        RedisOptions? redis = null
    )
    {
        return new RateShieldOptions
        {
            Storage =
                storage ?? new StorageOptions { Mode = "InMemory", FailureBehavior = "FailClosed" },
            CleanUp = new CleanUpOptions
            {
                IntervalSeconds = 60,
                BucketIdleTimeoutSeconds = 300,
                MaxBucketsPerScan = 10_000,
            },
            RejectionResponse = new RejectionResponseOptions
            {
                ContentType = "application/json",
                ErrorCode = "rate_limit_exceeded",
                Message = "Too many requests.",
            },
            Policies = new Dictionary<string, RateLimitPolicyOptins>
            {
                ["Default"] = new RateLimitPolicyOptins
                {
                    Capacity = 100,
                    RefillTokens = 10,
                    RefillPeriodSeconds = 1,
                    RequestCost = 1,
                },
            },
            Routes = new Dictionary<string, RoutePolicyOptions>
            {
                ["sample-api"] = new RoutePolicyOptions { PolicyName = "Default" },
            },
            Redis = redis ?? new RedisOptions(),
            Identity =
                identity
                ?? new IdentityOptions
                {
                    Strategy = "HeaderThenIp",
                    ApiKeyHeaderName = "X-Api-Key",
                    ClientIdHeaderName = "X-Client-Id",
                    TrustForwardedHeaders = false,
                    ForwardedForHeaderName = "X-Forwarded-For",
                    TrustedProxyIpAddresses = [],
                },
        };
    }
}
