using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting.Redis;

/// <summary>
/// Resolves route policy, builds a redis bucket key, runs the lua script and converts redis result to a RateLimitDecision
/// </summary>
/// <param name="scriptExecutor">the atomic lua script exec</param>
/// <param name="policyResolver">the policy resolver</param>
/// <param name="options">Rateshield config options</param>
public sealed class RedisRateLimitEvaluator(
    RedisTokenBucketScriptExecutor scriptExecutor,
    IRateLimitPolicyResolver policyResolver,
    IOptions<RateShieldOptions> options
) : IRedisRateLimitEvaluator
{
    private readonly RedisTokenBucketScriptExecutor _scriptExecutor = scriptExecutor;
    private readonly IRateLimitPolicyResolver _policyResolver = policyResolver;
    private readonly RateShieldOptions _options = options.Value;

    //impl the interface
    public async Task<RateLimitDecision> EvaluateAsync(
        RateLimitEvaluationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        //   throw new NotImplementedException();
        var policy = _policyResolver.ResolvePolicy(request.RouteId);

        var bucketKey = RedisTokenBucketKeyBuilder.Build(request.Client, request.RouteId, policy);

        var redisResult = await _scriptExecutor.EvaluateAsync(
            bucketKey: bucketKey,
            policy: policy,
            bucketIdleTimeoutSeconds: _options.CleanUp.BucketIdleTimeoutSeconds,
            cancellationToken: cancellationToken
        );

        if (redisResult.IsAllowed)
        {
            return RateLimitDecision.Allowed(
                policyName: policy.Name,
                limit: policy.Capacity,
                remaining: redisResult.RemainingTokens,
                resetAt: redisResult.ResetAt
            );
        }

        return RateLimitDecision.Rejected(
            policyName: policy.Name,
            limit: policy.Capacity,
            remaining: redisResult.RemainingTokens,
            resetAt: redisResult.ResetAt,
            retryAfter: redisResult.RetryAfter
        );
    }
}
