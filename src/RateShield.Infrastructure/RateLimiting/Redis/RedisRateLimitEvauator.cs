using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.RateLimiting;
using RateShield.Core.Time;

namespace RateShield.Infrastructure.RateLimiting.Redis;

/// <summary>
/// Resolves route policy, builds a redis bucket key, runs the lua script and converts redis result to a RateLimitDecision
/// </summary>
/// <param name="scriptExecutor">the atomic lua script exec</param>
/// <param name="policyResolver">the policy resolver</param>
/// <param name="options">Rateshield config options</param>
public sealed class RedisRateLimitEvaluator(
    IRedisTokenBucketScriptExecutor scriptExecutor,
    IRateLimitPolicyResolver policyResolver,
    IOptions<RateShieldOptions> options,
    IClock clock
) : IRedisRateLimitEvaluator
{
    private readonly IRedisTokenBucketScriptExecutor _scriptExecutor = scriptExecutor;
    private readonly IRateLimitPolicyResolver _policyResolver = policyResolver;
    private readonly RateShieldOptions _options = options.Value;
    private readonly IClock _clock = clock;

    //impl the interface
    public async Task<RateLimitDecision> EvaluateAsync(
        RateLimitEvaluationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        //   throw new NotImplementedException();
        var policy = _policyResolver.ResolvePolicy(request.RouteId);

        var bucketKey = RedisTokenBucketKeyBuilder.Build(request.Client, request.RouteId, policy);

        RedisTokenBucketResult redisResult;

        try
        {
            redisResult = await _scriptExecutor.EvaluateAsync(
                bucketKey: bucketKey,
                policy: policy,
                bucketIdleTimeoutSeconds: _options.CleanUp.BucketIdleTimeoutSeconds,
                cancellationToken: cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return CreateStorageFailureDecision(policy);
        }

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

    //helper
    private RateLimitDecision CreateStorageFailureDecision(RateLimitPolicy policy)
    {
        if (
            string.Equals(
                _options.Storage.FailureBehavior,
                "FailOpen",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return RateLimitDecision.Allowed(
                policyName: policy.Name,
                limit: policy.Capacity,
                remaining: policy.Capacity,
                resetAt: _clock.UtcNow
            );
        }

        var retryAfter = policy.RefillPeriod;

        return RateLimitDecision.Rejected(
            policyName: policy.Name,
            limit: policy.Capacity,
            remaining: 0,
            resetAt: _clock.UtcNow.Add(retryAfter),
            retryAfter: retryAfter
        );
    }
}
