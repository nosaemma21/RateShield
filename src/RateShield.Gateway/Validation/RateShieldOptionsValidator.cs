using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;

namespace RateShield.Gateway.Validation;

public sealed class RateShieldOptionsValidator : IValidateOptions<RateShieldOptions>
{
    private const string DefaultPolicyName = "Default";

    public ValidateOptionsResult Validate(string? name, RateShieldOptions options)
    {
        var failures = new List<string>();

        ValidatePolicies(options, failures);
        ValidateRoutes(options, failures);
        ValidateCleanup(options, failures);
        ValidateRejectionResponse(options, failures);
        ValidateIdentity(options, failures);
        ValidateRedis(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidatePolicies(RateShieldOptions options, List<string> failures)
    {
        if (options.Policies.Count == 0)
        {
            failures.Add("At least one rate limit policy must be configured.");
            return;
        }

        if (!options.Policies.ContainsKey(DefaultPolicyName))
        {
            failures.Add("A default rate limit policy must be configured.");
        }

        foreach (var (policyName, policy) in options.Policies)
        {
            if (string.IsNullOrWhiteSpace(policyName))
            {
                failures.Add("Rate limit policy names cannot be empty.");
            }

            if (policy.Capacity <= 0)
            {
                failures.Add($"Policy '{policyName}' refill tokens must be greater than zero.");
            }

            if (policy.RequestCost <= 0)
            {
                failures.Add($"Policy '{policyName}' request cost must be greater than zero.");
            }

            if (policy.RequestCost > policy.Capacity)
            {
                failures.Add($"Policy '{policyName}' request cost cannot be greater than capacity");
            }
        }
    }

    private static void ValidateRoutes(RateShieldOptions options, List<string> failures)
    {
        foreach (var (routeId, route) in options.Routes)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                failures.Add("Route IDs cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(route.PolicyName))
            {
                failures.Add($"Route '{routeId}' must reference a policy name.");
            }

            if (!options.Policies.ContainsKey(route.PolicyName))
            {
                failures.Add($"Route '{routeId}' references unknown policy '{route.PolicyName}'.");
            }
        }
    }

    private static void ValidateCleanup(RateShieldOptions options, List<string> failures)
    {
        if (options.CleanUp.IntervalSeconds <= 0)
        {
            failures.Add("Cleanup interval must be greater than zero.");
        }

        if (options.CleanUp.BucketIdleTimeoutSeconds <= 0)
        {
            failures.Add("Bucket idle timeout must be greater than zero.");
        }

        if (options.CleanUp.MaxBucketsPerScan <= 0)
        {
            failures.Add("Max buckets per scan must be greater than zero.");
        }
    }

    private static void ValidateRejectionResponse(RateShieldOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.RejectionResponse.ContentType))
        {
            failures.Add("Rejection response content type is required.");
        }

        if (string.IsNullOrWhiteSpace(options.RejectionResponse.ErrorCode))
        {
            failures.Add("Rejection response error code is required.");
        }

        if (string.IsNullOrWhiteSpace(options.RejectionResponse.Message))
        {
            failures.Add("Rejection response message is required");
        }
    }

    //added to validate identity opts
    private static void ValidateIdentity(RateShieldOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.Identity.Strategy))
        {
            failures.Add("RateShield:Identity:Strategy is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Identity.ApiKeyHeaderName))
        {
            failures.Add("RateShield:Identity:ApiKeyHeaderName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Identity.ClientIdHeaderName))
        {
            failures.Add("RateShield:Identity:ClientIdHeaderName is required.");
        }

        if (options.Identity.TrustForwardedHeaders)
        {
            if (string.IsNullOrWhiteSpace(options.Identity.ForwardedForHeaderName))
            {
                failures.Add(
                    "RateShield:Identity:ForwardedForHeaderName is required when forwarded headers are trusted."
                );
            }

            if (options.Identity.TrustedProxyIpAddresses.Length == 0)
            {
                failures.Add(
                    "RateShield:Identity:TrustedProxyIpAddresses must contain at least one IP address when forwarded headers are trusted."
                );
            }
        }
    }

    private static void ValidateRedis(RateShieldOptions options, List<string> failures)
    {
        if (!string.Equals(options.Storage.Mode, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        failures.Add(
            "RateShield:Redis:ConnectionString is required when RateShield:Storage:Mode is Redis."
        );

        if (options.Redis.ConnectTimeoutMilliseconds <= 0)
        {
            failures.Add("RateShield:Redis:ConnectTimeoutMilliseconds must be greater than 0.");
        }
        if (options.Redis.CommandTimeoutMilliseconds <= 0)
        {
            failures.Add("RateShield:Redis:CommandTimeoutMilliseconds must be greater than 0.");
        }
    }
}
